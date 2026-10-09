# Pinned source resolution observer

Build with `bash tools/build-resolution-oracle.sh`; the binary is
`tools/.cache/resolution-oracle`. This authored NDJSON adapter calls actual pinned
Module, TsOptions, and compiler file-loader producers. Unknown request fields,
operations, modes, and represented value families fail observation explicitly.
No checker is created.

Requests use `Name`, `Cwd`, `ConfigPath`, `ConfigText`, `Files`, `Queries`, and
optional `CaseInsensitive`. Files have `Path`/`Text`, optional `PathBase64`/
`TextBase64` raw-byte roles, or a `Symlink` target. Queries have `Kind`, `Name`,
`Containing`, `Mode` (0, 1, or 99), optional raw-byte name/containing variants,
and operation-specific fields. Kinds are `module`, `directory`, `type`, `config`,
`package-directory`, `entrypoints`, and selected `utility` API observations.
`EnableDirectorySearch` is the actual entrypoint API's third argument. An
entrypoint query requires exactly one already-retained package entry; it does
not resolve/read a package to manufacture an owner.

Optional resolver inputs are `ExtraExtensions` (strings), `TypingsLocation`,
`Redirects` (Id/ConfigPath/ConfigText), and `Static` (FallbackToResolver/Entries).
Static entries retain nullable Mode pointers and nullable actual result fields;
invalid construction returns `staticErrorBase64`, with no subsequent queries.
`RawCompilerOptions` is an explicitly separate source-API constructor using
actual Unmarshal/Finalize, with numeric enum values. Ordinary config parsing
retains config spelling and all effective settings. Graph raw-option controls
call actual ParsedCommandLine.SetCompilerOptions without changing the config AST
or discovered roots; they do not claim disk-config equivalence.

`Graph:true` uses actual file-config parsing and original root/task scheduling.
ConfigPath must designate the supplied configuration file; ConfigText records
those same bytes. Graph requests do not accept resolver-only query/constructor
inputs. The reproducible source-hashed Go overlay captures actual module/type/
path results, original scheduling decisions, and retained processed-file owners.
It omits exactly the external library-reference scheduling/owned diagnostics
block; options and AST lib directives remain, with explicit unscheduled rows.
Default-library, automatic global type, project-reference task, mapper process,
and general Program validation/checker stages are outside this graph unit.
Inclusion diagnostics invoke the actual original producers through a zero-checker
Program metadata view populated from the loader's real options/host/processed
files/resolution data. Pinned production files are never modified.

Responses include all stored compiler-option fields; complete diagnostics and
owner identities; all actual result fields; ordered trace arguments and errors;
ordered host reads/lookups; and rooted repeated-result identities. PackageDirectory
trace arguments use their actual AsDirectoryPath rather than private-field
reflection. Raw strings retain role-specific base64 without path/text relocation.
Unordered package-cache keys and symlink sets are sorted before projection; traces
and search sequences retain order. Retained package entry/content aliases retain
identity, and distinct reached equal result pointers remain distinct.

This observation excludes private lazy package-version topology, lock/runtime
objects, host timestamps/Sys data, and a whole private cache graph. Graph edges
belong to retained processed files, not discarded duplicate-package parse tasks.
Typed path/virtual input controls do not establish arbitrary-byte OS filename
support. Unsupported diagnostic transformed-source repopulation has no fabricated
success representation. Final acceptance additionally requires the published
corpus, direct assertions, order controls, ordinary CLI, native execution, and
retained regression cascade.
