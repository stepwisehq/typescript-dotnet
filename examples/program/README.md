# Program environment example

Run `tscheck --inspect-program examples/program/tsconfig.json` to load the genuine ES2022 library closure, explicit application type package, triple-slash path source and project-reference configuration. The output describes Program construction; semantic checking is unrequested.

Run the same command with `tsconfig-invalid.json` to observe the original missing-path diagnostic. The helper reference is metadata and source/output mapping; this command does not build its declaration output. The `program-demo` declaration is an application fixture, not a replacement for a Node, React or standard-library environment.
