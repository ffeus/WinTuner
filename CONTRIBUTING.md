# Contributing to WinTuner

Thanks for helping improve WinTuner. Please keep the project's core promise in
mind: **every change a plan makes must be snapshot-first and reversible.**

## Development setup

- Windows 10/11
- .NET 8 SDK

```powershell
dotnet build -c Release
dotnet test
```

## Guidelines

- **Never add a change type that cannot be snapshotted and restored.** If it
  can't be rolled back, it doesn't belong in a plan.
- **Keep plans declarative.** A `ChangePlan` describes intent; the engine owns
  the side effects.
- **Prefer data over code.** New presets should be data (service names, registry
  addresses, task paths), not bespoke procedures.
- **Add a test for every new plan or provider.** Logic tests must not require
  elevation or mutate the machine.
- **Do not copy code from other tools.** Service names and documented Windows
  behavior are facts; implementations must be original.

## Pull requests

1. Fork and branch from `main`.
2. Keep the build green (`dotnet build -c Release && dotnet test`).
3. Update `CHANGELOG.md` under *Unreleased*.
4. Describe *what* changed and *why*, and note any new reversibility caveats.

## Reporting issues

Include your Windows build (`winver`), the exact command or UI action, and the
full journal / error output. Do **not** paste secrets.
