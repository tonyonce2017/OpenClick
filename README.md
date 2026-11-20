# OpenClick

English | [简体中文](README.zh-CN.md)

OpenClick is a free, open-source autoclicker for Windows.

## Features

- Choose the interval, mouse button, click type, repeat count, and current or fixed cursor position.
- Use global hotkeys, including modifier combinations, or arm hold mode to click while you hold the selected button.
- Record mouse and keyboard input, then replay it at a chosen speed and repeat count.
- Click one or more unfocused windows without bringing them to the front.
- Switch the interface between English and Simplified Chinese; the first run follows the Windows display language.
- Keep settings between runs.

## Download or run

Download the current [Windows x64 release](https://github.com/tonyonce2017/OpenClick/releases/latest), or build it from source on Windows with the .NET 10 SDK:

```powershell
dotnet build
dotnet run --project src/OpenClick
```

## Interface language

The interface ships in English and Simplified Chinese. On first run it follows the Windows display language; change it at any time under **Settings → Language**, and the open window re-applies its texts immediately. The choice is stored with the other settings in `%APPDATA%\OpenClick\settings.json`.

## Default hotkeys

| Key | Action |
| --- | --- |
| `F6` | Start or stop the clicker. In hold mode, arm or disarm it. |
| `F7` | Start or stop recording. |
| `F8` | Start or stop macro playback. |

You can change these shortcuts in the Settings tab.

## Background clicking and permissions

Background clicks use Windows `PostMessage`. Programs that read direct hardware input, including many games, may ignore them. To click an application running as administrator, run OpenClick as administrator too.

Autoclickers can trigger antivirus warnings. OpenClick's source is available here if you want to inspect or build it yourself.

## Build a release

```powershell
dotnet publish src/OpenClick -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

## For maintainers

OpenClick is a WinForms application targeting `net10.0-windows`. It has no external NuGet packages and calls Windows APIs through P/Invoke. See [docs/SPEC.md](docs/SPEC.md) for the source map and runtime notes.

## License

[MIT](LICENSE)
