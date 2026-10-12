// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
namespace TypeScript.DiagnosticPrinting;
internal sealed class Priority { internal int Value; internal Priority(int value) => Value = value; }
internal sealed class EmitHelper
{
    internal string Name, Text, ImportName;
    internal bool Scoped;
    internal Func<Func<string, string>, string> TextCallback;
    internal Priority Priority;
    internal Slice<EmitHelper> Dependencies;
}
internal static class EmitHelpers
{
    internal static int compareEmitHelpers(EmitHelper x, EmitHelper y)
    {
        if (x == y || x.Priority == y.Priority) return 0;
        if (x.Priority == null) return 1;
        if (y.Priority == null) return -1;
        return x.Priority.Value - y.Priority.Value;
    }
    internal static readonly EmitHelper decorateHelper = new()
    {
        Name = "typescript:decorate",
        ImportName = "__decorate",
        Scoped = false,
        Priority = new(2),
        Text = "var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {\n    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;\n    if (typeof Reflect === \"object\" && typeof Reflect.decorate === \"function\") r = Reflect.decorate(decorators, target, key, desc);\n    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;\n    return c > 3 && r && Object.defineProperty(target, key, r), r;\n};",
    };
    internal static readonly EmitHelper metadataHelper = new()
    {
        Name = "typescript:metadata",
        ImportName = "__metadata",
        Scoped = false,
        Priority = new(3),
        Text = "var __metadata = (this && this.__metadata) || function (k, v) {\n    if (typeof Reflect === \"object\" && typeof Reflect.metadata === \"function\") return Reflect.metadata(k, v);\n};",
    };
    internal static readonly EmitHelper paramHelper = new()
    {
        Name = "typescript:param",
        ImportName = "__param",
        Scoped = false,
        Priority = new(4),
        Text = "var __param = (this && this.__param) || function (paramIndex, decorator) {\n    return function (target, key) { decorator(target, key, paramIndex); }\n};",
    };
    internal static readonly EmitHelper addDisposableResourceHelper = new()
    {
        Name = "typescript:addDisposableResource",
        ImportName = "__addDisposableResource",
        Scoped = false,
        Text = "var __addDisposableResource = (this && this.__addDisposableResource) || function (env, value, async) {\n    if (value !== null && value !== void 0) {\n        if (typeof value !== \"object\" && typeof value !== \"function\") throw new TypeError(\"Object expected.\");\n        var dispose, inner;\n        if (async) {\n            if (!Symbol.asyncDispose) throw new TypeError(\"Symbol.asyncDispose is not defined.\");\n            dispose = value[Symbol.asyncDispose];\n        }\n        if (dispose === void 0) {\n            if (!Symbol.dispose) throw new TypeError(\"Symbol.dispose is not defined.\");\n            dispose = value[Symbol.dispose];\n            if (async) inner = dispose;\n        }\n        if (typeof dispose !== \"function\") throw new TypeError(\"Object not disposable.\");\n        if (inner) dispose = function() { try { inner.call(this); } catch (e) { return Promise.reject(e); } };\n        env.stack.push({ value: value, dispose: dispose, async: async });\n    }\n    else if (async) {\n        env.stack.push({ async: true });\n    }\n    return value;\n};",
    };
    internal static readonly EmitHelper disposeResourcesHelper = new()
    {
        Name = "typescript:disposeResources",
        ImportName = "__disposeResources",
        Scoped = false,
        Text = "var __disposeResources = (this && this.__disposeResources) || (function (SuppressedError) {\n    return function (env) {\n        function fail(e) {\n            env.error = env.hasError ? new SuppressedError(e, env.error, \"An error was suppressed during disposal.\") : e;\n            env.hasError = true;\n        }\n        var r, s = 0;\n        function next() {\n            while (r = env.stack.pop()) {\n                try {\n                    if (!r.async && s === 1) return s = 0, env.stack.push(r), Promise.resolve().then(next);\n                    if (r.dispose) {\n                        var result = r.dispose.call(r.value);\n                        if (r.async) return s |= 2, Promise.resolve(result).then(next, function(e) { fail(e); return next(); });\n                    }\n                    else s |= 1;\n                }\n                catch (e) {\n                    fail(e);\n                }\n            }\n            if (s === 1) return env.hasError ? Promise.reject(env.error) : Promise.resolve();\n            if (env.hasError) throw env.error;\n        }\n        return next();\n    };\n})(typeof SuppressedError === \"function\" ? SuppressedError : function (error, suppressed, message) {\n    var e = new Error(message);\n    return e.name = \"SuppressedError\", e.error = error, e.suppressed = suppressed, e;\n});",
    };
    internal static readonly EmitHelper classPrivateFieldGetHelper = new()
    {
        Name = "typescript:classPrivateFieldGet",
        ImportName = "__classPrivateFieldGet",
        Scoped = false,
        Text = "var __classPrivateFieldGet = (this && this.__classPrivateFieldGet) || function (receiver, state, kind, f) {\n    if (kind === \"a\" && !f) throw new TypeError(\"Private accessor was defined without a getter\");\n    if (typeof state === \"function\" ? receiver !== state || !f : !state.has(receiver)) throw new TypeError(\"Cannot read private member from an object whose class did not declare it\");\n    return kind === \"m\" ? f : kind === \"a\" ? f.call(receiver) : f ? f.value : state.get(receiver);\n};",
    };
    internal static readonly EmitHelper classPrivateFieldSetHelper = new()
    {
        Name = "typescript:classPrivateFieldSet",
        ImportName = "__classPrivateFieldSet",
        Scoped = false,
        Text = "var __classPrivateFieldSet = (this && this.__classPrivateFieldSet) || function (receiver, state, value, kind, f) {\n    if (kind === \"m\") throw new TypeError(\"Private method is not writable\");\n    if (kind === \"a\" && !f) throw new TypeError(\"Private accessor was defined without a setter\");\n    if (typeof state === \"function\" ? receiver !== state || !f : !state.has(receiver)) throw new TypeError(\"Cannot write private member to an object whose class did not declare it\");\n    return (kind === \"a\" ? f.call(receiver, value) : f ? f.value = value : state.set(receiver, value)), value;\n};",
    };
    internal static readonly EmitHelper classPrivateFieldInHelper = new()
    {
        Name = "typescript:classPrivateFieldIn",
        ImportName = "__classPrivateFieldIn",
        Scoped = false,
        Text = "var __classPrivateFieldIn = (this && this.__classPrivateFieldIn) || function(state, receiver) {\n    if (receiver === null || (typeof receiver !== \"object\" && typeof receiver !== \"function\")) throw new TypeError(\"Cannot use 'in' operator on non-object\");\n    return typeof state === \"function\" ? receiver === state : state.has(receiver);\n};",
    };
    internal static readonly EmitHelper awaitHelper = new()
    {
        Name = "typescript:await",
        ImportName = "__await",
        Scoped = false,
        Text = "var __await = (this && this.__await) || function (v) { return this instanceof __await ? (this.v = v, this) : new __await(v); }",
    };
    internal static readonly EmitHelper asyncGeneratorHelper = new()
    {
        Name = "typescript:asyncGenerator",
        ImportName = "__asyncGenerator",
        Scoped = false,
        Dependencies = Slice<EmitHelper>.FromArray([awaitHelper]),
        Text = "var __asyncGenerator = (this && this.__asyncGenerator) || function (thisArg, _arguments, generator) {\n    if (!Symbol.asyncIterator) throw new TypeError(\"Symbol.asyncIterator is not defined.\");\n    var g = generator.apply(thisArg, _arguments || []), i, q = [];\n    return i = Object.create((typeof AsyncIterator === \"function\" ? AsyncIterator : Object).prototype), verb(\"next\"), verb(\"throw\"), verb(\"return\", awaitReturn), i[Symbol.asyncIterator] = function () { return this; }, i;\n    function awaitReturn(f) { return function (v) { return Promise.resolve(v).then(f, reject); }; }\n    function verb(n, f) { if (g[n]) { i[n] = function (v) { return new Promise(function (a, b) { q.push([n, v, a, b]) > 1 || resume(n, v); }); }; if (f) i[n] = f(i[n]); } }\n    function resume(n, v) { try { step(g[n](v)); } catch (e) { settle(q[0][3], e); } }\n    function step(r) { r.value instanceof __await ? Promise.resolve(r.value.v).then(fulfill, reject) : settle(q[0][2], r); }\n    function fulfill(value) { resume(\"next\", value); }\n    function reject(value) { resume(\"throw\", value); }\n    function settle(f, v) { if (f(v), q.shift(), q.length) resume(q[0][0], q[0][1]); }\n};",
    };
    internal static readonly EmitHelper asyncDelegatorHelper = new()
    {
        Name = "typescript:asyncDelegator",
        ImportName = "__asyncDelegator",
        Scoped = false,
        Dependencies = Slice<EmitHelper>.FromArray([awaitHelper]),
        Text = "var __asyncDelegator = (this && this.__asyncDelegator) || function (o) {\n    var i, p;\n    return i = {}, verb(\"next\"), verb(\"throw\", function (e) { throw e; }), verb(\"return\"), i[Symbol.iterator] = function () { return this; }, i;\n    function verb(n, f) { i[n] = o[n] ? function (v) { return (p = !p) ? { value: __await(o[n](v)), done: false } : f ? f(v) : v; } : f; }\n};",
    };
    internal static readonly EmitHelper asyncValuesHelper = new()
    {
        Name = "typescript:asyncValues",
        ImportName = "__asyncValues",
        Scoped = false,
        Text = "var __asyncValues = (this && this.__asyncValues) || function (o) {\n    if (!Symbol.asyncIterator) throw new TypeError(\"Symbol.asyncIterator is not defined.\");\n    var m = o[Symbol.asyncIterator], i;\n    return m ? m.call(o) : (o = typeof __values === \"function\" ? __values(o) : o[Symbol.iterator](), i = {}, verb(\"next\"), verb(\"throw\"), verb(\"return\"), i[Symbol.asyncIterator] = function () { return this; }, i);\n    function verb(n) { i[n] = o[n] && function (v) { return new Promise(function (resolve, reject) { v = o[n](v), settle(resolve, reject, v.done, v.value); }); }; }\n    function settle(resolve, reject, d, v) { Promise.resolve(v).then(function(v) { resolve({ value: v, done: d }); }, reject); }\n};",
    };
    internal static readonly EmitHelper restHelper = new()
    {
        Name = "typescript:rest",
        ImportName = "__rest",
        Scoped = false,
        Text = "var __rest = (this && this.__rest) || function (s, e) {\n    var t = {};\n    for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p) && e.indexOf(p) < 0)\n        t[p] = s[p];\n    if (s != null && typeof Object.getOwnPropertySymbols === \"function\")\n        for (var i = 0, p = Object.getOwnPropertySymbols(s); i < p.length; i++) {\n            if (e.indexOf(p[i]) < 0 && Object.prototype.propertyIsEnumerable.call(s, p[i]))\n                t[p[i]] = s[p[i]];\n        }\n    return t;\n};",
    };
    internal static readonly EmitHelper awaiterHelper = new()
    {
        Name = "typescript:awaiter",
        ImportName = "__awaiter",
        Scoped = false,
        Priority = new(5),
        Text = "var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {\n    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }\n    return new (P || (P = Promise))(function (resolve, reject) {\n        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }\n        function rejected(value) { try { step(generator[\"throw\"](value)); } catch (e) { reject(e); } }\n        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }\n        step((generator = generator.apply(thisArg, _arguments || [])).next());\n    });\n};",
    };
    internal static readonly EmitHelper AsyncSuperHelper = new()
    {
        Name = "typescript:async-super",
        Scoped = true,
        TextCallback = makeUniqueName => "\nconst " + makeUniqueName("_superIndex") + " = name => super[name];",
    };
    internal static readonly EmitHelper AdvancedAsyncSuperHelper = new()
    {
        Name = "typescript:advanced-async-super",
        Scoped = true,
        TextCallback = makeUniqueName => "\nconst " + makeUniqueName("_superIndex") + " = (function (geti, seti) {\n" +
			"    const cache = Object.create(null);\n" +
			"    return name => cache[name] || (cache[name] = { get value() { return geti(name); }, set value(v) { seti(name, v); } });\n" +
			"})(name => super[name], (name, value) => super[name] = value);",
    };
    internal static readonly EmitHelper esDecorateHelper = new()
    {
        Name = "typescript:esDecorate",
        ImportName = "__esDecorate",
        Scoped = false,
        Priority = new(2),
        Text = "var __esDecorate = (this && this.__esDecorate) || function (ctor, descriptorIn, decorators, contextIn, initializers, extraInitializers) {\n    function accept(f) { if (f !== void 0 && typeof f !== \"function\") throw new TypeError(\"Function expected\"); return f; }\n    var kind = contextIn.kind, key = kind === \"getter\" ? \"get\" : kind === \"setter\" ? \"set\" : \"value\";\n    var target = !descriptorIn && ctor ? contextIn[\"static\"] ? ctor : ctor.prototype : null;\n    var descriptor = descriptorIn || (target ? Object.getOwnPropertyDescriptor(target, contextIn.name) : {});\n    var _, done = false;\n    for (var i = decorators.length - 1; i >= 0; i--) {\n        var context = {};\n        for (var p in contextIn) context[p] = p === \"access\" ? {} : contextIn[p];\n        for (var p in contextIn.access) context.access[p] = contextIn.access[p];\n        context.addInitializer = function (f) { if (done) throw new TypeError(\"Cannot add initializers after decoration has completed\"); extraInitializers.push(accept(f || null)); };\n        var result = (0, decorators[i])(kind === \"accessor\" ? { get: descriptor.get, set: descriptor.set } : descriptor[key], context);\n        if (kind === \"accessor\") {\n            if (result === void 0) continue;\n            if (result === null || typeof result !== \"object\") throw new TypeError(\"Object expected\");\n            if (_ = accept(result.get)) descriptor.get = _;\n            if (_ = accept(result.set)) descriptor.set = _;\n            if (_ = accept(result.init)) initializers.unshift(_);\n        }\n        else if (_ = accept(result)) {\n            if (kind === \"field\") initializers.unshift(_);\n            else descriptor[key] = _;\n        }\n    }\n    if (target) Object.defineProperty(target, contextIn.name, descriptor);\n    done = true;\n};",
    };
    internal static readonly EmitHelper runInitializersHelper = new()
    {
        Name = "typescript:runInitializers",
        ImportName = "__runInitializers",
        Scoped = false,
        Priority = new(2),
        Text = "var __runInitializers = (this && this.__runInitializers) || function (thisArg, initializers, value) {\n    var useValue = arguments.length > 2;\n    for (var i = 0; i < initializers.length; i++) {\n        value = useValue ? initializers[i].call(thisArg, value) : initializers[i].call(thisArg);\n    }\n    return useValue ? value : void 0;\n};",
    };
    internal static readonly EmitHelper makeTemplateObjectHelper = new()
    {
        Name = "typescript:makeTemplateObject",
        ImportName = "__makeTemplateObject",
        Scoped = false,
        Priority = new(0),
        Text = "var __makeTemplateObject = (this && this.__makeTemplateObject) || function (cooked, raw) {\n    if (Object.defineProperty) { Object.defineProperty(cooked, \"raw\", { value: raw }); } else { cooked.raw = raw; }\n    return cooked;\n};",
    };
    internal static readonly EmitHelper propKeyHelper = new()
    {
        Name = "typescript:propKey",
        ImportName = "__propKey",
        Scoped = false,
        Text = "var __propKey = (this && this.__propKey) || function (x) {\n    return typeof x === \"symbol\" ? x : \"\".concat(x);\n};",
    };
    internal static readonly EmitHelper setFunctionNameHelper = new()
    {
        Name = "typescript:setFunctionName",
        ImportName = "__setFunctionName",
        Scoped = false,
        Text = "var __setFunctionName = (this && this.__setFunctionName) || function (f, name, prefix) {\n    if (typeof name === \"symbol\") name = name.description ? \"[\".concat(name.description, \"]\") : \"\";\n    return Object.defineProperty(f, \"name\", { configurable: true, value: prefix ? \"\".concat(prefix, \" \", name) : name });\n};",
    };
    internal static readonly EmitHelper createBindingHelper = new()
    {
        Name = "typescript:commonjscreatebinding",
        ImportName = "__createBinding",
        Scoped = false,
        Priority = new(1),
        Text = "var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {\n    if (k2 === undefined) k2 = k;\n    var desc = Object.getOwnPropertyDescriptor(m, k);\n    if (!desc || (\"get\" in desc ? !m.__esModule : desc.writable || desc.configurable)) {\n      desc = { enumerable: true, get: function() { return m[k]; } };\n    }\n    Object.defineProperty(o, k2, desc);\n}) : (function(o, m, k, k2) {\n    if (k2 === undefined) k2 = k;\n    o[k2] = m[k];\n}));",
    };
    internal static readonly EmitHelper setModuleDefaultHelper = new()
    {
        Name = "typescript:commonjscreatevalue",
        ImportName = "__setModuleDefault",
        Scoped = false,
        Priority = new(1),
        Text = "var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {\n    Object.defineProperty(o, \"default\", { enumerable: true, value: v });\n}) : function(o, v) {\n    o[\"default\"] = v;\n});",
    };
    internal static readonly EmitHelper importStarHelper = new()
    {
        Name = "typescript:commonjsimportstar",
        ImportName = "__importStar",
        Scoped = false,
        Priority = new(2),
        Dependencies = Slice<EmitHelper>.FromArray([createBindingHelper, setModuleDefaultHelper]),
        Text = "var __importStar = (this && this.__importStar) || (function () {\n    var ownKeys = function(o) {\n        ownKeys = Object.getOwnPropertyNames || function (o) {\n            var ar = [];\n            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;\n            return ar;\n        };\n        return ownKeys(o);\n    };\n    return function (mod) {\n        if (mod && mod.__esModule) return mod;\n        var result = {};\n        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== \"default\") __createBinding(result, mod, k[i]);\n        __setModuleDefault(result, mod);\n        return result;\n    };\n})();",
    };
    internal static readonly EmitHelper importDefaultHelper = new()
    {
        Name = "typescript:commonjsimportdefault",
        ImportName = "__importDefault",
        Scoped = false,
        Text = "var __importDefault = (this && this.__importDefault) || function (mod) {\n    return (mod && mod.__esModule) ? mod : { \"default\": mod };\n};",
    };
    internal static readonly EmitHelper exportStarHelper = new()
    {
        Name = "typescript:export-star",
        ImportName = "__exportStar",
        Scoped = false,
        Priority = new(2),
        Dependencies = Slice<EmitHelper>.FromArray([createBindingHelper]),
        Text = "var __exportStar = (this && this.__exportStar) || function(m, exports) {\n    for (var p in m) if (p !== \"default\" && !Object.prototype.hasOwnProperty.call(exports, p)) __createBinding(exports, m, p);\n};",
    };
    internal static readonly EmitHelper rewriteRelativeImportExtensionsHelper = new()
    {
        Name = "typescript:rewriteRelativeImportExtensions",
        ImportName = "__rewriteRelativeImportExtension",
        Scoped = false,
        Text = "var __rewriteRelativeImportExtension = (this && this.__rewriteRelativeImportExtension) || function (path, preserveJsx) {\n    if (typeof path === \"string\" && /^\\.\\.?\\//.test(path)) {\n        return path.replace(/\\.(tsx)$|((?:\\.d)?)((?:\\.[^./]+?)?)\\.([cm]?)ts$/i, function (m, tsx, d, ext, cm) {\n            return tsx ? preserveJsx ? \".jsx\" : \".js\" : d && (!ext || !cm) ? m : (d + ext + \".\" + cm.toLowerCase() + \"js\");\n        });\n    }\n    return path;\n};",
    };
}
