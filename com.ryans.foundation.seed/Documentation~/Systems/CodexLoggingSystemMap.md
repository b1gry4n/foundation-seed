# Codex Logging System Map

## Purpose
- Keep a bounded rolling log of recent project asset changes to support debugging and change-trace workflows.
- Provide a fast recent-change surface without scanning the full project on every prompt.

## Entry Points
- `Editor/Diagnostics/FoundationSeedCodexLoggingConfig.cs`
- `Editor/Diagnostics/FoundationSeedCodexChangeLogger.cs`

## Runtime Chain (Editor)
1. Asset pipeline callbacks emit imported/deleted/moved file events.
2. Events append to a project-root jsonl file under `PlantLogs/CodexLogs/` by default.
3. Log retention trims to configured cap (default 500 entries).

## Ownership
- Codex logging config owns enablement and retention policy.
- Codex change logger owns event write/trim behavior.

## Update Triggers
- Change retention rules.
- Add/remove tracked change kinds.
- Change codex log output location.
