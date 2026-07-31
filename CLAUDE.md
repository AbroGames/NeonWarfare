# CLAUDE.md

This file gives Claude Code (claude.ai/code) instructions for working with the code in this
repository.

It is deliberately short: only **how to build and verify**. All documentation lives in `README.md`.
It is not duplicated here.

## Required reading

Read [README.md](README.md) at the start of work.

## Commands

There are only unit tests, which do not launch Godot. Everything that lives inside the node tree is
verified by compilation plus a manual run of the game.

```bash
dotnet build                              # quick compilation check (~3 s)
dotnet test                               # unit tests, including documentation and code-style checks
"$GODOT_EXE" --path "./"                  # normal game launch
"$GODOT_EXE" --path "./" --auto-start     # straight into a single-player game, skipping the menu
"$GODOT_EXE" --path "./" --server         # straight to launching a dedicated server
"$GODOT_EXE" --path "./" --auto-connect   # straight to connecting to a dedicated server
```

**Build noise that does not need fixing:** `CS0649`, suppressed in `.csproj` (these fields are
filled through DI, not through a constructor).

## Shared libraries

The `KLUDGEBOX_SRC` environment variable points to the source code of `KludgeBox` — our own shared
library used across several Godot projects.
Consult it when you need to see how a `KludgeBox` API is implemented.

## Language

Code and documentation (comments, `Docs/`, `README.md`, identifiers, string keys) — in English.  
Chat replies, code reviews and plans — in Russian.
