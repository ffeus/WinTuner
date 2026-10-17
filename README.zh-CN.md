# WinTuner

[English](README.md) | **简体中文**

[![build](https://github.com/ffeus/WinTuner/actions/workflows/build.yml/badge.svg)](https://github.com/ffeus/WinTuner/actions/workflows/build.yml)
[![release](https://img.shields.io/github/v/release/ffeus/WinTuner?sort=semver)](https://github.com/ffeus/WinTuner/releases/latest)
[![license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

面向 Windows 的**声明式、可回滚**调优工具，只遵循一条铁律：**改动之前，必先快照。**

> 状态：早期骨架（v0.1）。架构与「更新控制」方案已端到端跑通；下方路线图是欢迎贡献的方向。

## 下载

预编译的单文件 win-x64 程序见
[Releases](https://github.com/ffeus/WinTuner/releases/latest) 页面
（`WinTuner-cli-win-x64.exe`、`WinTuner-gui-win-x64.exe`），旁边附有
`SHA256SUMS.txt` 校验文件。也可以从源码构建（见下）。

## 为什么还要再造一个调优工具？

关闭 Windows 更新、修改注册表与服务的工具随处可见，但几乎没有一个能告诉你**它到底改了什么**，更别说**帮你改回去**。WinTuner 把这两点做成一等公民：

- **快照优先**：每次应用前，先把所有待改目标的当前值完整快照下来。
- **可解释**：每次运行返回一份日志，逐条记录 `目标: 改动前 → 改动后`。
- **可回滚**：一条命令按快照撤销整批改动。
- **可脚本化**：图形界面与命令行共用同一套引擎。

## 仓库结构

```
src/
  WinTuner.Core/    引擎、数据模型、服务/注册表/计划任务 访问层 (net8.0-windows)
  WinTuner.Cli/     命令行入口 wintuner
  WinTuner.Gui/     WPF 桌面程序
tests/
  WinTuner.Core.Tests/
```

## 环境要求

- Windows 10 / 11
- **.NET 8 SDK**

## 快速开始

### 1. 获取源码并构建

```powershell
git clone https://github.com/ffeus/WinTuner.git
cd WinTuner
dotnet build -c Release
```

### 2. 先预览（不改动任何东西）

```powershell
cd src/WinTuner.Cli
dotnet run -- plan disable-windows-update
```

它会逐条打印该方案准备改动的目标，以及「当前值 vs 目标值」，方便你先看清利弊。

### 3. 应用（需要管理员权限）

**以管理员身份**打开 PowerShell，然后：

```powershell
cd src/WinTuner.Cli
dotnet run -- apply disable-windows-update
```

应用前会**自动先做快照**；结束后会打印快照文件路径，留着它就能回滚。

### 4. 回滚

```powershell
dotnet run -- rollback "%ProgramData%\WinTuner\snapshots\<快照ID>.json"
```

### 图形界面

```powershell
cd src/WinTuner.Gui
dotnet run
```

窗口里：选择方案 → `Preview` 预览 → `Apply` 应用 → `Rollback` 回滚。
程序清单已声明 `requireAdministrator`，启动时会弹 UAC 提权。

### 发布单文件 exe

```powershell
dotnet publish src/WinTuner.Cli -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

产物：`src/WinTuner.Cli/bin/Release/net8.0-windows/win-x64/publish/wintuner.exe`。

## 命令行用法

```text
wintuner list                        列出内置方案
wintuner status [服务名...]           查看服务的启动类型与运行状态
wintuner plan <方案ID>                预览方案（不写入任何改动）
wintuner apply <方案ID> [--dry-run]   应用方案（先写快照）
wintuner rollback <快照文件>          按快照还原
```

示例：

```powershell
wintuner plan disable-windows-update    # 预览差异
wintuner apply disable-windows-update   # 应用并保存快照
wintuner rollback "%ProgramData%\WinTuner\snapshots\<id>.json"
```

应用或回滚请以**管理员身份**运行。

## 安全模型

1. `apply` 先为方案中的每个目标捕获一份 `Snapshot`（快照）。
2. 每项改动独立尝试，失败只记录、不中断整批。
3. 运行返回 `ChangeJournal`，逐目标记录改动前后的值。
4. 快照写入 `%ProgramData%\WinTuner\snapshots\<id>.json`，可重放以撤销整批改动。

## 路线图

- [x] 快照 / 回滚引擎
- [x] 服务启动类型、注册表值、计划任务控制
- [x] Windows 更新启用/禁用方案
- [ ] 计划任务通配符路径
- [ ] 预设方案（游戏 / 办公 / 省电）
- [ ] 遥测与隐私开关
- [ ] 带签名的发布版程序（SignPath）
- [ ] JSON 自定义方案

## 许可证

MIT —— 见 [LICENSE](LICENSE)。
