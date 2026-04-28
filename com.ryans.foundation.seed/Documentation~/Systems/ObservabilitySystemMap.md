# Observability System Map

## Entry Points
- `Runtime/Diagnostics/FoundationSeedSessionLogRuntime.cs`
- `Runtime/Diagnostics/FoundationSeedLoggingConfig.cs`

## Chain
1. Logging runtime starts from bootstrap and loads config from `Resources/FoundationSeedLoggingConfig`.
2. Writes JSONL/transcript session logs.
3. Captures scene-load and optional Unity warning/error signals.
4. Trims old sessions to retention count.
5. Editor diagnostics can validate latest session evidence through `Tools/Foundation Seed/Diagnostics/Validate Foundation Setup`.
