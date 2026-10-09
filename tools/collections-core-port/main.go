// Selected collections core Go AST scaffold, adapted from checker-support-port. Handwritten guarded C# remains authoritative.
// Handwritten C# adaptations remain authoritative; do not overwrite reviewed files.
package main

import (
	"fmt"
	"go/ast"
	"go/parser"
	"go/token"
	"os"
	"strconv"
	"strings"
	"unicode"
)

var output strings.Builder
var indent int
var sourceSet *token.FileSet
var methods = map[string]*ast.FuncDecl{}
var objectNames map[*ast.Object]string
var receiver *ast.Object
var namedReturns []string
var labels map[string]string
var sequence int
var kindTypes = []string{"SymbolFlags", "FlowFlags", "FunctionFlags", "ModuleInstanceState", "ExternalModuleIndicatorOptions", "OperatorPrecedence", "JSDeclarationKind", "CommentDirectiveKind", "ModifierFlags", "TokenFlags", "NodeFlags", "PragmaKind", "Kind"}
var localEnums = []string{"InferenceFlags", "InferencePriority", "ElementFlags", "IndexFlags", "MappedTypeModifiers", "MappedTypeNameTypeKind", "IntersectionFlags", "VarianceFlags", "TypeFormatFlags", "ExternalEmitHelpers", "TypeSystemPropertyName", "ContextFlags", "CheckMode", "WideningKind", "FunctionFlags","SignatureCheckMode", "MinArgumentCountFlags", "SignatureFlags", "TypePredicateKind", "SignatureKind", "RecursionFlags", "IntersectionState", "ObjectFlags", "TypeFlags", "Ternary", "ContainerFlags", "EscapeSequenceScanningFlags", "ParsingContext", "ParseFlags", "jsdocScannerInfo", "jsdocState", "propertyLikeParse", "Tristate", "PragmaKind", "Kind", "NodeFlags", "ModifierFlags", "SymbolFlags"}
var nodeAliases = map[string]bool{"Expression": true, "Statement": true, "TypeNode": true, "IdentifierNode": true, "TokenNode": true, "DeclarationName": true, "ParameterDeclarationNode": true, "SignatureDeclaration": true, "EntityName": true, "ForInitializer": true, "HeritageClauseElement": true, "TypeLiteralNodeNode": true}
var listAliases = map[string]bool{"TypeParameterList": true}

func line(s string) { output.WriteString(strings.Repeat("    ", indent) + s + "\n") }
func fail(x any) {
	panic(fmt.Sprintf("unsupported %T at %s", x, sourceSet.Position(x.(ast.Node).Pos())))
}
func ident(x *ast.Ident) string {
	if x.Obj != nil {
		if x.Obj == receiver {
			return "this"
		}
		if n, ok := objectNames[x.Obj]; ok {
			return n
		}
	}
	n := x.Name
	if n == "nil" {
		return "null"
	}
	if n == "any" {
		return "object"
	}
	if strings.HasPrefix(n, "PC") {
		return "ParsingContext." + strings.TrimPrefix(n, "PC")
	}
	for _, p := range localEnums {
		if strings.HasPrefix(n, p) && n != p {
			return p + "." + strings.TrimPrefix(n, p)
		}
	}
	switch n {
	case "event", "params", "base", "default", "operator", "out", "ref", "checked", "namespace", "string", "internal", "in", "is", "lock", "object", "new", "return", "this":
		return "@" + n
	}
	return n
}
func typeName(x ast.Expr) string {
	switch x := x.(type) {
	case *ast.Ident:
		switch x.Name {
		case "any":
			return "object"
		case "rune", "int32", "uint8", "byte":
			return "int"
		case "uint32", "SignatureId", "TypeId":
			return "uint"
		case "uint64", "NodeId", "SymbolId":
			return "ulong"
		case "int64":
			return "long"
		case "ParsingContexts":
			return "int"
		}
		return x.Name
	case *ast.SelectorExpr:
		n := x.Sel.Name
		if n == "FlowLabel" {
			return "FlowNode"
		}
		if n == "NodeId" || n == "SymbolId" {
			return "ulong"
		}
		if nodeAliases[n] {
			return "Node"
		}
		if listAliases[n] {
			return "NodeList"
		}
		if n == "Visitor" {
			return "Visitor"
		}
		switch n {
		case "Builder":
			return "GoBuilder"
		case "TextPos", "UTF16Offset":
			return "int"
		case "Arena":
			return "GoArena"
		}
		return n
	case *ast.StarExpr:
		t := typeName(x.X)
		if t == "string" || t == "bool" || t == "int" {
			return "GoRef<" + t + ">"
		}
		return t
	case *ast.ArrayType:
		return "List<" + typeName(x.Elt) + ">"
	case *ast.Ellipsis:
		return "params " + typeName(x.Elt) + "[]"
	case *ast.MapType:
		return "Dictionary<" + typeName(x.Key) + ", " + typeName(x.Value) + ">"
	case *ast.FuncType:
		a := []string{}
		for _, p := range x.Params.List {
			count := len(p.Names)
			if count == 0 {
				count = 1
			}
			for i := 0; i < count; i++ {
				a = append(a, typeName(p.Type))
			}
		}
		if x.Results != nil {
			ts := []string{}
			for _, p := range x.Results.List {
				ts = append(ts, typeName(p.Type))
			}
			ret := strings.Join(ts, ", ")
			if len(ts) > 1 {
				ret = "(" + ret + ")"
			}
			a = append(a, ret)
			return "Func<" + strings.Join(a, ", ") + ">"
		}
		return "Action<" + strings.Join(a, ", ") + ">"
	case *ast.IndexExpr:
		if e(x.X) == "Collections.Set" {
			return "HashSet<" + typeName(x.Index) + ">"
		}
		return typeName(x.X) + "<" + typeName(x.Index) + ">"
	case *ast.IndexListExpr:
		a := []string{}
		for _, v := range x.Indices {
			a = append(a, typeName(v))
		}
		return typeName(x.X) + "<" + strings.Join(a, ", ") + ">"
	case *ast.InterfaceType:
		return "object"
	}
	fail(x)
	return ""
}
func expressions(xs []ast.Expr) string {
	a := []string{}
	for _, x := range xs {
		a = append(a, e(x))
	}
	return strings.Join(a, ", ")
}
func methodExpression(x ast.Expr) (string, bool) {
	xpar, ok := x.(*ast.ParenExpr)
	if !ok {
		return "", false
	}
	star, ok := xpar.X.(*ast.StarExpr)
	if !ok {
		return "", false
	}
	id, ok := star.X.(*ast.Ident)
	if !ok || (id.Name != "Binder" && id.Name != "ActiveLabel") {
		return "", false
	}
	return "Binder", true
}
func e(x ast.Expr) string {
	if x == nil {
		return ""
	}
	switch x := x.(type) {
	case *ast.Ident:
		return ident(x)
	case *ast.BasicLit:
		if x.Kind == token.CHAR {
			s, _ := strconv.Unquote(x.Value)
			rr := []rune(s)
			if len(rr) == 1 && rr[0] > 0xffff {
				return fmt.Sprint(rr[0])
			}
		}
		return x.Value
	case *ast.SelectorExpr:
		if _, ok := methodExpression(x.X); ok {
			fn, ok := methods[x.Sel.Name]
			if !ok {
				fail(x)
			}
			ps := []string{"binderCallback"}
			aa := []string{}
			for _, f := range fn.Type.Params.List {
				for _, n := range f.Names {
					nn := "arg" + n.Name
					ps = append(ps, nn)
					aa = append(aa, nn)
				}
			}
			return "(" + strings.Join(ps, ", ") + ") => binderCallback." + x.Sel.Name + "(" + strings.Join(aa, ", ") + ")"
		}
		if id, ok := x.X.(*ast.Ident); ok {
			n := x.Sel.Name
			switch id.Name {
			case "ast":
				for _, p := range kindTypes {
					if strings.HasPrefix(n, p) && n != p {
						return p + "." + strings.TrimPrefix(n, p)
					}
				}
				return "Ast." + n
			case "core":
				for _, p := range []string{"ScriptTarget", "ScriptKind", "LanguageVariant", "ModuleKind", "NewLineKind", "Tristate"} {
					if strings.HasPrefix(n, p) && n != p {
						return p + "." + strings.TrimPrefix(n, p)
					}
				}
				if n == "TSTrue" {
					return "Tristate.True"
				}
				if n == "TSFalse" {
					return "Tristate.False"
				}
				if n == "TSUnknown" {
					return "Tristate.Unknown"
				}
				return "Core." + n
			case "scanner":
				return "Scanner." + n
			case "collections":
				return "Collections." + n
			case "strings":
				return "GoStrings." + n
			case "slices":
				return "GoSlices." + n
			case "stringutil":
				return "StringUtil." + n
			case "diagnostics":
				return "Diagnostics." + n
			case "debug":
				return "Debug." + n
			case "utf8":
				return "Utf8." + n
			case "unicode":
				return "GoUnicode." + n
			case "strconv":
				return "Strconv." + n
			case "tspath":
				return "TsPath." + n
			}
		}
		return e(x.X) + "." + x.Sel.Name
	case *ast.ParenExpr:
		return "(" + e(x.X) + ")"
	case *ast.BinaryExpr:
		op := x.Op.String()
		if x.Op == token.SHL || x.Op == token.SHR {
			return "(" + e(x.X) + " " + op + " (int)(" + e(x.Y) + "))"
		}
		if x.Op == token.AND_NOT {
			return "(" + e(x.X) + " & ~" + e(x.Y) + ")"
		}
		return "(" + e(x.X) + " " + op + " " + e(x.Y) + ")"
	case *ast.UnaryExpr:
		if x.Op == token.AND {
			if id, ok := x.X.(*ast.Ident); ok && id.Obj != nil {
				if vd, ok := id.Obj.Decl.(*ast.ValueSpec); ok && vd.Type != nil && typeName(vd.Type) == "string" {
					v := e(id)
					return "new GoRef<string>(() => " + v + ", value => " + v + " = value)"
				}
			}
			return e(x.X)
		}
		return x.Op.String() + e(x.X)
	case *ast.StarExpr:
		return e(x.X) + ".Value"
	case *ast.IndexExpr:
		return e(x.X) + "[" + e(x.Index) + "]"
	case *ast.SliceExpr:
		lo := e(x.Low)
		if lo == "" {
			lo = "0"
		}
		hi := e(x.High)
		if hi == "" {
			return "GoRuntime.Slice(" + e(x.X) + ", " + lo + ")"
		}
		return "GoRuntime.Slice(" + e(x.X) + ", " + lo + ", " + hi + ")"
	case *ast.TypeAssertExpr:
		return "((" + typeName(x.Type) + ")" + e(x.X) + ")"
	case *ast.CompositeLit:
		if array, ok := x.Type.(*ast.ArrayType); ok {
			for _, el := range x.Elts {
				if nested, ok := el.(*ast.CompositeLit); ok && nested.Type == nil {
					nested.Type = array.Elt
				}
			}
			return "new List<" + typeName(array.Elt) + "> { " + expressions(x.Elts) + " }"
		}
		vals := []string{}
		for _, el := range x.Elts {
			if kv, ok := el.(*ast.KeyValueExpr); ok {
				vals = append(vals, e(kv.Key)+" = "+e(kv.Value))
			} else {
				vals = append(vals, e(el))
			}
		}
		return "new " + typeName(x.Type) + " { " + strings.Join(vals, ", ") + " }"
	case *ast.CallExpr:
		f := e(x.Fun)
		if f == "Ast.GetSymbolTable" && len(x.Args) == 1 {
			if u, ok := x.Args[0].(*ast.UnaryExpr); ok && u.Op == token.AND {
				v := e(u.X)
				return "Ast.GetSymbolTable(new GoRef<SymbolTable>(() => " + v + ", value => " + v + " = value))"
			}
		}
		switch f {
		case "len":
			return "GoRuntime.Len(" + expressions(x.Args) + ")"
		case "cap":
			return "GoRuntime.Cap(" + expressions(x.Args) + ")"
		case "string", "@string":
			return "GoString.FromRune(" + expressions(x.Args) + ")"
		case "rune", "int", "int32", "uint8", "byte", "uint32", "int64", "uint64", "ParsingContexts":
			t := "int"
			if f == "uint32" {
				t = "uint"
			}
			if f == "int64" {
				t = "long"
			}
			if f == "uint64" {
				t = "ulong"
			}
			return "(" + t + ")(" + expressions(x.Args) + ")"
		case "panic":
			return "throw new InvalidOperationException(" + expressions(x.Args) + ")"
		case "append":
			return "GoRuntime.Append(" + expressions(x.Args) + ")"
		case "make":
			if a, ok := x.Args[0].(*ast.ArrayType); ok {
				if len(x.Args) < 2 {
					fail(x)
				}
				return "GoRuntime.MakeSlice<" + typeName(a.Elt) + ">(" + e(x.Args[1]) + ")"
			}
			return "new " + typeName(x.Args[0]) + "()"
		case "min":
			return "Math.Min(" + expressions(x.Args) + ")"
		case "max":
			return "Math.Max(" + expressions(x.Args) + ")"
		case "delete":
			return e(x.Args[0]) + ".Remove(" + e(x.Args[1]) + ")"
		case "clear":
			return e(x.Args[0]) + ".Clear()"
		}
		if id, ok := x.Fun.(*ast.Ident); ok {
			for _, n := range []string{"ParseFlags", "ParsingContext", "jsdocState", "jsdocScannerInfo", "propertyLikeParse"} {
				if id.Name == n {
					return "(" + n + ")(" + expressions(x.Args) + ")"
				}
			}
		}
		return f + "(" + expressions(x.Args) + ")"
	case *ast.FuncLit:
		ps := []string{}
		for _, p := range x.Type.Params.List {
			for _, n := range p.Names {
				ps = append(ps, ident(n))
			}
		}
		saved := output
		savedIndent := indent
		savedReturns := namedReturns
		output = strings.Builder{}
		indent = 0
		namedReturns = nil
		line("(" + strings.Join(ps, ", ") + ") =>")
		block(x.Body)
		r := strings.TrimSpace(output.String())
		output = saved
		indent = savedIndent
		namedReturns = savedReturns
		return r
	}
	fail(x)
	return ""
}
func assignment(x *ast.AssignStmt) string {
	left := expressions(x.Lhs)
	right := expressions(x.Rhs)
	if len(x.Rhs) > 1 {
		right = "(" + right + ")"
	}
	if len(x.Lhs) > 1 {
		if len(x.Rhs) == 1 {
			if idx, ok := x.Rhs[0].(*ast.IndexExpr); ok {
				return "var " + e(x.Lhs[1]) + " = " + e(idx.X) + ".TryGetValue(" + e(idx.Index) + ", out var " + e(x.Lhs[0]) + ")"
			}
			if ta, ok := x.Rhs[0].(*ast.TypeAssertExpr); ok {
				return "var " + e(x.Lhs[0]) + " = " + e(ta.X) + " as " + typeName(ta.Type) + "; var " + e(x.Lhs[1]) + " = " + e(x.Lhs[0]) + " != null"
			}
		}
		if x.Tok == token.DEFINE {
			sequence++
			temp := fmt.Sprintf("tuple%d", sequence)
			parts := []string{"var " + temp + " = " + right}
			for i, lhs := range x.Lhs {
				id, ok := lhs.(*ast.Ident)
				if !ok {
					fail(lhs)
				}
				if id.Name == "_" {
					continue
				}
				prefix := ""
				if id.Obj != nil && id.Obj.Decl == x {
					prefix = "var "
				}
				parts = append(parts, prefix+e(lhs)+" = "+temp+".Item"+fmt.Sprint(i+1))
			}
			return strings.Join(parts, "; ")
		}
		return "(" + left + ") = " + right
	}
	if x.Tok == token.DEFINE {
		if literal, ok := x.Rhs[0].(*ast.FuncLit); ok {
			return typeName(literal.Type) + " " + left + " = " + right
		}
		if c, ok := x.Rhs[0].(*ast.CallExpr); ok {
			if _, ok := c.Fun.(*ast.FuncType); ok {
				fail(x)
			}
			if l, ok := c.Fun.(*ast.FuncLit); ok {
				return typeName(l.Type) + " " + left + " = " + right
			}
		}
		return "var " + left + " = " + right
	}
	if x.Tok == token.AND_NOT_ASSIGN {
		return left + " &= ~(" + right + ")"
	}
	return left + " " + x.Tok.String() + " " + right
}
func inline(x ast.Stmt) string {
	switch x := x.(type) {
	case *ast.AssignStmt:
		return assignment(x)
	case *ast.IncDecStmt:
		return e(x.X) + x.Tok.String()
	case *ast.ExprStmt:
		return e(x.X)
	}
	fail(x)
	return ""
}
func block(b *ast.BlockStmt) {
	line("{")
	indent++
	for _, s := range b.List {
		statement(s)
	}
	indent--
	line("}")
}

// caseTerminates follows only unconditional paths; the compiler diagnoses rarer cases.
func caseTerminates(body []ast.Stmt) bool {
	if len(body) == 0 {
		return false
	}
	switch x := body[len(body)-1].(type) {
	case *ast.ReturnStmt, *ast.BranchStmt:
		return true
	case *ast.BlockStmt:
		return caseTerminates(x.List)
	case *ast.ExprStmt:
		if c, ok := x.X.(*ast.CallExpr); ok {
			if id, ok := c.Fun.(*ast.Ident); ok && id.Name == "panic" {
				return true
			}
		}
	case *ast.SwitchStmt:
		hasDefault := false
		for _, st := range x.Body.List {
			c := st.(*ast.CaseClause)
			if c.List == nil {
				hasDefault = true
			}
			if !caseTerminates(c.Body) {
				return false
			}
		}
		return hasDefault
	case *ast.IfStmt:
		if x.Else == nil {
			return false
		}
		switch y := x.Else.(type) {
		case *ast.BlockStmt:
			return caseTerminates(x.Body.List) && caseTerminates(y.List)
		case *ast.IfStmt:
			return caseTerminates(x.Body.List) && caseTerminates([]ast.Stmt{y})
		}
	}
	return false
}
func statement(x ast.Stmt) {
	switch x := x.(type) {
	case *ast.BlockStmt:
		block(x)
	case *ast.AssignStmt:
		line(assignment(x) + ";")
	case *ast.ExprStmt:
		line(e(x.X) + ";")
	case *ast.IncDecStmt:
		line(e(x.X) + x.Tok.String() + ";")
	case *ast.ReturnStmt:
		ret := expressions(x.Results)
		if len(x.Results) > 1 {
			ret = "(" + ret + ")"
		}
		if len(x.Results) == 0 && len(namedReturns) > 0 {
			ret = strings.Join(namedReturns, ", ")
			if len(namedReturns) > 1 {
				ret = "(" + ret + ")"
			}
		}
		line("return " + ret + ";")
	case *ast.BranchStmt:
		if x.Tok == token.FALLTHROUGH {
			line("FALLTHROUGH;")
			return
		}
		if x.Label != nil {
			suffix := "End"
			if x.Tok == token.CONTINUE {
				suffix = "Continue"
			}
			line("goto " + x.Label.Name + suffix + ";")
			return
		}
		line(x.Tok.String() + ";")
	case *ast.DeclStmt:
		decl := x.Decl.(*ast.GenDecl)
		for _, spec := range decl.Specs {
			v, ok := spec.(*ast.ValueSpec)
			if !ok {
				fail(spec)
			}
			for i, n := range v.Names {
				t := "var"
				if v.Type != nil {
					t = typeName(v.Type)
				}
				initial := "default"
				if t == "string" {
					initial = "\"\""
				}
				if t == "GoBuilder" {
					t = "GoBuilder"
					initial = "new GoBuilder()"
				}
				if len(v.Values) > i {
					initial = e(v.Values[i])
				}
				if t == "var" && initial == "default" {
					fail(v)
				}
				line(t + " " + ident(n) + " = " + initial + ";")
			}
		}
	case *ast.IfStmt:
		if x.Init != nil {
			line("{")
			indent++
			statement(x.Init)
		}
		line("if (" + e(x.Cond) + ")")
		block(x.Body)
		if x.Else != nil {
			line("else")
			if b, ok := x.Else.(*ast.BlockStmt); ok {
				block(b)
			} else {
				line("{")
				indent++
				statement(x.Else)
				indent--
				line("}")
			}
		}
		if x.Init != nil {
			indent--
			line("}")
		}
	case *ast.ForStmt:
		if x.Init != nil {
			line("{")
			indent++
			statement(x.Init)
		}
		cond := e(x.Cond)
		if cond == "" {
			cond = "true"
		}
		post := ""
		if x.Post != nil {
			post = inline(x.Post)
		}
		if x.Post == nil {
			line("while (" + cond + ")")
		} else {
			line("for (; " + cond + "; " + post + ")")
		}
		block(x.Body)
		if x.Init != nil {
			indent--
			line("}")
		}
	case *ast.RangeStmt:
		key, value := e(x.Key), e(x.Value)
		sequence++
		seq := fmt.Sprintf("range%d", sequence)
		line("{")
		indent++
		if value == "" || value == "_" {
			if key == "" || key == "_" {
				line("foreach (var ignored" + fmt.Sprint(sequence) + " in GoRuntime.RangeValues(" + e(x.X) + "))")
			} else {
				line("foreach (var " + key + " in GoRuntime.RangeKeys(" + e(x.X) + "))")
			}
			block(x.Body)
		} else if key == "_" || key == "" {
			line("foreach (var " + value + " in GoRuntime.RangeValues(" + e(x.X) + "))")
			block(x.Body)
		} else {
			line("foreach (var " + seq + " in GoRuntime.Range(" + e(x.X) + "))")
			line("{")
			indent++
			line("var " + key + " = " + seq + ".Key;")
			line("var " + value + " = " + seq + ".Value;")
			for _, s := range x.Body.List {
				statement(s)
			}
			indent--
			line("}")
		}
		indent--
		line("}")
	case *ast.SwitchStmt:
		if x.Init != nil {
			line("{")
			indent++
			statement(x.Init)
		}
		tag := e(x.Tag)
		if tag == "" {
			tag = "true"
		}
		line("switch (" + tag + ")")
		line("{")
		indent++
		for ci, ss := range x.Body.List {
			c := ss.(*ast.CaseClause)
			if c.List == nil {
				line("default:")
			} else {
				for _, v := range c.List {
					if x.Tag == nil {
						line("case true when " + e(v) + ":")
					} else {
						line("case " + e(v) + ":")
					}
				}
			}
			line("{")
			indent++
			for _, s := range c.Body {
				if b, ok := s.(*ast.BranchStmt); ok && b.Tok == token.FALLTHROUGH {
					next := x.Body.List[ci+1].(*ast.CaseClause)
					if next.List == nil {
						line("goto default;")
					} else {
						line("goto case " + e(next.List[0]) + ";")
					}
					continue
				}
				statement(s)
			}
			if !caseTerminates(c.Body) {
				line("break;")
			}
			indent--
			line("}")
		}
		indent--
		line("}")
		if x.Init != nil {
			indent--
			line("}")
		}
	case *ast.LabeledStmt:
		statement(x.Stmt)
		line(x.Label.Name + "End: ;")
	case *ast.DeferStmt:
		if c, ok := x.Call.Fun.(*ast.Ident); ok && c.Name == "putBinder" {
			line("// Go binder-pool return omitted: this port creates an isolated managed binder.")
			return
		}
		fail(x)
	case *ast.EmptyStmt:
	default:
		fail(x)
	}
}
func function(fn *ast.FuncDecl) {
	if fn.Name.Name == "getBinder" || fn.Name.Name == "putBinder" {
		return
	}
	if fn.Recv != nil {
		if st, ok := fn.Recv.List[0].Type.(*ast.StarExpr); ok {
			if id, ok := st.X.(*ast.Ident); ok && id.Name == "ActiveLabel" {
				return
			}
		}
	}
	receiver = nil
	objectNames = map[*ast.Object]string{}
	counts := map[string]int{}
	if fn.Recv != nil {
		receiver = fn.Recv.List[0].Names[0].Obj
	}
	ast.Inspect(fn, func(n ast.Node) bool {
		if id, ok := n.(*ast.Ident); ok && id.Obj != nil && id.Obj != receiver && id.Obj.Kind == ast.Var {
			if _, seen := objectNames[id.Obj]; !seen {
				counts[id.Name]++
				nn := id.Name
				if counts[id.Name] > 1 {
					nn += fmt.Sprint(counts[id.Name])
				}
				if nn == "event" || nn == "params" || nn == "base" || nn == "operator" || nn == "ref" || nn == "default" || nn == "class" || nn == "namespace" || nn == "object" || nn == "string" || nn == "internal" || nn == "out" || nn == "in" {
					nn = "@" + nn
				}
				objectNames[id.Obj] = nn
			}
		}
		return true
	})
	ps := []string{}
	for _, p := range fn.Type.Params.List {
		for _, n := range p.Names {
			ps = append(ps, typeName(p.Type)+" "+ident(n))
		}
	}
	ret := "void"
	namedReturns = nil
	if fn.Type.Results != nil {
		ts := []string{}
		for _, r := range fn.Type.Results.List {
			ts = append(ts, typeName(r.Type))
			for _, n := range r.Names {
				namedReturns = append(namedReturns, ident(n))
			}
		}
		ret = strings.Join(ts, ", ")
		if len(ts) > 1 {
			ret = "(" + ret + ")"
		}
	}
	access := "private"
	if unicode.IsUpper(rune(fn.Name.Name[0])) {
		access = "public"
	}
	static := ""
	if fn.Recv == nil {
		static = " static"
	}
	generic := ""
	if fn.Type.TypeParams != nil {
		ns := []string{}
		for _, p := range fn.Type.TypeParams.List {
			for _, n := range p.Names {
				ns = append(ns, n.Name)
			}
		}
		generic = "<" + strings.Join(ns, ", ") + ">"
	}
	if fn.Doc != nil {
		for _, c := range fn.Doc.List {
			if strings.HasPrefix(c.Text, "//") {
				line(c.Text)
			} else {
				for _, cl := range strings.Split(strings.TrimSuffix(strings.TrimPrefix(c.Text, "/*"), "*/"), "\n") {
					line("// " + strings.TrimSpace(cl))
				}
			}
		}
	}
	line(fmt.Sprintf("// Upstream %s.", sourceSet.Position(fn.Pos())))
	line(access + static + " " + ret + " " + fn.Name.Name + generic + "(" + strings.Join(ps, ", ") + ")")
	if len(namedReturns) == 0 {
		block(fn.Body)
	} else {
		line("{")
		indent++
		for _, r := range fn.Type.Results.List {
			for _, n := range r.Names {
				value := "default"
				if typeName(r.Type) == "string" {
					value = "\"\""
				}
				line(typeName(r.Type) + " " + ident(n) + " = " + value + ";")
			}
		}
		for _, s := range fn.Body.List {
			statement(s)
		}
		indent--
		line("}")
	}
	line("")
}
func main() {
	if len(os.Args) < 3 || len(os.Args) > 4 {
		panic("usage: collections-core-port upstream.go output.cs [function1,function2,...]")
	}
	sourceSet = token.NewFileSet()
	f, err := parser.ParseFile(sourceSet, os.Args[1], nil, parser.ParseComments)
	if err != nil {
		panic(err)
	}
	for _, d := range f.Decls {
		if fn, ok := d.(*ast.FuncDecl); ok {
			methods[fn.Name.Name] = fn
		}
	}
	selected := map[string]bool{}
	if len(os.Args) == 4 {
		for _, name := range strings.Split(os.Args[3], ",") {
			selected[name] = true
		}
	}
	line("// Copyright (c) Microsoft Corporation. All rights reserved.")
	line("// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.")
	line("// Modified C# port of " + os.Args[1] + ", upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.")
	line("#nullable disable")
	line("using TypeScript.Scanner;")
	line("namespace TypeScript.Checking;")
	line("public sealed partial class Checker")
	line("{")
	indent++
	for _, d := range f.Decls {
		if fn, ok := d.(*ast.FuncDecl); ok {
			if len(selected) == 0 || selected[fn.Name.Name] {
				function(fn)
			}
		}
	}
	indent--
	line("}")
	if strings.Contains(output.String(), "FALLTHROUGH;") {
		panic("untranslated fallthrough")
	}
	if err := os.WriteFile(os.Args[2], []byte(output.String()), 0644); err != nil {
		panic(err)
	}
}
