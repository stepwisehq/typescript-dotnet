package main

import (
	"fmt"
	"go/ast"
	"go/parser"
	"go/token"
	"os"
	"regexp"
	"strconv"
	"strings"
)

var aliases = map[string]string{}
var out strings.Builder
var level int
var serial int

func emit(s string) { out.WriteString(strings.Repeat("    ", level) + s + "\n") }
func join(xs []ast.Expr) string {
	a := []string{}
	for _, x := range xs {
		a = append(a, e(x))
	}
	return strings.Join(a, ", ")
}
func name(n string) string {
	if n == "char" {
		return "charCode"
	}
	if n == "nil" {
		return "null"
	}
	return n
}
func typ(x ast.Expr) string {
	switch x := x.(type) {
	case *ast.Ident:
		if x.Name == "SubtreeFacts" {
			return "uint"
		}
		if a, ok := aliases[x.Name]; ok {
			return a
		}
		switch x.Name {
		case "rune", "int32", "int64", "byte":
			return "int"
		case "any":
			return "object"
		}
		return x.Name
	case *ast.SelectorExpr:
		return strings.TrimPrefix(e(x), "Core.")
	case *ast.StarExpr:
		return typ(x.X)
	case *ast.ArrayType:
		return "List<" + typ(x.Elt) + ">"
	case *ast.Ellipsis:
		return "params " + typ(x.Elt) + "[]"
	case *ast.MapType:
		return "Dictionary<" + typ(x.Key) + ", " + typ(x.Value) + ">"
	case *ast.FuncType:
		a := []string{}
		for _, p := range x.Params.List {
			a = append(a, typ(p.Type))
		}
		if x.Results != nil {
			for _, p := range x.Results.List {
				a = append(a, typ(p.Type))
			}
			return "Func<" + strings.Join(a, ", ") + ">"
		}
		return "Action<" + strings.Join(a, ", ") + ">"
	}
	panic(fmt.Sprintf("type %T", x))
}
func e(x ast.Expr) string {
	if x == nil {
		return ""
	}
	switch x := x.(type) {
	case *ast.Ident:
		n := name(x.Name)
		if strings.HasPrefix(n, "Subtree") && n != "SubtreeFacts" {
			return "JsxProjection." + strings.TrimPrefix(n, "Subtree")
		}
		for _, p := range []string{"TokenFlags", "NodeFlags", "ModifierFlags", "JSDeclarationKind", "OperatorPrecedence", "OperatorPrecedenceFlags", "identifierVariant", "EscapeSequenceScanningFlags", "regularExpressionFlags"} {
			if strings.HasPrefix(n, p) && n != p {
				return p + "." + strings.TrimPrefix(n, p)
			}
		}
		return n
	case *ast.BasicLit:
		if x.Kind == token.CHAR {
			u, _ := strconv.Unquote(x.Value)
			rr := []rune(u)
			if len(rr) == 1 && rr[0] > 0xffff {
				return fmt.Sprint(rr[0])
			}
		}
		return x.Value
	case *ast.SelectorExpr:
		a := e(x.X)
		b := name(x.Sel.Name)
		if a == "node" {
			return "this." + b
		}
		if a == "ast" {
			for _, p := range []string{"Kind", "TokenFlags", "CommentDirectiveKind"} {
				if strings.HasPrefix(b, p) && b != p {
					return p + "." + strings.TrimPrefix(b, p)
				}
			}
			return b
		}
		if a == "core" {
			for _, p := range []string{"LanguageVariant", "ScriptTarget"} {
				if strings.HasPrefix(b, p) && b != p {
					return p + "." + strings.TrimPrefix(b, p)
				}
			}
			if b == "NewTextRange" {
				return "new TextRange"
			}
			return "Core." + b
		}
		mp := map[string]string{"stringutil": "StringUtil", "jsnum": "JsNum", "utf8": "Utf8", "diagnostics": "Diagnostics", "debug": "Debug", "unicode": "Unicode", "utf16": "Utf16", "strconv": "Strconv", "fmt": "Fmt", "strings": "GoStrings"}
		if v, ok := mp[a]; ok {
			a = v
		}
		return a + "." + b
	case *ast.BinaryExpr:
		return "(" + e(x.X) + " " + strings.ReplaceAll(x.Op.String(), "&^", "& ~") + " " + e(x.Y) + ")"
	case *ast.UnaryExpr:
		if x.Op == token.AND {
			return e(x.X)
		}
		return strings.ReplaceAll(x.Op.String(), "^", "~") + e(x.X)
	case *ast.ParenExpr:
		return "(" + e(x.X) + ")"
	case *ast.CallExpr:
		f := e(x.Fun)
		if f == "len" {
			return "SliceLen(" + e(x.Args[0]) + ")"
		}
		if f == "string" {
			return "GoString.FromRune(" + join(x.Args) + ")"
		}
		if f == "rune" || f == "int" || f == "byte" || f == "int32" || f == "Core.UTF16Offset" {
			return "(int)(" + join(x.Args) + ")"
		}
		if f == "panic" {
			return "throw new InvalidOperationException(" + join(x.Args) + ")"
		}
		if f == "append" {
			return "APPEND(" + join(x.Args) + ")"
		}
		if f == "new" {
			return "new " + typ(x.Args[0]) + "()"
		}
		if f == "make" {
			if a, ok := x.Args[0].(*ast.ArrayType); ok {
				return "new List<" + typ(a.Elt) + ">()"
			}
			return "new " + typ(x.Args[0]) + "()"
		}
		return f + "(" + join(x.Args) + ")"
	case *ast.IndexExpr:
		return e(x.X) + "[" + e(x.Index) + "]"
	case *ast.SliceExpr:
		lo := e(x.Low)
		if lo == "" {
			lo = "0"
		}
		hi := e(x.High)
		if hi == "" {
			return e(x.X) + ".Substring(" + lo + ")"
		}
		return e(x.X) + ".Substring(" + lo + ", " + hi + " - (" + lo + "))"
	case *ast.CompositeLit:
		es := []string{}
		for _, el := range x.Elts {
			if kv, ok := el.(*ast.KeyValueExpr); ok {
				es = append(es, e(kv.Key)+" = "+e(kv.Value))
			} else {
				es = append(es, e(el))
			}
		}
		return "new " + typ(x.Type) + " { " + strings.Join(es, ", ") + " }"
	case *ast.FuncLit:
		ns := []string{}
		for _, p := range x.Type.Params.List {
			for _, n := range p.Names {
				ns = append(ns, n.Name)
			}
		}
		save := out
		out = strings.Builder{}
		emit("(" + strings.Join(ns, ", ") + ") =>")
		block(x.Body)
		r := strings.TrimSpace(out.String())
		out = save
		return r
	case *ast.StarExpr:
		return e(x.X)
	}
	panic(fmt.Sprintf("expr %T", x))
}
func block(b *ast.BlockStmt) {
	emit("{")
	level++
	for _, s := range b.List {
		stmt(s)
	}
	level--
	emit("}")
}
func assign(s *ast.AssignStmt) string {
	ls := join(s.Lhs)
	rs := join(s.Rhs)
	if len(s.Lhs) > 1 {
		if s.Tok == token.DEFINE {
			return "var (" + ls + ") = " + rs
		}
		return "(" + ls + ") = " + rs
	}
	if s.Tok == token.DEFINE {
		return "var " + ls + " = " + rs
	}
	if s.Tok == token.ASSIGN {
		if c, ok := s.Rhs[0].(*ast.CallExpr); ok && e(c.Fun) == "append" {
			return e(c.Args[0]) + ".Add(" + join(c.Args[1:]) + ")"
		}
	}
	return ls + " " + s.Tok.String() + " " + rs
}
func inline(s ast.Stmt) string {
	switch s := s.(type) {
	case *ast.AssignStmt:
		return assign(s)
	case *ast.IncDecStmt:
		return e(s.X) + s.Tok.String()
	case *ast.ExprStmt:
		return e(s.X)
	}
	panic(fmt.Sprintf("inline %T", s))
}

var switchNoTag bool

func stmt(s ast.Stmt) {
	switch s := s.(type) {
	case *ast.BlockStmt:
		block(s)
	case *ast.AssignStmt:
		emit(assign(s) + ";")
	case *ast.ExprStmt:
		emit(e(s.X) + ";")
	case *ast.IncDecStmt:
		emit(e(s.X) + s.Tok.String() + ";")
	case *ast.ReturnStmt:
		if len(s.Results) > 1 {
			emit("return (" + join(s.Results) + ");")
		} else {
			emit("return " + join(s.Results) + ";")
		}
	case *ast.BranchStmt:
		if s.Tok == token.FALLTHROUGH {
			emit("FALLTHROUGH;")
		} else if s.Label != nil {
			emit("goto " + s.Label.Name + "End;")
		} else {
			emit(s.Tok.String() + ";")
		}
	case *ast.DeclStmt:
		d := s.Decl.(*ast.GenDecl)
		for _, sp := range d.Specs {
			v := sp.(*ast.ValueSpec)
			for i, n := range v.Names {
				t := typ(v.Type)
				value := "default"
				if t == "string" {
					value = "\"\""
				}
				if t == "GoStrings.Builder" {
					t = "GoBuilder"
					value = "new GoBuilder()"
				}
				if len(v.Values) > i {
					value = e(v.Values[i])
				}
				emit(t + " " + n.Name + " = " + value + ";")
			}
		}
	case *ast.IfStmt:
		if s.Init != nil {
			emit("{")
			level++
			stmt(s.Init)
		}
		emit("if (" + e(s.Cond) + ")")
		block(s.Body)
		if s.Else != nil {
			emit("else")
			if _, ok := s.Else.(*ast.IfStmt); ok {
				emit("{")
				level++
				stmt(s.Else)
				level--
				emit("}")
			} else {
				stmt(s.Else)
			}
		}
		if s.Init != nil {
			level--
			emit("}")
		}
	case *ast.ForStmt:
		if s.Init != nil {
			emit("{")
			level++
			stmt(s.Init)
		}
		if s.Init == nil && s.Post == nil {
			c := e(s.Cond)
			if c == "" {
				c = "true"
			}
			emit("while (" + c + ")")
		} else {
			emit("for (; " + e(s.Cond) + "; " + func() string {
				if s.Post != nil {
					return inline(s.Post)
				}
				return ""
			}() + ")")
		}
		block(s.Body)
		if s.Init != nil {
			level--
			emit("}")
		}
	case *ast.SwitchStmt:
		if s.Init != nil {
			emit("{")
			level++
			stmt(s.Init)
		}
		serial++
		id := serial
		tag := e(s.Tag)
		if tag == "" {
			tag = "true"
		}
		emit("switch (" + tag + ")")
		emit("{")
		level++
		for _, ss := range s.Body.List {
			c := ss.(*ast.CaseClause)
			if c.List == nil {
				emit("default:")
			} else {
				for _, x := range c.List {
					if s.Tag == nil {
						emit(fmt.Sprintf("case var case%d when %s:", id, e(x)))
						id += 1000
					} else {
						emit("case " + e(x) + ":")
					}
				}
			}
			emit("{")
			level++
			for _, x := range c.Body {
				stmt(x)
			}
			emit("break;")
			level--
			emit("}")
		}
		level--
		emit("}")
		if s.Init != nil {
			level--
			emit("}")
		}
	case *ast.LabeledStmt:
		stmt(s.Stmt)
		emit(s.Label.Name + "End: ;")
	case *ast.RangeStmt:
		if s.Key == nil || e(s.Key) == "_" {
			emit("foreach (var " + e(s.Value) + " in " + e(s.X) + " ?? [])")
			block(s.Body)
		} else {
			emit("for (var " + e(s.Key) + " = 0; " + e(s.Key) + " < SliceLen(" + e(s.X) + "); " + e(s.Key) + "++)")
			emit("{")
			level++
			if s.Value != nil && e(s.Value) != "_" {
				emit("var " + e(s.Value) + " = " + e(s.X) + "[" + e(s.Key) + "]; ")
			}
			for _, x := range s.Body.List {
				stmt(x)
			}
			level--
			emit("}")
		}
	case *ast.EmptyStmt:
	default:
		panic(fmt.Sprintf("stmt %T", s))
	}
}

func main() {
	root := "upstream/TypeScript/tsc/internal/ast/"
	structs := map[string][]string{}
	methods := map[string]map[string]*ast.FuncDecl{}
	origins := map[*ast.FuncDecl]string{}
	for _, file := range []string{"ast_generated.go", "kind_generated.go", "ast.go"} {
		b, _ := os.ReadFile(root + file)
		for _, m := range regexp.MustCompile(`(?m)^\s*(\w+)\s*=\s*(Node|NodeList|Kind)\b`).FindAllStringSubmatch(string(b), -1) {
			aliases[m[1]] = m[2]
		}
		f, er := parser.ParseFile(token.NewFileSet(), root+file, nil, 0)
		if er != nil {
			panic(er)
		}
		for _, d := range f.Decls {
			switch d := d.(type) {
			case *ast.GenDecl:
				for _, sp := range d.Specs {
					if t, ok := sp.(*ast.TypeSpec); ok {
						if st, ok := t.Type.(*ast.StructType); ok {
							embeds := []string{}
							for _, f := range st.Fields.List {
								if len(f.Names) == 0 {
									embeds = append(embeds, typ(f.Type))
								}
							}
							structs[t.Name.Name] = embeds
						}
					}
				}
			case *ast.FuncDecl:
				if d.Recv != nil {
					r := typ(d.Recv.List[0].Type)
					if methods[r] == nil {
						methods[r] = map[string]*ast.FuncDecl{}
					}
					methods[r][d.Name.Name] = d
					origins[d] = file
				}
			}
		}
	}
	find := func(n, method string) *ast.FuncDecl {
        // Go promotes the unique method at the shallowest embedding depth.
        level := []string{n}
        for len(level) != 0 {
            next := []string{}
            var found *ast.FuncDecl
            for _, name := range level {
                if fn := methods[name][method]; fn != nil {
                    if found != nil && found != fn { panic("ambiguous promoted method: " + n + "." + method) }
                    found = fn
                }
                next = append(next, structs[name]...)
            }
            if found != nil { return found }
            level = next
        }
        return nil
    }
	b, _ := os.ReadFile("src/TypeScript.Scanner/Ast/AstGenerated.cs")
	payloads := regexp.MustCompile(`public sealed partial class (\w+) : NodeData`).FindAllStringSubmatch(string(b), -1)
	outputs := map[string]*strings.Builder{"ast.go": {}, "ast_generated.go": {}}
	for _, m := range payloads {
		n := m[1]
		for _, method := range []string{"computeSubtreeFacts", "propagateSubtreeFacts"} {
			fn := find(n, method)
			if fn == nil {
				continue
			}
			r := typ(fn.Recv.List[0].Type)
			if r == "NodeDefault" || r == "CompositeBase" {
				continue
			}
			out = strings.Builder{}
			level = 0
			emit("public sealed partial class " + n)
			emit("{")
			level++
			if n == "BinaryExpression" && method == "computeSubtreeFacts" {
				original := fn
				fn = &ast.FuncDecl{Body: &ast.BlockStmt{List: []ast.Stmt{fn.Body.List[0], fn.Body.List[len(fn.Body.List)-1]}}}
				origins[fn] = origins[original]
			}
			name := "ComputeJsxFacts"
			if method == "propagateSubtreeFacts" {
				name = "PropagateJsxFacts"
			}
			emit("internal override uint " + name + "()")
			block(fn.Body)
			level--
			emit("}")
			s := out.String()
			s = regexp.MustCompile(`\bKind(\w+)`).ReplaceAllString(s, "Kind.$1")
			s = strings.ReplaceAll(s, ".SubtreeFacts()", ".JsxFacts()")
			s = regexp.MustCompile(`\b(NodeFlags|ModifierFlags|TokenFlags)\.`).ReplaceAllString(s, "TypeScript.Scanner.${1}.")
			s = strings.ReplaceAll(s, "node.AsNode()", "this.AsNode()")
			s = regexp.MustCompile(`(?m)(^|[^.\w])(Is[A-Z]\w*\(|HasSyntacticModifier\()`).ReplaceAllString(s, "${1}Ast.${2}")
			s = regexp.MustCompile(`(return [^;\n]+;|throw [^;\n]+;)\n\s*break;`).ReplaceAllString(s, "$1")
			mp := map[string]string{"propagateSubtreeFacts": "Ast.JsxPropagate", "propagateNodeListSubtreeFacts": "Ast.JsxListFacts", "propagateModifierListSubtreeFacts": "Ast.JsxModifierFacts", "propagateEraseableSyntaxListSubtreeFacts": "Ast.JsxEraseableListFacts", "propagateEraseableSyntaxSubtreeFacts": "Ast.JsxEraseableFacts", "propagateObjectBindingElementSubtreeFacts": "Ast.JsxPropagate", "propagateBindingElementSubtreeFacts": "Ast.JsxPropagate"}
			for k, v := range mp {
				s = regexp.MustCompile(`\b`+k+`\b`).ReplaceAllString(s, v)
			}
			outputs[origins[fn]].WriteString(s)
		}
	}
	for file, body := range outputs {
		dest := "src/TypeScript.Scanner/Ast/Ast.cs"
		if file == "ast_generated.go" {
			dest = "src/TypeScript.Scanner/Ast/AstGenerated.cs"
		}
		data, _ := os.ReadFile(dest)
		start := "// BEGIN GENERATED JSX PROJECTION"
		end := "// END GENERATED JSX PROJECTION"
		s := string(data)
		if i := strings.Index(s, start); i >= 0 {
			j := strings.Index(s[i:], end) + i + len(end)
			s = s[:i] + s[j:]
		}
		added := start + " (tools/generate-jsx-projection.go; upstream " + file + ")\n" + body.String() + end + "\n"
		if i := strings.Index(s, "// BEGIN GENERATED AST ACCESSORS"); i >= 0 {
			s = s[:i] + added + s[i:]
		} else {
			s += "\n" + added
		}
		os.WriteFile(dest, []byte(s), 0644)
	}
}
