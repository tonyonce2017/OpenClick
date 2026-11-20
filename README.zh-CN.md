# OpenClick

[English](README.md) | 简体中文

OpenClick 是一款免费、开源的 Windows 自动点击工具。

## 功能

- 可设置点击间隔、鼠标按键、点击类型、重复次数，以及使用当前位置或固定光标位置。
- 支持全局快捷键（含组合键），也可以启用「按住模式」：物理按住指定鼠标键时自动连点。
- 可录制鼠标与键盘操作，并按指定倍速和重复次数回放。
- 可在不激活目标窗口的情况下，向一个或多个后台窗口发送点击。
- 界面支持中文和英文，首次运行跟随 Windows 显示语言，随时可切换。
- 自动保存设置，下次启动继续使用。

## 下载或运行

下载最新的 [Windows x64 版本](https://github.com/tonyonce2017/OpenClick/releases/latest)，或在装有 .NET 10 SDK 的 Windows 上从源码构建运行：

```powershell
dotnet build
dotnet run --project src/OpenClick
```

## 界面语言

界面提供英文和简体中文。首次运行跟随 Windows 显示语言；之后可在「设置 → 语言」中随时切换，当前窗口会立即刷新文案。语言选择与其他设置一起保存在 `%APPDATA%\OpenClick\settings.json`。

## 默认快捷键

| 按键 | 作用 |
| --- | --- |
| `F6` | 开始/停止点击；按住模式下为启用/停用。 |
| `F7` | 开始/停止录制。 |
| `F8` | 开始/停止宏回放。 |

这些快捷键都可以在「设置」标签页中修改。

## 后台点击与权限说明

后台点击通过 Windows `PostMessage` 发送。读取直接硬件输入的程序（包括许多游戏）可能忽略这些消息。如需点击以管理员身份运行的程序，OpenClick 也需要以管理员身份运行。

自动点击类工具可能触发杀毒软件告警。如果希望自行检查或构建，源码就在本仓库中。

## 构建发布包

```powershell
dotnet publish src/OpenClick -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

## 面向维护者

OpenClick 是基于 `net10.0-windows` 的 WinForms 应用，不依赖任何外部 NuGet 包，并通过 P/Invoke 调用 Windows API。源码地图与运行时注意事项见 [docs/SPEC.md](docs/SPEC.md)。

## 许可证

[MIT](LICENSE)
