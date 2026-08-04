---
name: task
description: Manage tasks stored in .claude/tasks/. Use when the user asks to create a new task, write a plan for a task, start/continue work on a task, or code-review a finished task — typically phrased as "task 3", "задача 3", "новая задача", "план для задачи N", "ревью задачи N".
---

# Task workflow

Tasks live in `.claude/tasks/NNN-short-slug/`, where `NNN` is a zero-padded number.
`task.md` is mandatory; `plan.md` and `review.md` are produced by this skill.
Any other files may be added freely.

## Resolving a task

A task is referenced by its number. Find its folder with `ls .claude/tasks/ | grep '^NNN'`.
If the number does not exist and the user did not ask to create it — stop and say so.

## Modes

The user's phrasing selects one of four modes. If it is ambiguous, ask.

### 1. New task

Trigger: "create a task ...".

1. `ls .claude/tasks/` → next free number = max + 1, zero-padded to 3 digits.
2. Pick a short English kebab-case slug from the description.
3. Create `.claude/tasks/NNN-slug/task.md`:

```markdown
---
status: todo
---

# NNN — <Title>

## Goal

<What must be achieved, in the user's words, cleaned up.>

## Done when

- <verifiable criterion>
```

Do **not** start working on it. Report the created number and path.

### 2. Plan

Trigger: "plan task N".

Read `task.md`, read the docs `README.md` points to for this area, explore the
relevant code, then write `plan.md` next to `task.md`:

```markdown
# Plan — NNN <Title>

## Approach

<1–2 paragraphs: the chosen approach and why.>

## Steps

1. `path/to/File.cs` — <what changes>
2. ...

## Risks / open questions

- <anything the user must decide>
```

Set `status: planned` in `task.md`. Do not write production code in this mode.
If there are open questions, ask them in chat instead of guessing.

### 3. Do

Trigger: "start task N", "continue task N".

1. Read `task.md`, and `plan.md` if it exists. If there is no plan and the task is
   non-trivial, propose making one first.
2. Set `status: in-progress`.
3. Implement. Follow `CLAUDE.md` and the `Docs/` files relevant to the area.
4. Verify: `dotnet build`, and `dotnet test` if anything testable changed.
   Anything that only shows up inside the node tree — ask the user to run the game.
5. If the change affects the architecture, update the matching file in `Docs/` (and
   `README.md` if the list of documents or the entry points changed). Keep the doc
   edit as short as possible.
6. Append a `## Result` section to `task.md`: what was changed, what was verified and
   how, what was left out. Once the build passes, set `status: review`.

**You must not set `status: done`, and you must not review your own work.** After
`review` is set, tell the user the task is ready for review and stop.

### 4. Review

Trigger: "review task N".

**The review is always done by a different agent than the one that wrote the code.**
If this session implemented the task, do not review it inline — spawn a subagent
(`Agent` tool, `subagent_type: general-purpose`, run synchronously) with a
self-contained prompt: the task number and folder path, the instruction to read
`README.md` first per `CLAUDE.md`, and the review contract below. Only a session that
did not write the code may review it directly.

Review the work done for the task — the diff, plus the code around it — against
`task.md`'s "Done when" and against `Docs/`. Check specifically that:

- the code satisfies every "Done when" criterion;
- the architectural changes are reflected in `Docs/` (and `README.md` if the document
  list or the entry points changed) — a missing doc update is a finding;
- those doc edits are as short as they can be — bloat is a finding too.

Write `review.md`:

```markdown
# Review — NNN <Title>

## Verdict

<ok | needs work>

## Findings

### <severity: critical | major | minor> — <short title>

`path/File.cs:42` — <what is wrong, why it matters, what to do.>
```

Findings only — do not fix anything in this mode unless the user asks.
Review text is in Russian, per `CLAUDE.md`.

The reviewer sets the final status: `done` on an `ok` verdict, back to `in-progress`
on `needs work`. This is the only mode allowed to set `done`.

## Status values

`todo` → `planned` → `in-progress` → `review` → `done`, with `review` → `in-progress`
when the review asks for changes. Keep the frontmatter accurate; it is the only place
the state is recorded.
