namespace OpenClick.Localization;

/// <summary>
/// Every user-visible string, keyed by area. English is the neutral column; Chinese is the
/// <c>zh</c> column. Format placeholders use the standard composite-format syntax consumed by
/// <see cref="Loc.F"/>.
/// </summary>
internal static class Strings
{
    internal static readonly Dictionary<string, (string English, string Chinese)> Table = new(StringComparer.Ordinal)
    {
        // ---- Shared ----
        ["Common.None"] = ("None", "无"),
        ["Common.Idle"] = ("Idle", "空闲"),
        ["Common.Untitled"] = ("(untitled)", "(无标题)"),
        ["App.Title"] = ("OpenClick", "OpenClick"),

        // ---- Tabs ----
        ["Tab.Autoclick"] = ("Autoclick", "自动点击"),
        ["Tab.Record"] = ("Record && Playback", "录制与回放"),
        ["Tab.Background"] = ("Background && Multiclick", "后台点击与多窗口"),
        ["Tab.Settings"] = ("Settings", "设置"),

        // ---- Autoclick tab ----
        ["Autoclick.Interval"] = ("Click interval", "点击间隔"),
        ["Autoclick.Hours"] = ("Hours", "小时"),
        ["Autoclick.Minutes"] = ("Mins", "分"),
        ["Autoclick.Seconds"] = ("Secs", "秒"),
        ["Autoclick.Milliseconds"] = ("Ms", "毫秒"),
        ["Autoclick.RandomOffset"] = ("Random offset ±", "随机偏移 ±"),
        ["Autoclick.MillisecondUnit"] = ("ms", "毫秒"),
        ["Autoclick.Options"] = ("Click options", "点击选项"),
        ["Autoclick.MouseButton"] = ("Mouse button:", "鼠标按键："),
        ["Autoclick.ButtonLeft"] = ("Left", "左键"),
        ["Autoclick.ButtonRight"] = ("Right", "右键"),
        ["Autoclick.ButtonMiddle"] = ("Middle", "中键"),
        ["Autoclick.ClickType"] = ("Click type:", "点击类型："),
        ["Autoclick.TypeSingle"] = ("Single", "单击"),
        ["Autoclick.TypeDouble"] = ("Double", "双击"),
        ["Autoclick.HoldMode"] = (
            "Hold mode — autoclick while the trigger mouse button is physically held",
            "按住模式：物理按住触发按钮时自动连点"),
        ["Autoclick.RepeatGroup"] = ("Click repeat", "重复设置"),
        ["Autoclick.RepeatUntilStopped"] = ("Repeat until stopped", "一直重复，直到手动停止"),
        ["Autoclick.RepeatCount"] = ("Repeat", "重复"),
        ["Autoclick.Times"] = ("times", "次"),
        ["Autoclick.Position"] = ("Cursor position", "光标位置"),
        ["Autoclick.PositionCurrent"] = ("Current position", "当前位置"),
        ["Autoclick.PositionFixed"] = ("Fixed", "固定位置"),
        ["Autoclick.PositionX"] = ("X:", "X："),
        ["Autoclick.PositionY"] = ("Y:", "Y："),
        ["Autoclick.PickLocation"] = ("Pick location...", "选取位置..."),
        ["Autoclick.Start"] = ("Start ({0})", "开始（{0}）"),
        ["Autoclick.Stop"] = ("Stop ({0})", "停止（{0}）"),
        ["Autoclick.ArmHold"] = ("Arm hold mode ({0})", "启用按住模式（{0}）"),
        ["Autoclick.DisarmHold"] = ("Disarm hold mode ({0})", "停用按住模式（{0}）"),

        // ---- Record & playback tab ----
        ["Record.Group"] = ("Recording", "录制"),
        ["Record.Record"] = ("● Record ({0})", "● 录制（{0}）"),
        ["Record.StopRecording"] = ("■ Stop Recording ({0})", "■ 停止录制（{0}）"),
        ["Record.PlaybackGroup"] = ("Playback", "回放"),
        ["Record.Speed"] = ("Speed:", "速度："),
        ["Record.Repeat"] = ("Repeat:", "重复次数："),
        ["Record.RepeatNote"] = ("(0 = repeat forever)", "（0 = 无限重复）"),
        ["Record.Play"] = ("▶ Play ({0})", "▶ 播放（{0}）"),
        ["Record.StopPlayback"] = ("■ Stop ({0})", "■ 停止（{0}）"),
        ["Record.MacroFile"] = ("Macro file", "宏文件"),
        ["Record.SaveMacro"] = ("Save macro...", "保存宏..."),
        ["Record.LoadMacro"] = ("Load macro...", "加载宏..."),
        ["Record.NoMacro"] = ("No macro loaded", "未加载宏"),
        ["Record.EventsRecorded"] = ("{0} events recorded", "已录制 {0} 个事件"),
        ["Record.EventsCaptured"] = ("{0} events captured", "已捕获 {0} 个事件"),
        ["Record.LoadedEvents"] = ("Loaded {0} events", "已加载 {0} 个事件"),
        ["Record.RepeatCompleted"] = ("Repeat {0} completed", "第 {0} 次重复已完成"),
        ["Record.RecordingStatus"] = ("Recording… ({0} events)", "录制中…（{0} 个事件）"),

        // ---- Status bar ----
        ["Status.Clicking"] = ("Clicking… ({0})", "点击中…（{0}）"),
        ["Status.Playing"] = ("Playing…", "回放中…"),
        ["Status.HoldArmed"] = ("Hold mode armed", "按住模式已启用"),
        ["Status.HotkeyReminder"] = ("{0} {1}   {2} record   {3} play", "{0} {1}   {2} 录制   {3} 回放"),
        ["Status.HotkeyStartStop"] = ("start/stop", "开始/停止"),
        ["Status.HotkeyArmHold"] = ("arm/disarm hold", "启用/停用按住"),

        // ---- Background & multiclick tab ----
        ["Background.ColumnWindow"] = ("Window", "窗口"),
        ["Background.ColumnClass"] = ("Class", "窗口类"),
        ["Background.ColumnPoint"] = ("Point", "坐标"),
        ["Background.AddTarget"] = ("Add target... (pick)", "添加目标...（拾取）"),
        ["Background.Remove"] = ("Remove", "移除"),
        ["Background.TestClick"] = ("Test click", "测试点击"),
        ["Background.Start"] = ("Start background clicking", "开始后台点击"),
        ["Background.Stop"] = ("Stop background clicking", "停止后台点击"),
        ["Background.Note"] = (
            "Background clicks use PostMessage; some apps (games using raw input) ignore them.",
            "后台点击通过 PostMessage 发送；部分程序（例如使用原始输入的游戏）会忽略这些消息。"),

        // ---- Settings tab ----
        ["Settings.ToggleClicker"] = ("Toggle clicker:", "切换点击器："),
        ["Settings.ToggleRecording"] = ("Toggle recording:", "切换录制："),
        ["Settings.TogglePlayback"] = ("Toggle playback:", "切换回放："),
        ["Settings.AlwaysOnTop"] = ("Always on top", "窗口置顶"),
        ["Settings.GitHub"] = ("OpenClick on GitHub", "在 GitHub 上查看 OpenClick"),
        ["Settings.Language"] = ("Language:", "语言："),
        ["Settings.LanguageSystem"] = ("Follow system", "跟随系统"),
        ["Settings.LanguageEnglish"] = ("English", "English"),
        ["Settings.LanguageChinese"] = ("简体中文", "简体中文"),
        ["Settings.LanguageHint"] = ("Applies immediately.", "切换后立即生效。"),

        // ---- Dialogs ----
        ["Message.NoMacroToSave"] = ("No macro to save. Record something first.", "没有可保存的宏，请先录制一段操作。"),
        ["Message.SaveMacroFailed"] = ("Failed to save macro: {0}", "保存宏失败：{0}"),
        ["Message.LoadMacroFailed"] = ("Failed to load macro: {0}", "加载宏失败：{0}"),
        ["Message.NoBackgroundTarget"] = ("Add at least one background target first.", "请先添加至少一个后台点击目标。"),
        ["Filter.Macro"] = (
            "OpenClick macro (*.ocmacro.json)|*.ocmacro.json|All files (*.*)|*.*",
            "OpenClick 宏文件 (*.ocmacro.json)|*.ocmacro.json|所有文件 (*.*)|*.*"),

        // ---- Controls with their own text ----
        ["Hotkey.PressKeys"] = ("Press keys…", "请按下按键…"),
        ["Overlay.PickLocation"] = ("click to select, Esc to cancel", "单击选择，Esc 取消"),
        ["Overlay.PickLocationWithPoint"] = ("{0}, {1} — click to select, Esc to cancel", "{0}, {1} — 单击选择，Esc 取消"),
        ["Overlay.PickWindowWithInfo"] = ("{0} — click to select, Esc to cancel", "{0} — 单击选择，Esc 取消"),
    };
}
