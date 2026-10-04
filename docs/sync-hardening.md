# Experimental sync hardening

Based on upstream `dev` commit `e760d3018495589ded3ecb21901705c47deb4a2c`.
This branch fixes two diagnostic failure paths. It does not certify arbitrary mods or promise zero desyncs.

## Changes

- Address-cache growth now reserves the new slot in the replacement array. Previously the insertion that triggered growth wrote into the old, still valid managed array and was lost from the active cache. This is a cache correctness/performance bug, not proof of a simulation desync or unmanaged use-after-free.
- A managed exception from native trace collection no longer aborts the caller after an RNG draw. The failed pooled item is returned; tracing stops for that game and records an error. A new game retries tracing.
- After a trace failure, the incomplete current trace hash sequence is cleared and future trace hashes are omitted for that coordinator. This intentionally loses trace-based comparison, preventing partial diagnostic data from reporting a false mismatch. Existing map, world, command RNG and map-identity checks remain unchanged. This is not a repair for a genuinely diverged simulation. Native access violations/process crashes are outside the managed exception guarantee.

The upstream base already includes quest enumeration snapshots, gravship reachability RNG isolation, a synchronized map context for launch cancellation, and an ARM64 tracer guard. These are upstream changes, not new work in this fork.

## Engineering validation

Use .NET 10 for the current source generator and install the .NET 8 runtime for tests. The inherited .NET 9 configuration cannot load the current Roslyn 5.3 generator on the tested machine. The new `Sync hardening` workflow builds and tests on Linux, Windows and macOS without publishing a release.

`AddrTableTest` exercises 6,000 inserted addresses, growth boundaries, immediate lookup, payload retention and entry counts. The original implementation fails at insertion 514; the fix passes.

`DesyncSafetyTests` links the actual postfix against controlled doubles. It forces trace and log failures, checks pooled-item return and absence of further attempts, checks new-game retry and the ARM64 guard. It does **not** execute Harmony patch installation, Mono stack walking or RimWorld gameplay. `TraceFailureComparisonTest` checks that clearing incomplete trace data leaves map/world/command RNG evidence intact; it does not run a full game or compare cross-platform simulation results.

## Acceptance for a 300–400 mod pack

1. Freeze the exact game build, all five DLCs, mod IDs and order, selected install directories, binary/content hashes, Multiplayer Compatibility version and shared gameplay settings on every peer. Resolve different duplicate copies before testing. Run `Tools/sync_manifest.py` on each machine and compare the manifests; this is an equality preflight, not a compatibility verdict.
2. Establish a clean baseline on disposable copies of a save: core + Harmony + required loader only, then DLCs. Use two peers, including native Apple Silicon and x64, with tracing on/off, synchronous/asynchronous time, one/multiple maps and one/multiple factions. Keep every first-divergence report.
3. Add mod groups in dependency order (frameworks, pawn/jobs/AI, combat, vehicles, storage, world/quests, UI). A matching MPCompat source attribute only signals an available patch; no match is unknown, and may mean native support or content needing no patch. Verify patch load errors and actual mod versions.
4. Reproduce triggers: quest creation/end during ticks; launch/cancel/landing on different camera maps; ranged/melee combat; pawn pathing around doors with different faction permissions; storage hauling; powers/rituals; construction mechs; vehicles/caravans; Anomaly corpses; opening rewards/blueprints; save/reload and reconnect.
5. For each group run at least 100,000 ticks from a recorded identical save/seed at 1x/3x speed and repeat three times. Compare world/map/command state reports, collect the earliest divergence, and bisect only the new dependency-valid group when it fails. Finally soak the full pack for at least 1,000,000 ticks and repeat save/reload/reconnect. Record measured performance as well; do not assume the cache fix improves overall TPS.
6. Accept only a named, hashed profile with **zero observed desyncs in the recorded scenarios**, no patch exceptions, and no diagnostic circuit-breaker activation on supported x64 runs. Unsupported ARM64 native tracing is expected to be skipped. A build/test pass alone is not this acceptance result.

Still required: the actual 300–400-mod target list, a reproducible disposable save, and both peers. The locally observed 252-entry list is an investigation baseline, not automatically the requested target pack. No gameplay acceptance has been performed in this work.

## Manifest commands

```sh
python Tools/sync_manifest.py --mods-config /path/to/ModsConfig.xml --mods-root /path/to/Mods --mods-root /path/to/workshop/content/294100 --mods-root /path/to/RimWorld/Data --output host.json
python Tools/sync_manifest.py --compare host.json client.json
python -m unittest discover -s Tools -p "test_*.py"
```

For divergent duplicates, use `--selections chosen-directories.json` with a JSON object such as `{"rwmt.multiplayercompatibility":"/actual/loaded/mod/directory"}`. Choose the directory from the running game's loaded mod list; the tool deliberately does not guess precedence. Run the same process on both peers. Manifests stay local and contain hashes and relative mod-file names, without absolute directory locations or configuration values.
