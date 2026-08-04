# CLAUDE.md

This file gives Claude Code (claude.ai/code) instructions for working with the code in this
repository.

It is deliberately short: only **how to build and verify**. All documentation lives in `README.md`.
It is not duplicated here.

## Required reading

**IT IS FIRST ACTION, NO EXCEPTIONS**  
The first tool call of the session must be reading README.md — before any
search, build or edit.

Read `Testing.md` and `Smoke-testing.md` **only** when editing tests or smoke tests.

## Tasks

Tasks live in `.claude/tasks/NNN-short-slug/` (`NNN` — a zero-padded number).
`task.md` is mandatory in every task folder; any other files (`plan.md`, `review.md`, …)
may be added as needed. Work with them through the `task` skill.

## Working style

- If you see a significantly better approach or a critical flaw in the
  current one, say so and wait for the user's answer before proceeding.
  Minor improvements: implement as asked, mention afterwards.
- Mark uncertain claims explicitly ("not verified", "assumption").
  Never present an unverified claim as a fact.
- If a fix does not work after 3 attempts, stop and report: what was tried,
  what the output was, what is needed from the user. Do not keep guessing.
- **Never commit and never push.** Committing and pushing are the user's job, always —
  even when the work is finished and verified. Report what changed and stop.
- The user can run the game and read the Godot log. When verification
  requires launching Godot, ask the user to run it and paste the output.

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
