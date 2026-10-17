# WinTuner

Declarative, **rollback-safe** tuning for Windows — built around one rule:
*nothing changes without a snapshot first.*

> Status: early skeleton (v0.1). The architecture and the update-control plans
> work end to end; the roadmap below is where contributions are welcome.

## Why another tuning tool?

Tools that flip Windows Update services and registry keys are everywhere. Almost
none of them can tell you **exactly what they changed** or **put it back**.
WinTuner makes both first-class:

- **Snapshot-first** — every apply captures the current value of everything it
  is about to touch, before touching it.
- **Explainable** — each run returns a journal of `target: before -> after`.
- **Reversible** — one command restores a saved snapshot to undo a whole batch.
- **Scriptable** — the same engine backs both a GUI and a CLI.

## Repository layout

```
src/
  WinTuner.Core/    engine, models, service/registry/task providers  (net8.0-windows)
  WinTuner.Cli/     `wintuner` command-line entry point
  WinTuner.Gui/     WPF desktop app
tests/
  WinTuner.Core.Tests/
```

## Build

Requires the **.NET 8 SDK** on Windows.

```powershell
dotnet build -c Release
dotnet test
```

## CLI usage

```text
wintuner list                    List built-in plans.
wintuner status [service...]     Show service start modes and status.
wintuner plan <id>               Preview a plan (nothing written).
wintuner apply <id> [--dry-run]  Apply a plan (snapshot written first).
wintuner rollback <snapshot>     Restore a saved snapshot.
```

Example:

```powershell
wintuner plan disable-windows-update     # preview the diff
wintuner apply disable-windows-update    # apply + save a snapshot
wintuner rollback "%ProgramData%\WinTuner\snapshots\<id>.json"
```

Run elevated (Administrator) to apply or roll back.

## Safety model

1. `apply` captures a `Snapshot` of every target in the plan.
2. Each change is attempted independently; failures are recorded, not fatal.
3. The run returns a `ChangeJournal` (`before`/`after` per target).
4. The snapshot is written to `%ProgramData%\WinTuner\snapshots\<id>.json`
   and can be replayed to revert the entire batch.

## Roadmap

- [x] Snapshot / rollback engine
- [x] Service start-mode control, registry values, scheduled tasks
- [x] Windows Update enable/disable profiles
- [ ] Wildcard scheduled-task paths
- [ ] Profile presets (gaming / office / power-saving)
- [ ] Telemetry & privacy toggles
- [ ] Signed release binaries (SignPath)
- [ ] JSON-defined custom profiles

## License

MIT — see [LICENSE](LICENSE).
