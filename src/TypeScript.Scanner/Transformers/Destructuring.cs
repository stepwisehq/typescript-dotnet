// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/transformers/destructuring.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
namespace TypeScript.Transformers;
internal enum FlattenLevel { All, ObjectRest }
internal delegate Node CreateAssignmentCallback(Node name,Node value,TextRange? location);
internal static partial class Transformers
{
    internal static Node FlattenDestructuringAssignment(Transformer tx,Node node,bool needsValue,FlattenLevel level,CreateAssignmentCallback callback)
    {
        var f=new Flattener(tx,level) {createAssignmentCallback=callback,hoistTempVariables=true};
        f.emitBindingOrAssignment=static(self,target,value,location,original)=>self.emitAssignment(target,value,location,original);
        f.createArrayBindingOrAssignmentPattern=static(self,elements)=>self.createArrayAssignmentPattern(elements);
        f.createObjectBindingOrAssignmentPattern=static(self,elements)=>self.createObjectAssignmentPattern(elements);
        f.createArrayBindingOrAssignmentElement=static(self,expr)=>self.createArrayAssignmentElement(expr);
        return f.flattenDestructuringAssignment(node,needsValue);
    }
    internal static Node FlattenDestructuringBinding(Transformer tx,Node node,Node rval,FlattenLevel level,bool hoistTempVariables,bool skipInitializer)
    {
        var f=new Flattener(tx,level) {hoistTempVariables=hoistTempVariables};
        f.emitBindingOrAssignment=static(self,target,value,location,original)=>self.emitBinding(target,value,location,original);
        f.createArrayBindingOrAssignmentPattern=static(self,elements)=>self.createArrayBindingPattern(elements);
        f.createObjectBindingOrAssignmentPattern=static(self,elements)=>self.createObjectBindingPattern(elements);
        f.createArrayBindingOrAssignmentElement=static(self,expr)=>self.createArrayBindingElement(expr);
        return f.flattenDestructuringBinding(node,rval,skipInitializer);
    }
    private struct pendingDecl
    {
        internal Slice<Node> pendingExpressions;
        internal Node name,value;
        internal TextRange location;
        internal Node original;
    }
    private readonly record struct restIdElemPair(Node id,Node element);
    private sealed class Flattener
    {
        internal readonly Transformer tx;
        internal readonly FlattenLevel level;
        internal CreateAssignmentCallback createAssignmentCallback;
        internal Slice<Node> expressions;
        private Slice<pendingDecl> declarations;
        private bool hasTransformedPriorElement;
        internal bool hoistTempVariables;
        internal Action<Flattener,Node,Node,TextRange,Node> emitBindingOrAssignment;
        internal Func<Flattener,Slice<Node>,Node> createArrayBindingOrAssignmentPattern,createObjectBindingOrAssignmentPattern;
        internal Func<Flattener,Node,Node> createArrayBindingOrAssignmentElement;
        internal Flattener(Transformer tx,FlattenLevel level) {this.tx=tx;this.level=level;}
        internal Node createArrayAssignmentPattern(Slice<Node> elements)=>tx.Factory().NewArrayLiteralExpression(tx.Factory().NewNodeList(elements),false);
        internal Node createObjectAssignmentPattern(Slice<Node> elements)=>tx.Factory().NewObjectLiteralExpression(tx.Factory().NewNodeList(elements),false);
        internal Node createArrayAssignmentElement(Node expr)=>expr;
        internal void emitAssignment(Node target,Node value,TextRange location,Node original)
        {
            Node expression;
            if(createAssignmentCallback!=null&&Ast.IsIdentifier(target))expression=createAssignmentCallback(target,value,location);
            else {expression=tx.Factory().NewAssignmentExpression(tx.Visitor().VisitNode(target),value);expression.Loc=location;}
            tx.EmitContext().SetOriginal(expression,original);emitExpression(expression);
        }
        internal Node createArrayBindingPattern(Slice<Node> elements)=>tx.Factory().NewBindingPattern(Kind.ArrayBindingPattern,tx.Factory().NewNodeList(elements));
        internal Node createObjectBindingPattern(Slice<Node> elements)=>tx.Factory().NewBindingPattern(Kind.ObjectBindingPattern,tx.Factory().NewNodeList(elements));
        internal Node createArrayBindingElement(Node expr)=>tx.Factory().NewBindingElement(null,null,expr,null);
        internal void emitBinding(Node target,Node value,TextRange location,Node original)
        {
            if(expressions.Length>0) {value=tx.Factory().InlineExpressions(expressions.Append(value));expressions=default;}
            declarations=declarations.Append(new pendingDecl {name=target,value=value,location=location,original=original});
        }
        private void emitExpression(Node expr)=>expressions=expressions.Append(expr);
        private Node ensureIdentifier(Node value,bool reuseIdentifierExpressions,TextRange location)
        {
            if(reuseIdentifierExpressions&&Ast.IsIdentifier(value))return value;
            var temp=tx.Factory().NewTempVariable();
            if(hoistTempVariables)
            {
                tx.EmitContext().AddVariableDeclaration(temp);
                var assign=tx.Factory().NewAssignmentExpression(temp,value);assign.Loc=location;emitExpression(assign);
            }
            else emitBindingOrAssignment(this,temp,value,location,null);
            return temp;
        }
        private Node createDefaultValueCheck(Node value,Node defaultValue,TextRange location)
        {
            value=ensureIdentifier(value,true,location);
            return tx.Factory().NewConditionalExpression(tx.Factory().NewTypeCheck(value,"undefined"),tx.Factory().NewToken(Kind.QuestionToken),defaultValue,tx.Factory().NewToken(Kind.ColonToken),value);
        }
        private Node createDestructuringPropertyAccess(Node value,Node propertyName)
        {
            if(Ast.IsComputedPropertyName(propertyName))
            {
                var argument=ensureIdentifier(tx.Visitor().VisitNode(propertyName.Expression()),false,propertyName.Loc);
                return tx.Factory().NewElementAccessExpression(value,null,argument,NodeFlags.None);
            }
            else if(Ast.IsStringOrNumericLiteralLike(propertyName)||Ast.IsBigIntLiteral(propertyName))
            {
                var argument=propertyName.Clone(tx.Factory());return tx.Factory().NewElementAccessExpression(value,null,argument,NodeFlags.None);
            }
            else
            {
                var name=tx.Factory().NewIdentifierWithTextValue(propertyName.TextValue());
                return tx.Factory().NewPropertyAccessExpression(value,null,name,NodeFlags.None);
            }
        }
        internal Node flattenDestructuringAssignment(Node node,bool needsValue)
        {
            var location=node.Loc;Node value=null;
            if(Ast.IsDestructuringAssignment(node))
            {
                value=node.AsBinaryExpression().Right;
                while(Ast.IsEmptyArrayLiteral(node.AsBinaryExpression().Left)||Ast.IsEmptyObjectLiteral(node.AsBinaryExpression().Left))
                {
                    if(Ast.IsDestructuringAssignment(value)) {node=value;location=node.Loc;value=node.AsBinaryExpression().Right;}
                    else return tx.Visitor().VisitNode(value);
                }
            }
            if(value!=null)
            {
                value=tx.Visitor().VisitNode(value);
                if(Ast.IsIdentifier(value)&&BindingOrAssignmentElementAssignsToName(node,value.TextValue())||BindingOrAssignmentElementContainsNonLiteralComputedName(node))value=ensureIdentifier(value,false,location);
                else if(needsValue)value=ensureIdentifier(value,true,location);
                else if(Ast.NodeIsSynthesized(node))location=value.Loc;
            }
            flattenBindingOrAssignmentElement(node,value,location,Ast.IsDestructuringAssignment(node));
            if(value!=null&&needsValue) {if(expressions.Length==0)return value;expressions=expressions.Append(value);}
            var result=tx.Factory().InlineExpressions(expressions);return result??tx.Factory().NewOmittedExpression();
        }
        internal Node flattenDestructuringBinding(Node node,Node rval,bool skipInitializer)
        {
            if(Ast.IsVariableDeclaration(node))
            {
                var initializer=GetInitializerOfBindingOrAssignmentElement(node);
                if(initializer!=null&&(Ast.IsIdentifier(initializer)&&BindingOrAssignmentElementAssignsToName(node,initializer.TextValue())||BindingOrAssignmentElementContainsNonLiteralComputedName(node)))
                {
                    initializer=ensureIdentifier(tx.Visitor().VisitNode(initializer),false,initializer.Loc);
                    node=tx.Factory().UpdateVariableDeclaration(node.AsVariableDeclaration(),node.Name(),null,null,initializer);
                }
            }
            flattenBindingOrAssignmentElement(node,rval,node.Loc,skipInitializer);
            if(expressions.Length>0)
            {
                var temp=tx.Factory().NewTempVariable();
                if(hoistTempVariables)
                {
                    var value=tx.Factory().InlineExpressions(expressions);expressions=default;emitBindingOrAssignment(this,temp,value,default,null);
                }
                else
                {
                    tx.EmitContext().AddVariableDeclaration(temp);
                    var last=declarations[declarations.Length-1];
                    last.pendingExpressions=last.pendingExpressions.Append(tx.Factory().NewAssignmentExpression(temp,last.value));
                    last.pendingExpressions=last.pendingExpressions.Append(expressions);last.value=temp;
                    // Go takes &declarations[last]; copied value header must be written to its owner slot.
                    declarations[declarations.Length-1]=last;
                }
            }
            var decls=Slice<Node>.Make(0,declarations.Length);
            foreach(var pending in declarations)
            {
                var expr=pending.value;
                if(pending.pendingExpressions.Length>0)expr=tx.Factory().InlineExpressions(pending.pendingExpressions.Append(pending.value));
                var decl=tx.Factory().NewVariableDeclaration(pending.name,null,null,expr);decl.Loc=pending.location;
                if(pending.original!=null)tx.EmitContext().SetOriginal(decl,pending.original);
                decls=decls.Append(decl);
            }
            if(decls.Length==1)return decls[0];if(decls.Length==0)return null;return tx.Factory().NewSyntaxList(decls);
        }
        private void flattenBindingOrAssignmentElement(Node element,Node value,TextRange location,bool skipInitializer)
        {
            var target=Ast.GetTargetOfBindingOrAssignmentElement(element);if(target==null)return;
            if(!skipInitializer)
            {
                var initializer=tx.Visitor().VisitNode(GetInitializerOfBindingOrAssignmentElement(element));
                if(initializer!=null)
                {
                    if(value!=null)
                    {
                        value=createDefaultValueCheck(value,initializer,location);
                        if(!IsSimpleCopiableExpression(initializer)&&(Ast.IsBindingPattern(target)||Ast.IsAssignmentPattern(target)))value=ensureIdentifier(value,true,location);
                    }
                    else value=initializer;
                }
                else if(value==null)value=tx.Factory().NewVoidZeroExpression();
            }
            if(isObjectBindingOrAssignmentPattern(target))flattenObjectBindingOrAssignmentPattern(element,target,value,location);
            else if(isArrayBindingOrAssignmentPattern(target))flattenArrayBindingOrAssignmentPattern(element,target,value,location);
            else emitBindingOrAssignment(this,target,value,location,element);
        }
        private void flattenObjectBindingOrAssignmentPattern(Node parent,Node pattern,Node value,TextRange location)
        {
            var elements=Ast.GetElementsOfBindingOrAssignmentPattern(pattern);var count=elements.Length;
            if(count!=1)value=ensureIdentifier(value,!Ast.IsDeclarationBindingElement(parent)||count!=0,location);
            Slice<Node> bindingElements=default,computedTemps=default;
            for(var i=0;i<elements.Length;i++)
            {
                var element=elements[i];
                if(Ast.GetRestIndicatorOfBindingOrAssignmentElement(element)==null)
                {
                    var propertyName=Ast.TryGetPropertyNameOfBindingOrAssignmentElement(element);
                    if((int)level>=(int)FlattenLevel.ObjectRest&&(element.SubtreeFacts()&(SubtreeFacts.ContainsRestOrSpread|SubtreeFacts.ContainsObjectRestOrSpread))==0&&
                        (Ast.GetTargetOfBindingOrAssignmentElement(element).SubtreeFacts()&(SubtreeFacts.ContainsRestOrSpread|SubtreeFacts.ContainsObjectRestOrSpread))==0&&!Ast.IsComputedPropertyName(propertyName))
                        bindingElements=bindingElements.Append(tx.Visitor().VisitNode(element));
                    else
                    {
                        if(bindingElements.Length>0) {emitBindingOrAssignment(this,createObjectBindingOrAssignmentPattern(this,bindingElements),value,location,pattern);bindingElements=default;}
                        var rhs=createDestructuringPropertyAccess(value,propertyName);
                        if(Ast.IsComputedPropertyName(propertyName))computedTemps=computedTemps.Append(rhs.AsElementAccessExpression().ArgumentExpression);
                        flattenBindingOrAssignmentElement(element,rhs,element.Loc,false);
                    }
                }
                else if(i==count-1)
                {
                    if(bindingElements.Length>0) {emitBindingOrAssignment(this,createObjectBindingOrAssignmentPattern(this,bindingElements),value,location,pattern);bindingElements=default;}
                    var rhs=tx.Factory().NewRestHelper(value,elements,computedTemps,pattern.Loc);flattenBindingOrAssignmentElement(element,rhs,element.Loc,false);
                }
            }
            if(bindingElements.Length>0)emitBindingOrAssignment(this,createObjectBindingOrAssignmentPattern(this,bindingElements),value,location,pattern);
        }
        private void flattenArrayBindingOrAssignmentPattern(Node parent,Node pattern,Node value,TextRange location)
        {
            var elements=Ast.GetElementsOfBindingOrAssignmentPattern(pattern);var count=elements.Length;
            if(count!=1&&((int)level<(int)FlattenLevel.ObjectRest||count==0)||Core.Every(elements,Ast.IsOmittedExpression))value=ensureIdentifier(value,!Ast.IsDeclarationBindingElement(parent)||count!=0,location);
            Slice<Node> bindingElements=default;Slice<restIdElemPair> restElements=default;
            for(var i=0;i<elements.Length;i++)
            {
                var element=elements[i];
                if((int)level>=(int)FlattenLevel.ObjectRest)
                {
                    if((element.SubtreeFacts()&SubtreeFacts.ContainsObjectRestOrSpread)!=0||hasTransformedPriorElement&&!isSimpleBindingOrAssignmentElement(element))
                    {
                        hasTransformedPriorElement=true;var temp=tx.Factory().NewTempVariable();if(hoistTempVariables)tx.EmitContext().AddVariableDeclaration(temp);
                        restElements=restElements.Append(new restIdElemPair(temp,element));bindingElements=bindingElements.Append(createArrayBindingOrAssignmentElement(this,temp));
                    }
                    else bindingElements=bindingElements.Append(element);
                }
                else if(Ast.IsOmittedExpression(element))continue;
                else if(Ast.GetRestIndicatorOfBindingOrAssignmentElement(element)==null)
                {
                    var rhs=tx.Factory().NewElementAccessExpression(value,null,tx.Factory().NewNumericLiteral(Strconv.Itoa(i),TokenFlags.None),NodeFlags.None);
                    flattenBindingOrAssignmentElement(element,rhs,element.Loc,false);
                }
                else if(i==count-1) {var rhs=tx.Factory().NewArraySliceCall(value,i);flattenBindingOrAssignmentElement(element,rhs,element.Loc,false);}
            }
            if(bindingElements.Length>0)emitBindingOrAssignment(this,createArrayBindingOrAssignmentPattern(this,bindingElements),value,location,pattern);
            foreach(var pair in restElements)flattenBindingOrAssignmentElement(pair.element,pair.id,pair.element.Loc,false);
        }
    }
    internal static bool BindingOrAssignmentElementAssignsToName(Node element,ByteStringValue name)
    {
        var target=Ast.GetTargetOfBindingOrAssignmentElement(element);if(target==null)return false;
        if(Ast.IsBindingPattern(target)||Ast.IsAssignmentPattern(target))return bindingOrAssignmentPatternAssignsToName(target,name);
        else if(Ast.IsIdentifier(target))return target.TextValue().EqualsValue(name);
        return false;
    }
    private static bool bindingOrAssignmentPatternAssignsToName(Node pattern,ByteStringValue name)
    {
        foreach(var element in Ast.GetElementsOfBindingOrAssignmentPattern(pattern))if(BindingOrAssignmentElementAssignsToName(element,name))return true;
        return false;
    }
    internal static bool BindingOrAssignmentElementContainsNonLiteralComputedName(Node element)
    {
        var propertyName=Ast.TryGetPropertyNameOfBindingOrAssignmentElement(element);
        if(propertyName!=null&&Ast.IsComputedPropertyName(propertyName)&&!Ast.IsLiteralExpression(propertyName.Expression()))return true;
        var target=Ast.GetTargetOfBindingOrAssignmentElement(element);
        return target!=null&&(Ast.IsBindingPattern(target)||Ast.IsAssignmentPattern(target))&&bindingOrAssignmentPatternContainsNonLiteralComputedName(target);
    }
    private static bool bindingOrAssignmentPatternContainsNonLiteralComputedName(Node pattern)
    {
        foreach(var element in Ast.GetElementsOfBindingOrAssignmentPattern(pattern))if(BindingOrAssignmentElementContainsNonLiteralComputedName(element))return true;
        return false;
    }
    internal static Node GetInitializerOfBindingOrAssignmentElement(Node element)
    {
        if(element==null)return null;
        if(Ast.IsDeclarationBindingElement(element))return element.Initializer();
        if(Ast.IsPropertyAssignment(element)) {var initializer=element.Initializer();return Ast.IsAssignmentExpression(initializer,true)?initializer.AsBinaryExpression().Right:null;}
        if(Ast.IsShorthandPropertyAssignment(element))return element.AsShorthandPropertyAssignment().ObjectAssignmentInitializer;
        if(Ast.IsAssignmentExpression(element,true))return element.AsBinaryExpression().Right;
        if(Ast.IsSpreadElement(element))return GetInitializerOfBindingOrAssignmentElement(element.Expression());
        return null;
    }
    private static bool isObjectBindingOrAssignmentPattern(Node node)=>node!=null&&(node.Kind==Kind.ObjectBindingPattern||node.Kind==Kind.ObjectLiteralExpression);
    private static bool isArrayBindingOrAssignmentPattern(Node node)=>node!=null&&(node.Kind==Kind.ArrayBindingPattern||node.Kind==Kind.ArrayLiteralExpression);
    private static bool isSimpleBindingOrAssignmentElement(Node element)
    {
        var target=Ast.GetTargetOfBindingOrAssignmentElement(element);if(target==null||Ast.IsOmittedExpression(target))return true;
        var propertyName=Ast.TryGetPropertyNameOfBindingOrAssignmentElement(element);if(propertyName!=null&&!Ast.IsPropertyNameLiteral(propertyName))return false;
        var initializer=GetInitializerOfBindingOrAssignmentElement(element);if(initializer!=null&&!IsSimpleInlineableExpression(initializer))return false;
        if(Ast.IsBindingPattern(target)||Ast.IsAssignmentPattern(target))return Core.Every(Ast.GetElementsOfBindingOrAssignmentPattern(target),isSimpleBindingOrAssignmentElement);
        return Ast.IsIdentifier(target);
    }
}
