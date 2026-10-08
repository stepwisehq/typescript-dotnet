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
		if a == "s" || a == "scanner" {
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
	all := map[string]*ast.FuncDecl{}
	src := map[string]string{}
	for _, file := range []string{"utilities.go", "precedence.go"} {
		f, er := parser.ParseFile(token.NewFileSet(), root+file, nil, parser.ParseComments)
		if er != nil {
			panic(er)
		}
		for _, d := range f.Decls {
			if fn, ok := d.(*ast.FuncDecl); ok && fn.Recv == nil {
				all[fn.Name.Name] = fn
				src[fn.Name.Name] = file
			}
		}
	}
	existing := map[string]bool{}
	generated, _ := os.ReadFile("src/TypeScript.Scanner/Ast/AstGenerated.cs")
	for _, m := range regexp.MustCompile(`public static bool (\w+)\(`).FindAllStringSubmatch(string(generated), -1) {
		existing[m[1]] = true
	}
	selected := map[string]bool{}
	var add func(string)
	add = func(n string) {
		if selected[n] || existing[n] || all[n] == nil {
			return
		}
		selected[n] = true
		ast.Inspect(all[n].Body, func(x ast.Node) bool {
			if c, ok := x.(*ast.CallExpr); ok {
				if f, ok := c.Fun.(*ast.Ident); ok {
					add(f.Name)
				}
			}
			return true
		})
	}
	for _, n := range strings.Fields("IsTypeNode IsJsxOpeningLikeElement IsImportMeta IsKeyword IsQuestionToken IsFunctionLike IsModifier CanHaveDecorators CanHaveIllegalDecorators CompareNodePositions ForEachDynamicImportOrRequireCall GetAssignmentDeclarationKind GetExternalModuleName GetReparsedNodeForNode GetRightMostAssignedExpression GetSourceFileOfNode HasSamePropertyAccessName HasSyntacticModifier IsAmbientModule IsAnyImportOrReExport IsClassMemberModifier IsExternalModule IsImportCall IsImportPhaseMetaProperty IsInJSFile IsLeftHandSideExpression IsOptionalChain IsThisIdentifier NodeIsMissing NodeIsPresent ModifierToFlag ModifiersToFlags SetImportsOfSourceFile TagNamesAreEquivalent GetBinaryOperatorPrecedence") {
		add(n)
	}
	for _, file := range []string{"utilities.go", "precedence.go"} {
		out = strings.Builder{}
		level = 0
		emit("// Copyright (c) Microsoft Corporation. All rights reserved.")
		emit("// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.")
		emit("// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.")
		emit("// Port of tsc/internal/ast/" + file + "; parser-required syntax dependency slice.")
		emit("#nullable disable")
		emit("namespace TypeScript.Scanner;")
		emit("public static partial class Ast")
		emit("{")
		level++
		f, _ := parser.ParseFile(token.NewFileSet(), root+file, nil, parser.ParseComments)
		for _, d := range f.Decls {
			fn, ok := d.(*ast.FuncDecl)
			if !ok || !selected[fn.Name.Name] {
				continue
			}
			n := fn.Name.Name
			ret := "void"
			if fn.Type.Results != nil {
				ts := []string{}
				for _, p := range fn.Type.Results.List {
					ts = append(ts, typ(p.Type))
				}
				ret = strings.Join(ts, ", ")
				if len(ts) > 1 {
					ret = "(" + ret + ")"
				}
			}
			ps := []string{}
			for _, p := range fn.Type.Params.List {
				for _, nn := range p.Names {
					ps = append(ps, typ(p.Type)+" "+name(nn.Name))
				}
			}
			emit("// Upstream function: " + n)
			emit("public static " + ret + " " + n + "(" + strings.Join(ps, ", ") + ")")
			fmt.Fprintf(os.Stderr, "%s: %s\n", file, n)
			if fn.Type.Results != nil {
				decls := []ast.Stmt{}
				for _, r := range fn.Type.Results.List {
					for _, n := range r.Names {
						decls = append(decls, &ast.DeclStmt{Decl: &ast.GenDecl{Tok: token.VAR, Specs: []ast.Spec{&ast.ValueSpec{Names: []*ast.Ident{n}, Type: r.Type}}}})
					}
				}
				fn.Body.List = append(decls, fn.Body.List...)
			}
			block(fn.Body)
		}
		level--
		emit("}")
		dst := "src/TypeScript.Scanner/Ast/Utilities.cs"
		if file == "precedence.go" {
			dst = "src/TypeScript.Scanner/Ast/Precedence.cs"
		}
		os.WriteFile(dst, []byte(out.String()), 0644)
	}
}
