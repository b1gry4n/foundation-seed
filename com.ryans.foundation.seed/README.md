# Foundation Seed Package

Reusable Unity starter foundation for new projects.

## Includes
- Runtime bootstrap and service initialization
- Gameplay time + reason-based pause authority
- Timed-behaviour driver seam for project-owned ticking
- Session logging and trace runtime with retention policy
- Runtime dev console with extensible command hooks
- Default development console toggle bridge on backquote/tilde (works immediately in Play Mode)
- Generic input action router seam
- Save/load service seam with provider contracts
- Development-gate helpers
- Editor setup tooling
- Quick/full validation profiles
- Cleanup audit and decommission ledger scaffold
- Boundary guardrails for plant/foundation separation
- System-map freshness checks
- ADR-lite decision scaffold
- Optional intent logging stream
- Doctrine + system maps in `Documentation~`
- Runtime/editor tests

## Non-Goals
- Game-specific mechanics
- Project-specific design doctrine
- Any project theme/content schema
- Default gameplay input bindings

## Drop-In Usage
1. Copy folder to `Packages/com.your.foundation.seed` in a new Unity project.
2. Open Unity and let assemblies compile.
3. Run `Tools/Foundation Seed/Setup/Run Initial Setup` to create runtime + logging + plant scaffold assets.
4. Press Play once to initialize runtime bootstrap, then run `Tools/Foundation Seed/Diagnostics/Validate Foundation Setup`.
5. Use `Tools/Foundation Seed/Docs/Copy AGENTS Foundation Setup Blurb` and paste into project-root `AGENTS.md`.
6. Use scaffolded project-owned plant root under `Assets/Plant/`:
   - `Assets/Plant/Runtime/` for runtime scripts
   - `Assets/Plant/Editor/` for editor scripts
   - `Assets/Plant/Doctrine/` for entry doctrine and mapping doctrine
   - `Assets/Plant/SystemMap/` for full-project map index/maps
   - `Assets/Plant/Design/` for deeper design doctrine/systems/workflows

## Codex Logging
- Foundation includes an editor-side codex change logger that tracks recent asset changes.
- Config asset: `Assets/Plant/Resources/FoundationSeedCodexLoggingConfig.asset`.
- Default retention cap: 500 entries.
- Reveal log: `Tools/Foundation Seed/Diagnostics/Reveal Codex Change Log`.
- Optional intent log config: `Assets/Plant/Resources/FoundationSeedIntentLoggingConfig.asset`.
- Intent entries can be written from `Tools/Foundation Seed/Diagnostics/Write Intent Entry`.

## Extension Model
Projects should branch out through composition and adapters:
- Add project code in project-owned assemblies.
- Subscribe to input/console/trace seams.
- Register save providers.
- Build project timing policy on top of foundation time/timed driver.
- Override or disable default console toggle with:
  - `FoundationSeedConsoleToggleBridge.EnableDefaultBackquoteToggle = false`
  - `Assets/Plant/Resources/FoundationSeedInputConfig.asset`
- Register project commands from project code:
  - `FoundationSeedDeveloperConsole.RegisterCommand("mycmd", "does thing", args => "ok");`
- Hook plain C# systems into foundation time:
  - implement `IFoundationSeedTimedClient`, or inherit `FoundationSeedTimedClientBase`
  - register via `FoundationSeedTimedRuntimeDriver.EnsureInstance().RegisterClient(client)`

Do not modify core package scripts for project behavior.

## Client-Safe Export
- Use `Tools/Foundation Seed/Release/Export Client-Safe Package` to generate a stripped copy for handoff.
- Export removes non-runtime handoff layers while keeping runtime/editor core:
  - `Documentation~/`
  - `OperatorSetup~/`
  - `Tests/`
  - `AGENTS.md`

## Governance Layer
- `Assets/Plant/Doctrine/CleanupDoctrine.md` defines deprecation -> sunset -> removal flow.
- `Assets/Plant/SystemMap/DeprecatedSystems.md` records phased-out systems and evidence.
- `Assets/Plant/Doctrine/Decisions/ADR-Template.md` provides ADR-lite decision tracking.
- Validation commands:
  - `Tools/Foundation Seed/Diagnostics/Validate Foundation Setup`
