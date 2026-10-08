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
			return "SubtreeFacts." + strings.TrimPrefix(n, "Subtree")
		}
		for _, p := range []string{"NodeFlags", "ModifierFlags", "JSDeclarationKind", "OperatorPrecedence", "OperatorPrecedenceFlags", "identifierVariant", "EscapeSequenceScanningFlags", "regularExpressionFlags"} {
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
		if a == "n" || a == "m" {
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
		return "(" + e(x.X) + " " + x.Op.String() + " " + e(x.Y) + ")"
	case *ast.UnaryExpr:
		if x.Op == token.AND {
			return e(x.X)
		}
		return x.Op.String() + e(x.X)
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
	for _, file := range []string{"ast_generated.go", "kind_generated.go", "ast.go"} {
		b, _ := os.ReadFile(root + file)
		for _, m := range regexp.MustCompile(`(?m)^\s*(\w+)\s*=\s*(Node|NodeList|Kind)\b`).FindAllStringSubmatch(string(b), -1) {
			aliases[m[1]] = m[2]
		}
	}
	f, er := parser.ParseFile(token.NewFileSet(), root+"ast.go", nil, parser.ParseComments)
	if er != nil {
		panic(er)
	}
	selected := map[string]bool{}
	for _, n := range strings.Fields("Expression RawText ArgumentList TypeArgumentList TypeParameterList MemberList StatementList CanHaveStatements Type Initializer TagName PropertyName PropertyNameOrName IsTypeOnly CommentList Label Attributes Children ModuleSpecifier ImportClause Statement PropertyList ElementList PostfixToken QuestionToken QuestionDotToken TypeExpression ClassName SetExpression SetType SetInitializer") {
		selected[n] = true
	}
	emit("// BEGIN GENERATED AST ACCESSORS (tools/generate-ast-accessors.go)")
	emit("public sealed partial class Node")
	emit("{")
	level++
	for _, d := range f.Decls {
		fn, ok := d.(*ast.FuncDecl)
		if !ok || fn.Recv == nil || !selected[fn.Name.Name] {
			continue
		}
		rtype := typ(fn.Recv.List[0].Type)
		if rtype != "Node" && rtype != "MutableNode" {
			continue
		}
		ret := "void"
		if fn.Type.Results != nil {
			ret = typ(fn.Type.Results.List[0].Type)
		}
		ps := []string{}
		for _, p := range fn.Type.Params.List {
			for _, nn := range p.Names {
				ps = append(ps, typ(p.Type)+" "+name(nn.Name))
			}
		}
		emit("public " + ret + " " + fn.Name.Name + "(" + strings.Join(ps, ", ") + ")")
		if rtype == "MutableNode" {
			fn.Body.List = fn.Body.List[1:]
		}
		block(fn.Body)
	}
	level--
	emit("}")
	emit("// END GENERATED AST ACCESSORS")
	s := out.String()
	s = regexp.MustCompile(`\bKind(\w+)`).ReplaceAllString(s, "Kind.$1")
	s = strings.ReplaceAll(s, "SliceLen(", "GoRuntime.Len(")
	s = strings.ReplaceAll(s, ".Kind.String()", ".Kind.ToString()")
	s = regexp.MustCompile(`(return [^;\n]+;|throw [^;\n]+;)\n\s*break;`).ReplaceAllString(s, "$1")
	p := "src/TypeScript.Scanner/Ast/Ast.cs"
	old, _ := os.ReadFile(p)
	start := strings.Index(string(old), "// BEGIN GENERATED AST ACCESSORS")
	if start >= 0 {
		old = old[:start]
	}
	os.WriteFile(p, append(old, []byte("\n"+s)...), 0644)
}
