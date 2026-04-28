# Human Setup Guide (Operator-Only)

Audience: human operators and technical leads.

Purpose: step-by-step setup for adding this seed package to a new Unity project.

Codex rule: this folder should be excluded from Codex startup-read order and active scanning unless a human explicitly asks for it.

## Setup Steps
1. Copy this package folder into your new project under `Packages/com.your.foundation.seed`.
2. Open Unity and let assemblies compile.
3. Run foundation setup via menu:
   `Tools/Foundation Seed/Setup/Run Initial Setup`
4. Confirm config assets exist:
   - `Assets/Plant/Resources/FoundationSeedLoggingConfig.asset`
   - `Assets/Plant/Resources/FoundationSeedInputConfig.asset`
   - `Assets/Plant/Resources/FoundationSeedCodexLoggingConfig.asset`
   - `Assets/Plant/Resources/FoundationSeedIntentLoggingConfig.asset`
5. Confirm plant scaffold exists:
   - `Assets/Plant/Runtime/`
   - `Assets/Plant/Editor/`
   - `Assets/Plant/Doctrine/`
   - `Assets/Plant/SystemMap/`
   - `Assets/Plant/Design/Doctrine/`
   - `Assets/Plant/Design/Systems/`
   - `Assets/Plant/Design/Workflows/`
6. Copy AGENTS starter blurb:
   `Tools/Foundation Seed/Docs/Copy AGENTS Foundation Setup Blurb`
7. Confirm runtime services initialize in Play Mode:
   - `FoundationSeedBootstrapRoot`
   - `FoundationSeedGameTime`
   - `FoundationSeedTimedRuntimeDriver`
   - `FoundationSeedSessionLogRuntime`
   - `FoundationSeedInputRouter`
   - `FoundationSeedSaveService`
   - `FoundationSeedDeveloperConsole`
8. Run validation:
   - `Tools/Foundation Seed/Diagnostics/Validate Foundation Setup`
9. Add project-owned adapters (do not modify package internals):
   - input binding adapter -> `FoundationSeedInputRouter`
   - optional console toggle override (default backquote/tilde works in development runtime)
   - project save providers -> `FoundationSeedSaveService.RegisterProvider(...)`
10. In plant-level work, keep runtime code in `Assets/Plant/Runtime` and editor tooling in `Assets/Plant/Editor`.
11. Keep project design intent in `Assets/Plant/Design/Doctrine`.
12. Create project docs scaffold (`_ProjectDocs/*`) and project `AGENTS.md` if your project also uses a root docs layer.
13. In `AGENTS.md`, route Codex startup through foundation doctrine/system docs first, then plant/project docs.

## AGENTS Snippet (Copy/Paste)
Preferred: use `Tools/Foundation Seed/Docs/Copy AGENTS Foundation Setup Blurb`.

Manual fallback:

```md
## Foundation Startup Read Order
1. AGENTS.md (project root above Assets/)
2. Packages/com.your.foundation.seed/Documentation~/Doctrine/CoreDoctrine.md
3. Packages/com.your.foundation.seed/Documentation~/Doctrine/ObservabilityDoctrine.md
4. Packages/com.your.foundation.seed/Documentation~/Doctrine/PackageStructureDoctrine.md
5. Packages/com.your.foundation.seed/Documentation~/Doctrine/SystemMapDoctrine.md
6. Packages/com.your.foundation.seed/Documentation~/Systems/BootstrapSystemMap.md
7. Packages/com.your.foundation.seed/Documentation~/Systems/TimeAndTickSystemMap.md
8. Packages/com.your.foundation.seed/Documentation~/Systems/ObservabilitySystemMap.md
9. Packages/com.your.foundation.seed/Documentation~/Systems/ConsoleSystemMap.md
10. Packages/com.your.foundation.seed/Documentation~/Systems/InputSystemMap.md
11. Packages/com.your.foundation.seed/Documentation~/Systems/SaveSystemMap.md
12. Assets/Plant/Doctrine/EntryDoctrine.md
13. Assets/Plant/Doctrine/SystemMappingDoctrine.md
14. Assets/Plant/SystemMap/SystemMapIndex.md
15. Assets/Plant/Design/Doctrine/*
16. Assets/Plant/Design/Systems/*

## Codex Ignore Layer
- Ignore operator-only docs under:
  - Packages/com.your.foundation.seed/OperatorSetup~/
- Do not include operator-only docs in default startup read order.
- Only read operator-only docs when a human explicitly asks.

## System Map Rule
- Update Assets/Plant/SystemMap/* in the same pass when systems are added/changed.
```

## Boundary Reminder
- Foundation package = reusable infrastructure seed.
- New project behavior = project-owned code and project docs.
- Never move project-specific gameplay/design truth into this package.

## Cleanup And Decommission
- Use `Assets/Plant/Doctrine/CleanupDoctrine.md` as process source.
- Log phase-outs in `Assets/Plant/SystemMap/DeprecatedSystems.md` before removal.
- Keep `RemovedDateUtc` and evidence updated for every decommission.

## Client Handoff Workflow
1. Finish prototype work using full seed package.
2. Run `Tools/Foundation Seed/Release/Export Client-Safe Package`.
3. Hand off the exported package copy, not your working seed source.
4. Verify the exported copy excludes:
   - `OperatorSetup~/`
   - `Documentation~/`
   - `Tests/`
   - `AGENTS.md`
