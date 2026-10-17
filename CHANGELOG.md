# Changelog

All notable changes to this project are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Simplified Chinese README (`README.zh-CN.md`) with a step-by-step quick start.

## [0.1.0] - 2026-10-17

### Added
- Snapshot-first apply pipeline (`TuningEngine`) producing a per-target
  `ChangeJournal` with before/after values.
- `SnapshotEngine` / `RollbackEngine`: capture, save and restore machine state.
- `ServiceManager`: read service start mode + runtime status, change start mode
  through the Service Control Manager.
- `RegistryToolkit`: read/write typed registry values by `HIVE\Path\ValueName`.
- `ScheduledTaskManager`: enable/disable tasks via `schtasks`.
- Built-in plans: `disable-windows-update`, `enable-windows-update`.
- `wintuner` CLI (list / status / plan / apply / rollback).
- WPF desktop UI with preview, apply and rollback.
- xUnit tests for built-in plan shapes.
