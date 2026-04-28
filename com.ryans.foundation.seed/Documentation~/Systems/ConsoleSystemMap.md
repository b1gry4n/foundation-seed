# Console System Map

## Entry Point
- `Runtime/DevConsole/FoundationSeedDeveloperConsole.cs`
- `Runtime/DevConsole/FoundationSeedConsoleToggleBridge.cs`
- `Runtime/Input/FoundationSeedInputConfig.cs`

## Chain
1. Console can open/close immediately through the default backquote/tilde toggle bridge.
2. Console pauses gameplay while open.
3. Built-in commands cover logging/time status.
4. Projects can register command handlers and help providers.
5. Console keeps a command/response transcript rather than a single output line.

## Default Toggle Rule
- Default toggle is for development runtime only.
- Toggle behavior can be configured through `Resources/FoundationSeedInputConfig`.
- Projects may disable it and provide their own key/input bridge.

## Project Command Extension
- Project code can register commands with descriptions:
- `FoundationSeedDeveloperConsole.RegisterCommand("cmd", "description", args => "result")`
- External handlers can still attach through `ExternalCommandRequested`.
