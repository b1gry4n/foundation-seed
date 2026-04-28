# Time And Tick System Map

## Entry Points
- `Runtime/Time/FoundationSeedGameTime.cs`
- `Runtime/Time/FoundationSeedTimedRuntimeDriver.cs`
- `Runtime/Time/FoundationSeedTimedBehaviour.cs`
- `Runtime/Time/IFoundationSeedTimedClient.cs`
- `Runtime/Time/FoundationSeedTimedClientBase.cs`

## Chain
1. `FoundationSeedGameTime` owns gameplay clock and reason-based pause.
2. Timed driver computes gameplay/presentation deltas.
3. Timed behaviours receive centralized callbacks instead of raw per-script Unity loops.
4. Plain C# timed clients can register with the same driver through `IFoundationSeedTimedClient`.
