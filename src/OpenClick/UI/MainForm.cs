using System.Diagnostics;
using OpenClick.Core;
using OpenClick.Localization;

namespace OpenClick.UI;

/// <summary>Main OpenClick window: 4 tabs (Autoclick / Record & Playback / Background & Multiclick / Settings).</summary>
public sealed class MainForm : Form
{
    private enum EngineMode { None, Foreground, Background }

    /// <summary>Which wording the macro label uses: nothing loaded, just recorded, or loaded from disk.</summary>
    private enum MacroInfoState { None, Recorded, Loaded }

    // ---- Core services (owned here) ----
    private readonly ClickEngine _engine = new();
    private readonly HotkeyManager _hotkeyManager = new();
    private readonly HoldClickMonitor _holdMonitor = new();
    private readonly Recorder _recorder = new();
    private readonly Player _player = new();

    private AppSettings _settings = new();
    private MacroScript? _currentScript;
    private MacroInfoState _macroInfoState = MacroInfoState.None;
    private EngineMode _engineMode = EngineMode.None;
    // Hold mode is selected by the checkbox, but only armed by the clicker toggle.
    private bool _holdModeArmed;

    // ---- StatusStrip ----
    private readonly StatusStrip _statusStrip = new();
    private readonly ToolStripStatusLabel _lblState = new() { Text = Loc.T("Common.Idle"), Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel _lblHotkeyReminder = new();

    // ---- Tab 1: Autoclick ----
    private readonly TabControl _tabControl = new() { Dock = DockStyle.Fill };
    private readonly TabPage _tabAutoclick = new(Loc.T("Tab.Autoclick")) { AutoScroll = true };
    private readonly TabPage _tabRecord = new(Loc.T("Tab.Record")) { AutoScroll = true };
    private readonly TabPage _tabBackground = new(Loc.T("Tab.Background")) { AutoScroll = true };
    private readonly TabPage _tabSettings = new(Loc.T("Tab.Settings")) { AutoScroll = true };

    private readonly NumericUpDown _numHours = new() { Minimum = 0, Maximum = 23, Width = 50 };
    private readonly NumericUpDown _numMinutes = new() { Minimum = 0, Maximum = 59, Width = 50 };
    private readonly NumericUpDown _numSeconds = new() { Minimum = 0, Maximum = 59, Width = 50 };
    private readonly NumericUpDown _numMs = new() { Minimum = 0, Maximum = 9999, Width = 60 };
    private readonly CheckBox _chkRandomOffset = new() { Text = Loc.T("Autoclick.RandomOffset"), AutoSize = true };
    private readonly NumericUpDown _numRandomOffsetMs = new() { Minimum = 0, Maximum = 60000, Width = 70 };

    private readonly ComboBox _cboButton = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
    private readonly ComboBox _cboClickType = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
    private readonly CheckBox _chkHoldMode = new()
    {
        Text = Loc.T("Autoclick.HoldMode"),
        AutoSize = true,
    };

    private readonly RadioButton _radRepeatUntilStopped = new() { Text = Loc.T("Autoclick.RepeatUntilStopped"), AutoSize = true, Checked = true };
    private readonly RadioButton _radRepeatCount = new() { Text = Loc.T("Autoclick.RepeatCount"), AutoSize = true };
    private readonly NumericUpDown _numRepeatCount = new() { Minimum = 1, Maximum = 1000000, Width = 80, Value = 100 };

    private readonly RadioButton _radPosCurrent = new() { Text = Loc.T("Autoclick.PositionCurrent"), AutoSize = true, Checked = true };
    private readonly RadioButton _radPosFixed = new() { Text = Loc.T("Autoclick.PositionFixed"), AutoSize = true };
    private readonly NumericUpDown _numFixedX = new() { Minimum = -32768, Maximum = 32767, Width = 65 };
    private readonly NumericUpDown _numFixedY = new() { Minimum = -32768, Maximum = 32767, Width = 65 };
    private readonly Button _btnPickLocation = new() { Text = Loc.T("Autoclick.PickLocation") };

    private readonly Button _btnStart = new() { Text = Loc.T("Autoclick.Start") };
    private readonly Button _btnStop = new() { Text = Loc.T("Autoclick.Stop"), Enabled = false };

    // ---- Tab 2: Record & Playback ----
    private readonly Button _btnRecordToggle = new() { Text = Loc.T("Record.Group") };
    private readonly Label _lblEventCount = new() { Text = Loc.F("Record.EventsRecorded", 0), AutoSize = true };

    private readonly ComboBox _cboSpeed = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
    private readonly NumericUpDown _numRepeatPlayback = new() { Minimum = 0, Maximum = 10000, Width = 70, Value = 1 };
    private readonly Button _btnPlayToggle = new() { Text = Loc.T("Record.PlaybackGroup"), Enabled = false };
    private readonly Label _lblPlaybackProgress = new() { Text = Loc.T("Common.Idle"), AutoSize = true };

    private readonly Button _btnSaveMacro = new() { Text = Loc.T("Record.SaveMacro") };
    private readonly Button _btnLoadMacro = new() { Text = Loc.T("Record.LoadMacro") };
    private readonly Label _lblMacroInfo = new() { Text = Loc.T("Record.NoMacro"), AutoSize = true };

    // ---- Tab 3: Background & Multiclick ----
    private readonly ListView _lvTargets = new()
    {
        View = View.Details,
        CheckBoxes = true,
        FullRowSelect = true,
        HideSelection = false,
    };
    private readonly Button _btnAddTarget = new() { Text = Loc.T("Background.AddTarget") };
    private readonly Button _btnRemoveTarget = new() { Text = Loc.T("Background.Remove") };
    private readonly Button _btnTestClick = new() { Text = Loc.T("Background.TestClick") };
    private readonly Button _btnStartBackground = new() { Text = Loc.T("Background.Start") };
    private readonly Button _btnStopBackground = new() { Text = Loc.T("Background.Stop"), Enabled = false };

    // ---- Tab 4: Settings ----
    private readonly HotkeyCaptureBox _hkClicker = new();
    private readonly HotkeyCaptureBox _hkRecord = new();
    private readonly HotkeyCaptureBox _hkPlayback = new();
    private readonly ComboBox _cboLanguage = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
    private readonly CheckBox _chkAlwaysOnTop = new() { Text = Loc.T("Settings.AlwaysOnTop"), AutoSize = true };
    private readonly LinkLabel _lnkGithub = new() { Text = Loc.T("Settings.GitHub"), AutoSize = true };

    // ---- Localization ----
    // Static texts are registered here so a language switch can re-apply them without
    // rebuilding the window (which owns the engine, recorder and hook lifetimes).
    private readonly Dictionary<Control, string> _textBindings = new();
    private bool _applyingLanguage;

    // Each tab was originally laid out for the default window size. Preserve that
    // design at its minimum size, then scale its contained controls as the user
    // gives the window more room instead of leaving a small fixed layout in the
    // top-left corner.
    //
    // Tab children deliberately keep the default Top|Left anchor: right-anchored
    // controls are re-positioned by WinForms when the tab page is first sized from
    // its designer default, which pushes them outside the visible area. This
    // dictionary plus ApplyResponsiveLayout is the single source of layout truth.
    private readonly Dictionary<Control, ResponsiveLayoutMetrics> _responsiveLayout = new();
    private readonly Dictionary<TabPage, Size> _responsiveTabSizes = new();
    private readonly Dictionary<ListView, int[]> _responsiveListViewColumns = new();
    private readonly Dictionary<Control, Font> _responsiveFonts = new();
    private bool _responsiveLayoutCaptured;

    public MainForm()
    {
        Text = Loc.T("App.Title");
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font(Loc.UiFontFamily, 9f);
        ClientSize = new Size(540, 460);
        // Keep the original layout usable while allowing users to give it more room.
        // MinimumSize is an outer-window size, so use the calculated size after setting
        // ClientSize to preserve the full designed client area.
        MinimumSize = Size;

        BuildAutoclickTab();
        BuildRecordTab();
        BuildBackgroundTab();
        BuildSettingsTab();
        RegisterLocalizedControls();

        _tabControl.TabPages.AddRange(new[] { _tabAutoclick, _tabRecord, _tabBackground, _tabSettings });

        _statusStrip.Items.Add(_lblState);
        _statusStrip.Items.Add(_lblHotkeyReminder);

        Controls.Add(_tabControl);
        Controls.Add(_statusStrip);

        Shown += (_, _) => CaptureResponsiveLayout();
        Resize += (_, _) => ApplyResponsiveLayout();

        WireCoreEvents();
        WireControlEvents();

        _settings = SettingsStore.Load();
        ApplySettingsToControls(_settings);
        RegisterHotkeysFromSettings();
        UpdateButtonLabels();
        UpdateStatusHotkeyLabel();
        UpdateEngineButtons();
        UpdatePlayButtonEnabled();

        Loc.Changed += OnLanguageChanged;
        FormClosing += OnMainFormClosing;
        FormClosed += OnMainFormClosed;
    }

    // ================================================================
    // Localization
    // ================================================================

    /// <summary>Registers fields whose text is a fixed string; dynamic labels are refreshed by name.</summary>
    private void RegisterLocalizedControls()
    {
        BindText(_tabAutoclick, "Tab.Autoclick");
        BindText(_tabRecord, "Tab.Record");
        BindText(_tabBackground, "Tab.Background");
        BindText(_tabSettings, "Tab.Settings");

        BindText(_chkRandomOffset, "Autoclick.RandomOffset");
        BindText(_chkHoldMode, "Autoclick.HoldMode");
        BindText(_radRepeatUntilStopped, "Autoclick.RepeatUntilStopped");
        BindText(_radRepeatCount, "Autoclick.RepeatCount");
        BindText(_radPosCurrent, "Autoclick.PositionCurrent");
        BindText(_radPosFixed, "Autoclick.PositionFixed");
        BindText(_btnPickLocation, "Autoclick.PickLocation");

        BindText(_btnSaveMacro, "Record.SaveMacro");
        BindText(_btnLoadMacro, "Record.LoadMacro");

        BindText(_btnAddTarget, "Background.AddTarget");
        BindText(_btnRemoveTarget, "Background.Remove");
        BindText(_btnTestClick, "Background.TestClick");
        BindText(_btnStartBackground, "Background.Start");
        BindText(_btnStopBackground, "Background.Stop");

        BindText(_chkAlwaysOnTop, "Settings.AlwaysOnTop");
        BindText(_lnkGithub, "Settings.GitHub");

        SetComboTexts(
            _cboButton,
            Loc.T("Autoclick.ButtonLeft"),
            Loc.T("Autoclick.ButtonRight"),
            Loc.T("Autoclick.ButtonMiddle"));
        SetComboTexts(
            _cboClickType,
            Loc.T("Autoclick.TypeSingle"),
            Loc.T("Autoclick.TypeDouble"));
    }

    private T BindText<T>(T control, string key)
        where T : Control
    {
        _textBindings[control] = key;
        control.Text = Loc.T(key);
        return control;
    }

    private Label MakeLabel(string key, int x, int y)
    {
        Label label = new() { Text = Loc.T(key), AutoSize = true, Location = new Point(x, y) };
        _textBindings[label] = key;
        return label;
    }

    private static void SetComboTexts(ComboBox combo, params string[] texts)
    {
        int previousIndex = combo.SelectedIndex;
        combo.BeginUpdate();
        combo.Items.Clear();
        combo.Items.AddRange(texts);
        combo.EndUpdate();
        if (previousIndex >= 0)
        {
            combo.SelectedIndex = Math.Min(previousIndex, texts.Length - 1);
        }
    }

    /// <summary>Re-applies every localized string and reflows the layout for the new language.</summary>
    private void OnLanguageChanged()
    {
        ApplyLocalizedTexts();
        ResetLayoutToDesign();
        ApplyResponsiveLayout();
    }

    private void ApplyLocalizedTexts()
    {
        _applyingLanguage = true;
        try
        {
            Text = Loc.T("App.Title");

            foreach ((Control control, string key) in _textBindings)
            {
                control.Text = Loc.T(key);
            }

            SetComboTexts(
                _cboButton,
                Loc.T("Autoclick.ButtonLeft"),
                Loc.T("Autoclick.ButtonRight"),
                Loc.T("Autoclick.ButtonMiddle"));
            SetComboTexts(
                _cboClickType,
                Loc.T("Autoclick.TypeSingle"),
                Loc.T("Autoclick.TypeDouble"));
            SetComboTexts(
                _cboLanguage,
                Loc.T("Settings.LanguageSystem"),
                Loc.T("Settings.LanguageEnglish"),
                Loc.T("Settings.LanguageChinese"));
            _cboLanguage.SelectedIndex = (int)_settings.Language;

            _lvTargets.Columns[0].Text = Loc.T("Background.ColumnWindow");
            _lvTargets.Columns[1].Text = Loc.T("Background.ColumnClass");
            _lvTargets.Columns[2].Text = Loc.T("Background.ColumnPoint");

            UpdateButtonLabels();
            UpdateStatusLabel();
            UpdateStatusHotkeyLabel();
            RefreshEventCountLabel();
            RefreshMacroInfoLabel();
            if (!_player.IsPlaying)
            {
                _lblPlaybackProgress.Text = Loc.T("Common.Idle");
            }
        }
        finally
        {
            _applyingLanguage = false;
        }
    }

    private void OnLanguageSelectionChanged()
    {
        if (_applyingLanguage)
        {
            return;
        }

        AppLanguage language = _cboLanguage.SelectedIndex switch
        {
            1 => AppLanguage.English,
            2 => AppLanguage.Chinese,
            _ => AppLanguage.System,
        };

        _settings.Language = language;
        Loc.SetLanguage(language);
    }

    // ================================================================
    // Tab building
    // ================================================================

    private void BuildAutoclickTab()
    {
        GroupBox grpInterval = new()
        {
            Text = Loc.T("Autoclick.Interval"),
            Bounds = new Rectangle(10, 10, 512, 90),
        };
        BindText(grpInterval, "Autoclick.Interval");
        Label lblHours = MakeLabel("Autoclick.Hours", 10, 27);
        _numHours.Location = new Point(60, 24);
        Label lblMinutes = MakeLabel("Autoclick.Minutes", 125, 27);
        _numMinutes.Location = new Point(170, 24);
        Label lblSeconds = MakeLabel("Autoclick.Seconds", 235, 27);
        _numSeconds.Location = new Point(280, 24);
        Label lblMs = MakeLabel("Autoclick.Milliseconds", 345, 27);
        _numMs.Location = new Point(380, 24);
        _chkRandomOffset.Location = new Point(10, 58);
        _numRandomOffsetMs.Location = new Point(155, 56);
        Label lblOffsetMs = MakeLabel("Autoclick.MillisecondUnit", 230, 58);
        grpInterval.Controls.AddRange(new Control[]
        {
            lblHours, _numHours, lblMinutes, _numMinutes, lblSeconds, _numSeconds, lblMs, _numMs,
            _chkRandomOffset, _numRandomOffsetMs, lblOffsetMs,
        });

        GroupBox grpOptions = new()
        {
            Text = Loc.T("Autoclick.Options"),
            Bounds = new Rectangle(10, 108, 512, 80),
        };
        BindText(grpOptions, "Autoclick.Options");
        Label lblButton = MakeLabel("Autoclick.MouseButton", 10, 27);
        _cboButton.Location = new Point(105, 24);
        Label lblClickType = MakeLabel("Autoclick.ClickType", 230, 27);
        _cboClickType.Location = new Point(300, 24);
        _chkHoldMode.Location = new Point(10, 54);
        grpOptions.Controls.AddRange(new Control[] { lblButton, _cboButton, lblClickType, _cboClickType, _chkHoldMode });

        GroupBox grpRepeat = new()
        {
            Text = Loc.T("Autoclick.RepeatGroup"),
            Bounds = new Rectangle(10, 196, 250, 120),
        };
        BindText(grpRepeat, "Autoclick.RepeatGroup");
        _radRepeatUntilStopped.Location = new Point(10, 24);
        _radRepeatCount.Location = new Point(10, 52);
        _numRepeatCount.Location = new Point(75, 50);
        Label lblTimes = MakeLabel("Autoclick.Times", 160, 52);
        grpRepeat.Controls.AddRange(new Control[] { _radRepeatUntilStopped, _radRepeatCount, _numRepeatCount, lblTimes });

        GroupBox grpPosition = new()
        {
            Text = Loc.T("Autoclick.Position"),
            Bounds = new Rectangle(270, 196, 252, 126),
        };
        BindText(grpPosition, "Autoclick.Position");
        _radPosCurrent.Location = new Point(10, 20);
        _radPosFixed.Location = new Point(10, 44);
        // The Chinese labels use a full-width colon and are wider than the ASCII ones, so the
        // numerics start far enough right to keep their edit area clear of the label in both languages.
        Label lblX = MakeLabel("Autoclick.PositionX", 14, 72);
        _numFixedX.Location = new Point(48, 69);
        Label lblY = MakeLabel("Autoclick.PositionY", 125, 72);
        _numFixedY.Location = new Point(159, 69);
        _btnPickLocation.Bounds = new Rectangle(10, 96, 232, 24);
        grpPosition.Controls.AddRange(new Control[] { _radPosCurrent, _radPosFixed, lblX, _numFixedX, lblY, _numFixedY, _btnPickLocation });

        _btnStart.Bounds = new Rectangle(10, 328, 246, 40);
        _btnStop.Bounds = new Rectangle(276, 328, 246, 40);

        _tabAutoclick.Controls.AddRange(new Control[] { grpInterval, grpOptions, grpRepeat, grpPosition, _btnStart, _btnStop });
    }

    private void BuildRecordTab()
    {
        GroupBox grpRecording = new()
        {
            Text = Loc.T("Record.Group"),
            Bounds = new Rectangle(10, 10, 512, 65),
        };
        BindText(grpRecording, "Record.Group");
        _btnRecordToggle.Bounds = new Rectangle(10, 24, 170, 28);
        _lblEventCount.Location = new Point(195, 30);
        grpRecording.Controls.AddRange(new Control[] { _btnRecordToggle, _lblEventCount });

        GroupBox grpPlayback = new()
        {
            Text = Loc.T("Record.PlaybackGroup"),
            Bounds = new Rectangle(10, 85, 512, 130),
        };
        BindText(grpPlayback, "Record.PlaybackGroup");
        Label lblSpeed = MakeLabel("Record.Speed", 10, 30);
        _cboSpeed.Location = new Point(65, 27);
        _cboSpeed.Items.AddRange(new object[] { "0.25x", "0.5x", "1x", "2x", "4x" });
        _cboSpeed.SelectedIndex = 2;
        Label lblRepeat = MakeLabel("Record.Repeat", 165, 30);
        _numRepeatPlayback.Location = new Point(240, 27);
        Label lblRepeatNote = MakeLabel("Record.RepeatNote", 315, 30);
        _btnPlayToggle.Bounds = new Rectangle(10, 65, 170, 28);
        _lblPlaybackProgress.Location = new Point(195, 71);
        grpPlayback.Controls.AddRange(new Control[]
        {
            lblSpeed, _cboSpeed, lblRepeat, _numRepeatPlayback, lblRepeatNote, _btnPlayToggle, _lblPlaybackProgress,
        });

        GroupBox grpFile = new()
        {
            Text = Loc.T("Record.MacroFile"),
            Bounds = new Rectangle(10, 225, 512, 70),
        };
        BindText(grpFile, "Record.MacroFile");
        _btnSaveMacro.Bounds = new Rectangle(10, 26, 150, 28);
        _btnLoadMacro.Bounds = new Rectangle(170, 26, 150, 28);
        _lblMacroInfo.Location = new Point(335, 32);
        grpFile.Controls.AddRange(new Control[] { _btnSaveMacro, _btnLoadMacro, _lblMacroInfo });

        _tabRecord.Controls.AddRange(new Control[] { grpRecording, grpPlayback, grpFile });
    }

    private void BuildBackgroundTab()
    {
        _lvTargets.Bounds = new Rectangle(10, 10, 512, 215);
        _lvTargets.Columns.Add(Loc.T("Background.ColumnWindow"), 240);
        _lvTargets.Columns.Add(Loc.T("Background.ColumnClass"), 120);
        _lvTargets.Columns.Add(Loc.T("Background.ColumnPoint"), 100);

        _btnAddTarget.Bounds = new Rectangle(10, 232, 150, 28);
        _btnRemoveTarget.Bounds = new Rectangle(170, 232, 100, 28);
        _btnTestClick.Bounds = new Rectangle(280, 232, 110, 28);

        Label lblNote = new()
        {
            Text = Loc.T("Background.Note"),
            AutoSize = false,
            Bounds = new Rectangle(10, 266, 512, 32),
            ForeColor = SystemColors.GrayText,
        };
        BindText(lblNote, "Background.Note");

        _btnStartBackground.Bounds = new Rectangle(10, 304, 246, 34);
        _btnStopBackground.Bounds = new Rectangle(276, 304, 246, 34);

        _tabBackground.Controls.AddRange(new Control[]
        {
            _lvTargets, _btnAddTarget, _btnRemoveTarget, _btnTestClick, lblNote, _btnStartBackground, _btnStopBackground,
        });
    }

    private void BuildSettingsTab()
    {
        Label lblClicker = MakeLabel("Settings.ToggleClicker", 10, 23);
        _hkClicker.Bounds = new Rectangle(150, 20, 170, 23);
        Label lblRecord = MakeLabel("Settings.ToggleRecording", 10, 58);
        _hkRecord.Bounds = new Rectangle(150, 55, 170, 23);
        Label lblPlayback = MakeLabel("Settings.TogglePlayback", 10, 93);
        _hkPlayback.Bounds = new Rectangle(150, 90, 170, 23);

        Label lblLanguage = MakeLabel("Settings.Language", 10, 135);
        _cboLanguage.Bounds = new Rectangle(150, 132, 170, 23);
        SetComboTexts(
            _cboLanguage,
            Loc.T("Settings.LanguageSystem"),
            Loc.T("Settings.LanguageEnglish"),
            Loc.T("Settings.LanguageChinese"));
        _cboLanguage.SelectedIndexChanged += (_, _) => OnLanguageSelectionChanged();
        Label lblLanguageHint = MakeLabel("Settings.LanguageHint", 330, 135);

        _chkAlwaysOnTop.Location = new Point(10, 175);
        _lnkGithub.Location = new Point(10, 205);
        _lnkGithub.LinkClicked += OnGithubLinkClicked;

        _tabSettings.Controls.AddRange(new Control[]
        {
            lblClicker, _hkClicker, lblRecord, _hkRecord, lblPlayback, _hkPlayback,
            lblLanguage, _cboLanguage, lblLanguageHint, _chkAlwaysOnTop, _lnkGithub,
        });
    }

    // ================================================================
    // Responsive window layout
    // ================================================================

    private void CaptureResponsiveLayout()
    {
        if (_responsiveLayoutCaptured)
        {
            return;
        }

        foreach (TabPage tab in _tabControl.TabPages)
        {
            _responsiveTabSizes.Add(tab, tab.ClientSize);
            CaptureResponsiveLayout(tab);
        }

        _responsiveLayoutCaptured = true;
        ApplyResponsiveLayout();
    }

    private void CaptureResponsiveLayout(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            _responsiveLayout.Add(child, new ResponsiveLayoutMetrics(
                child.Bounds,
                child.Font,
                child.AutoSize && child is Label or LinkLabel or CheckBox or RadioButton));
            if (child is ListView listView)
            {
                _responsiveListViewColumns.Add(listView, listView.Columns.Cast<ColumnHeader>().Select(column => column.Width).ToArray());
            }

            CaptureResponsiveLayout(child);
        }
    }

    private void ApplyResponsiveLayout()
    {
        if (!_responsiveLayoutCaptured)
        {
            return;
        }

        foreach ((TabPage tab, Size baseSize) in _responsiveTabSizes)
        {
            if (baseSize.Width <= 0 || baseSize.Height <= 0)
            {
                continue;
            }

            float horizontalScale = Math.Max(1f, tab.ClientSize.Width / (float)baseSize.Width);
            float verticalScale = Math.Max(1f, tab.ClientSize.Height / (float)baseSize.Height);
            float fontScale = Math.Min(horizontalScale, verticalScale);

            tab.SuspendLayout();
            ApplyResponsiveLayout(tab, horizontalScale, verticalScale, fontScale);
            tab.ResumeLayout(performLayout: false);
        }
    }

    private void ApplyResponsiveLayout(Control parent, float horizontalScale, float verticalScale, float fontScale)
    {
        foreach (Control child in parent.Controls)
        {
            ResponsiveLayoutMetrics metrics = _responsiveLayout[child];
            child.Location = new Point(
                (int)Math.Round(metrics.Bounds.X * horizontalScale),
                (int)Math.Round(metrics.Bounds.Y * verticalScale));

            if (!metrics.AutoSize)
            {
                child.Size = new Size(
                    Math.Max(1, (int)Math.Round(metrics.Bounds.Width * horizontalScale)),
                    Math.Max(1, (int)Math.Round(metrics.Bounds.Height * verticalScale)));
            }

            ApplyResponsiveFont(child, metrics.Font, fontScale);
            if (child is ListView listView && _responsiveListViewColumns.TryGetValue(listView, out int[]? columnWidths))
            {
                for (int i = 0; i < columnWidths.Length; i++)
                {
                    listView.Columns[i].Width = Math.Max(1, (int)Math.Round(columnWidths[i] * horizontalScale));
                }
            }

            ApplyResponsiveLayout(child, horizontalScale, verticalScale, fontScale);
        }
    }

    private void ApplyResponsiveFont(Control control, Font baseFont, float scale)
    {
        if (Math.Abs(scale - 1f) < 0.01f)
        {
            if (_responsiveFonts.Remove(control, out Font? scaledFont))
            {
                control.Font = baseFont;
                scaledFont.Dispose();
            }

            return;
        }

        float size = Math.Max(1f, baseFont.SizeInPoints * scale);
        if (Math.Abs(control.Font.SizeInPoints - size) < 0.01f && control.Font.Style == baseFont.Style)
        {
            return;
        }

        Font resizedFont = new(baseFont.FontFamily, size, baseFont.Style, GraphicsUnit.Point);
        Font? previousFont = _responsiveFonts.GetValueOrDefault(control);
        control.Font = resizedFont;
        _responsiveFonts[control] = resizedFont;
        previousFont?.Dispose();
    }

    private void DisposeResponsiveFonts()
    {
        foreach (Font font in _responsiveFonts.Values)
        {
            font.Dispose();
        }

        _responsiveFonts.Clear();
    }

    /// <summary>
    /// Restores the captured design geometry. Text lengths change with the language, and
    /// <see cref="ApplyResponsiveLayout"/> derives sizes from the captured metrics, so resetting
    /// first keeps a language switch from compounding with the current window scale.
    /// </summary>
    private void ResetLayoutToDesign()
    {
        foreach ((Control control, ResponsiveLayoutMetrics metrics) in _responsiveLayout)
        {
            if (_responsiveFonts.Remove(control, out Font? scaled))
            {
                scaled.Dispose();
            }

            control.Bounds = metrics.Bounds;
            control.Font = metrics.Font;
        }

        foreach ((ListView listView, int[] widths) in _responsiveListViewColumns)
        {
            for (int i = 0; i < widths.Length && i < listView.Columns.Count; i++)
            {
                listView.Columns[i].Width = widths[i];
            }
        }
    }

    private readonly record struct ResponsiveLayoutMetrics(Rectangle Bounds, Font Font, bool AutoSize);

    // ================================================================
    // Wiring
    // ================================================================

    private void WireCoreEvents()
    {
        _engine.Started += () => UiInvoke(() => { UpdateEngineButtons(); UpdateStatusLabel(); });
        _engine.Stopped += () => UiInvoke(() => { UpdateEngineButtons(); UpdateStatusLabel(); });
        _engine.ClickTick += n => UiInvoke(() => { _lblState.Text = Loc.F("Status.Clicking", n); });

        _hotkeyManager.HotkeyPressed += actionId => UiInvoke(() => OnHotkeyPressed(actionId));

        _holdMonitor.HoldStarted += () => UiInvoke(() =>
        {
            if (_holdModeArmed)
            {
                StartForeground();
            }
        });
        _holdMonitor.HoldEnded += () => UiInvoke(() =>
        {
            if (_holdModeArmed)
            {
                StopEngine();
            }
        });

        _recorder.EventCaptured += n => UiInvoke(() =>
        {
            _lblEventCount.Text = Loc.F("Record.EventsCaptured", n);
            _lblState.Text = Loc.F("Record.RecordingStatus", n);
        });

        _player.PlaybackStarted += () => UiInvoke(() =>
        {
            _btnPlayToggle.Text = Loc.F("Record.StopPlayback", _settings.TogglePlaybackHotkey.ToDisplayString());
            UpdateStatusLabel();
        });
        _player.PlaybackFinished += () => UiInvoke(() =>
        {
            UpdatePlayButtonEnabled();
            _btnPlayToggle.Text = PlayButtonLabel();
            _lblPlaybackProgress.Text = Loc.T("Common.Idle");
            UpdateStatusLabel();
        });
        _player.RepeatCompleted += n => UiInvoke(() => { _lblPlaybackProgress.Text = Loc.F("Record.RepeatCompleted", n); });
    }

    private void WireControlEvents()
    {
        _btnStart.Click += (_, _) => ToggleClicker();
        _btnStop.Click += (_, _) => StopClicker();
        _chkHoldMode.CheckedChanged += (_, _) =>
        {
            if (_chkHoldMode.Checked)
            {
                // Selecting hold mode changes how the next clicker start behaves,
                // but does not arm the mouse hook by itself.
                if (_engineMode == EngineMode.Foreground)
                {
                    StopEngine();
                }
            }
            else
            {
                DisarmHoldMode();
            }

            UpdateButtonLabels();
            UpdateStatusHotkeyLabel();
            UpdateEngineButtons();
        };
        _cboButton.SelectedIndexChanged += (_, _) => _holdMonitor.TriggerButton = GetSelectedMouseButton();
        _btnPickLocation.Click += (_, _) =>
        {
            Point? pt = PickLocationOverlay.Pick();
            if (pt.HasValue)
            {
                _radPosFixed.Checked = true;
                _numFixedX.Value = Math.Clamp(pt.Value.X, -32768, 32767);
                _numFixedY.Value = Math.Clamp(pt.Value.Y, -32768, 32767);
            }
        };

        _btnRecordToggle.Click += (_, _) => { if (_recorder.IsRecording) StopRecording(); else StartRecording(); };
        _btnPlayToggle.Click += (_, _) => { if (_player.IsPlaying) StopPlayback(); else StartPlayback(); };
        _btnSaveMacro.Click += OnSaveMacroClick;
        _btnLoadMacro.Click += OnLoadMacroClick;

        _lvTargets.ItemChecked += (_, e) =>
        {
            if (e.Item.Tag is WindowTargetInfo info)
            {
                info.Enabled = e.Item.Checked;
            }
        };
        _btnAddTarget.Click += OnAddTargetClick;
        _btnRemoveTarget.Click += (_, _) =>
        {
            foreach (ListViewItem item in _lvTargets.SelectedItems.Cast<ListViewItem>().ToList())
            {
                _lvTargets.Items.Remove(item);
            }
        };
        _btnTestClick.Click += (_, _) =>
        {
            if (_lvTargets.SelectedItems.Count > 0 && _lvTargets.SelectedItems[0].Tag is WindowTargetInfo info)
            {
                new WindowTarget(info).PerformClick(BuildClickSettingsFromTab1(), new Random());
            }
        };
        _btnStartBackground.Click += (_, _) => StartBackground();
        _btnStopBackground.Click += (_, _) => StopEngine();

        _hkClicker.SuspendRequested += suspended => _hotkeyManager.Suspended = suspended;
        _hkRecord.SuspendRequested += suspended => _hotkeyManager.Suspended = suspended;
        _hkPlayback.SuspendRequested += suspended => _hotkeyManager.Suspended = suspended;
        _hkClicker.ComboChanged += combo => OnHotkeyComboChanged("toggle-clicker", combo, c => _settings.ToggleClickerHotkey = c);
        _hkRecord.ComboChanged += combo => OnHotkeyComboChanged("toggle-record", combo, c => _settings.ToggleRecordHotkey = c);
        _hkPlayback.ComboChanged += combo => OnHotkeyComboChanged("toggle-playback", combo, c => _settings.TogglePlaybackHotkey = c);

        _chkAlwaysOnTop.CheckedChanged += (_, _) => TopMost = _chkAlwaysOnTop.Checked;
    }

    private void OnHotkeyComboChanged(string actionId, HotkeyCombo combo, Action<HotkeyCombo> assign)
    {
        assign(combo);
        if (combo.IsEmpty)
        {
            _hotkeyManager.Unregister(actionId);
        }
        else
        {
            _hotkeyManager.Register(actionId, combo);
        }

        UpdateButtonLabels();
        UpdateStatusHotkeyLabel();
    }

    private void OnGithubLinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://github.com/") { UseShellExecute = true });
        }
        catch
        {
            // Ignore failures to launch a browser.
        }
    }

    private void OnAddTargetClick(object? sender, EventArgs e)
    {
        Hide();
        WindowTargetInfo? picked;
        try
        {
            picked = WindowPicker.PickTarget(this);
        }
        finally
        {
            Show();
        }

        if (picked != null)
        {
            AddTargetToList(picked);
        }
    }

    private void OnSaveMacroClick(object? sender, EventArgs e)
    {
        if (_currentScript == null || _currentScript.Events.Count == 0)
        {
            MessageBox.Show(this, Loc.T("Message.NoMacroToSave"), Loc.T("App.Title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using SaveFileDialog dlg = new()
        {
            Filter = Loc.T("Filter.Macro"),
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            FileName = "macro.ocmacro.json",
        };

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                _currentScript.Save(dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Loc.F("Message.SaveMacroFailed", ex.Message), Loc.T("App.Title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OnLoadMacroClick(object? sender, EventArgs e)
    {
        using OpenFileDialog dlg = new()
        {
            Filter = Loc.T("Filter.Macro"),
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                _currentScript = MacroScript.Load(dlg.FileName);
                _macroInfoState = MacroInfoState.Loaded;
                _lblMacroInfo.Text = Loc.F("Record.LoadedEvents", _currentScript.Events.Count);
                _lblEventCount.Text = Loc.F("Record.EventsRecorded", _currentScript.Events.Count);
                UpdatePlayButtonEnabled();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Loc.F("Message.LoadMacroFailed", ex.Message), Loc.T("App.Title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ================================================================
    // Hotkey dispatch
    // ================================================================

    private void OnHotkeyPressed(string actionId)
    {
        switch (actionId)
        {
            case "toggle-clicker":
                ToggleClicker();
                break;
            case "toggle-record":
                if (_recorder.IsRecording) StopRecording(); else StartRecording();
                break;
            case "toggle-playback":
                if (_player.IsPlaying) StopPlayback(); else StartPlayback();
                break;
        }
    }

    // ================================================================
    // Engine / recorder / player actions
    // ================================================================

    private void ToggleClicker()
    {
        if (_chkHoldMode.Checked)
        {
            if (_holdModeArmed)
            {
                DisarmHoldMode();
            }
            else
            {
                ArmHoldMode();
            }

            return;
        }

        if (_engine.IsRunning)
        {
            StopEngine();
        }
        else
        {
            StartForeground();
        }
    }

    private void StopClicker()
    {
        if (_chkHoldMode.Checked)
        {
            DisarmHoldMode();
        }
        else
        {
            StopEngine();
        }
    }

    private void ArmHoldMode()
    {
        if (_recorder.IsRecording)
        {
            StopRecording();
        }

        // The clicker hotkey is the explicit activation step. Stop any other
        // engine mode before arming so a physical hold cannot overlap it.
        if (_engine.IsRunning)
        {
            StopEngine();
        }

        _holdModeArmed = true;
        _holdMonitor.TriggerButton = GetSelectedMouseButton();
        _holdMonitor.Enabled = true;
        UpdateEngineButtons();
        UpdateStatusLabel();
    }

    private void DisarmHoldMode()
    {
        _holdModeArmed = false;
        _holdMonitor.Enabled = false;

        if (_engine.IsRunning && _engineMode == EngineMode.Foreground)
        {
            StopEngine();
        }
        else
        {
            UpdateEngineButtons();
            UpdateStatusLabel();
        }
    }

    private void StartForeground()
    {
        if (_recorder.IsRecording) StopRecording();
        if (_engine.IsRunning) _engine.Stop();

        _engineMode = EngineMode.Foreground;
        _engine.Start(BuildClickSettingsFromTab1(), new ForegroundTarget());
        UpdateEngineButtons();
        UpdateStatusLabel();
    }

    private void StartBackground()
    {
        List<WindowTargetInfo> targets = GetBackgroundTargetInfos();
        if (targets.Count == 0)
        {
            MessageBox.Show(this, Loc.T("Message.NoBackgroundTarget"), Loc.T("App.Title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_recorder.IsRecording) StopRecording();
        if (_engine.IsRunning) _engine.Stop();

        _engineMode = EngineMode.Background;
        _engine.Start(BuildClickSettingsFromTab1(), new MultiTarget(targets));
        UpdateEngineButtons();
        UpdateStatusLabel();
    }

    private void StopEngine()
    {
        _engine.Stop();
        _engineMode = EngineMode.None;
        UpdateEngineButtons();
        UpdateStatusLabel();
    }

    private void StartRecording()
    {
        if (_player.IsPlaying) StopPlayback();

        _recorder.IgnoreKeys = BuildIgnoreKeysForRecordHotkey();
        _recorder.Start();
        UpdateRecordButton();
        UpdateStatusLabel();
    }

    private void StopRecording()
    {
        _currentScript = _recorder.StopAndGet();
        _macroInfoState = MacroInfoState.Recorded;
        _lblEventCount.Text = Loc.F("Record.EventsRecorded", _currentScript.Events.Count);
        _lblMacroInfo.Text = Loc.F("Record.EventsRecorded", _currentScript.Events.Count);
        UpdateRecordButton();
        UpdatePlayButtonEnabled();
        UpdateStatusLabel();
    }

    private void StartPlayback()
    {
        if (_recorder.IsRecording) StopRecording();
        if (_currentScript == null || _currentScript.Events.Count == 0) return;

        double speed = ParseSpeed();
        int repeat = (int)_numRepeatPlayback.Value;
        _player.Start(_currentScript, speed, repeat);
    }

    private void StopPlayback() => _player.Stop();

    // ================================================================
    // UI state helpers
    // ================================================================

    private void UpdateEngineButtons()
    {
        bool holdModeOn = _chkHoldMode.Checked;
        bool running = _engine.IsRunning;

        if (holdModeOn)
        {
            _btnStart.Enabled = !_holdModeArmed;
            _btnStop.Enabled = _holdModeArmed;
        }
        else
        {
            _btnStart.Enabled = !(running && _engineMode == EngineMode.Foreground);
            _btnStop.Enabled = running && _engineMode == EngineMode.Foreground;
        }

        _btnStartBackground.Enabled = !_holdModeArmed && !(running && _engineMode == EngineMode.Background);
        _btnStopBackground.Enabled = running && _engineMode == EngineMode.Background;
    }

    private void UpdateRecordButton()
    {
        _btnRecordToggle.Text = RecordButtonLabel();
    }

    private void UpdatePlayButtonEnabled()
    {
        bool hasScript = _currentScript != null && _currentScript.Events.Count > 0;
        _btnPlayToggle.Enabled = hasScript || _player.IsPlaying;
        _btnPlayToggle.Text = PlayButtonLabel();
    }

    private void UpdateStatusLabel()
    {
        if (_recorder.IsRecording)
        {
            // Left as-is; EventCaptured keeps this current while recording.
            return;
        }

        if (_player.IsPlaying)
        {
            _lblState.Text = Loc.T("Status.Playing");
        }
        else if (_engine.IsRunning)
        {
            _lblState.Text = Loc.F("Status.Clicking", _engine.ClicksPerformed);
        }
        else if (_holdModeArmed)
        {
            _lblState.Text = Loc.T("Status.HoldArmed");
        }
        else
        {
            _lblState.Text = Loc.T("Common.Idle");
        }
    }

    private void UpdateButtonLabels()
    {
        string clickerHotkey = _settings.ToggleClickerHotkey.ToDisplayString();
        _btnStart.Text = _chkHoldMode.Checked
            ? Loc.F("Autoclick.ArmHold", clickerHotkey)
            : Loc.F("Autoclick.Start", clickerHotkey);
        _btnStop.Text = _chkHoldMode.Checked
            ? Loc.F("Autoclick.DisarmHold", clickerHotkey)
            : Loc.F("Autoclick.Stop", clickerHotkey);
        _btnRecordToggle.Text = RecordButtonLabel();
        _btnPlayToggle.Text = PlayButtonLabel();
    }

    private string RecordButtonLabel() => _recorder.IsRecording
        ? Loc.F("Record.StopRecording", _settings.ToggleRecordHotkey.ToDisplayString())
        : Loc.F("Record.Record", _settings.ToggleRecordHotkey.ToDisplayString());

    private string PlayButtonLabel() => _player.IsPlaying
        ? Loc.F("Record.StopPlayback", _settings.TogglePlaybackHotkey.ToDisplayString())
        : Loc.F("Record.Play", _settings.TogglePlaybackHotkey.ToDisplayString());

    private void UpdateStatusHotkeyLabel()
    {
        string clickerAction = _chkHoldMode.Checked
            ? Loc.T("Status.HotkeyArmHold")
            : Loc.T("Status.HotkeyStartStop");
        _lblHotkeyReminder.Text = Loc.F(
            "Status.HotkeyReminder",
            _settings.ToggleClickerHotkey.ToDisplayString(),
            clickerAction,
            _settings.ToggleRecordHotkey.ToDisplayString(),
            _settings.TogglePlaybackHotkey.ToDisplayString());
    }

    private void RefreshEventCountLabel()
    {
        if (_recorder.IsRecording)
        {
            return;
        }

        _lblEventCount.Text = Loc.F("Record.EventsRecorded", _currentScript?.Events.Count ?? 0);
    }

    private void RefreshMacroInfoLabel()
    {
        int count = _currentScript?.Events.Count ?? 0;
        _lblMacroInfo.Text = _macroInfoState switch
        {
            MacroInfoState.Recorded => Loc.F("Record.EventsRecorded", count),
            MacroInfoState.Loaded => Loc.F("Record.LoadedEvents", count),
            _ => Loc.T("Record.NoMacro"),
        };
    }

    private void RegisterHotkeysFromSettings()
    {
        RegisterOrUnregister("toggle-clicker", _settings.ToggleClickerHotkey);
        RegisterOrUnregister("toggle-record", _settings.ToggleRecordHotkey);
        RegisterOrUnregister("toggle-playback", _settings.TogglePlaybackHotkey);
    }

    private void RegisterOrUnregister(string actionId, HotkeyCombo combo)
    {
        if (combo.IsEmpty)
        {
            _hotkeyManager.Unregister(actionId);
        }
        else
        {
            _hotkeyManager.Register(actionId, combo);
        }
    }

    // ================================================================
    // Settings <-> controls
    // ================================================================

    private void ApplySettingsToControls(AppSettings s)
    {
        _applyingLanguage = true;
        try
        {
            _cboLanguage.SelectedIndex = (int)s.Language;
        }
        finally
        {
            _applyingLanguage = false;
        }

        _numHours.Value = Math.Clamp(s.Click.IntervalHours, 0, 23);
        _numMinutes.Value = Math.Clamp(s.Click.IntervalMinutes, 0, 59);
        _numSeconds.Value = Math.Clamp(s.Click.IntervalSeconds, 0, 59);
        _numMs.Value = Math.Clamp(s.Click.IntervalMilliseconds, 0, 9999);
        _chkRandomOffset.Checked = s.Click.RandomOffsetEnabled;
        _numRandomOffsetMs.Value = Math.Clamp(s.Click.RandomOffsetMs, 0, 60000);

        _cboButton.SelectedIndex = (int)s.Click.Button;
        _cboClickType.SelectedIndex = (int)s.Click.ClickType;

        if (s.Click.RepeatMode == RepeatMode.Count) _radRepeatCount.Checked = true; else _radRepeatUntilStopped.Checked = true;
        _numRepeatCount.Value = Math.Clamp(s.Click.RepeatCount, 1, 1000000);

        if (s.Click.PositionMode == PositionMode.Fixed) _radPosFixed.Checked = true; else _radPosCurrent.Checked = true;
        _numFixedX.Value = Math.Clamp(s.Click.FixedX, -32768, 32767);
        _numFixedY.Value = Math.Clamp(s.Click.FixedY, -32768, 32767);

        _chkAlwaysOnTop.Checked = s.AlwaysOnTop;
        TopMost = s.AlwaysOnTop;

        _hkClicker.Combo = s.ToggleClickerHotkey;
        _hkRecord.Combo = s.ToggleRecordHotkey;
        _hkPlayback.Combo = s.TogglePlaybackHotkey;

        SetSpeedComboFromValue(s.PlaybackSpeed);
        _numRepeatPlayback.Value = Math.Clamp(s.PlaybackRepeatCount, 0, 10000);

        _lvTargets.Items.Clear();
        foreach (WindowTargetInfo target in s.BackgroundTargets)
        {
            AddTargetToList(target);
        }

        // Hold mode is applied last since its CheckedChanged handler reads the trigger button.
        _holdMonitor.TriggerButton = GetSelectedMouseButton();
        _holdModeArmed = false;
        _chkHoldMode.Checked = s.HoldClickMode;
        _holdMonitor.Enabled = false;
    }

    private void SaveControlsToSettings()
    {
        _settings.Click = BuildClickSettingsFromTab1();
        _settings.HoldClickMode = _chkHoldMode.Checked;
        _settings.AlwaysOnTop = _chkAlwaysOnTop.Checked;
        _settings.PlaybackSpeed = ParseSpeed();
        _settings.PlaybackRepeatCount = (int)_numRepeatPlayback.Value;
        _settings.BackgroundTargets = GetBackgroundTargetInfos();
    }

    private ClickSettings BuildClickSettingsFromTab1() => new()
    {
        IntervalHours = (int)_numHours.Value,
        IntervalMinutes = (int)_numMinutes.Value,
        IntervalSeconds = (int)_numSeconds.Value,
        IntervalMilliseconds = (int)_numMs.Value,
        RandomOffsetEnabled = _chkRandomOffset.Checked,
        RandomOffsetMs = (int)_numRandomOffsetMs.Value,
        Button = GetSelectedMouseButton(),
        ClickType = _cboClickType.SelectedIndex == 1 ? ClickType.Double : ClickType.Single,
        RepeatMode = _radRepeatCount.Checked ? RepeatMode.Count : RepeatMode.UntilStopped,
        RepeatCount = (int)_numRepeatCount.Value,
        PositionMode = _radPosFixed.Checked ? PositionMode.Fixed : PositionMode.Current,
        FixedX = (int)_numFixedX.Value,
        FixedY = (int)_numFixedY.Value,
    };

    private MouseButton GetSelectedMouseButton() => _cboButton.SelectedIndex switch
    {
        1 => MouseButton.Right,
        2 => MouseButton.Middle,
        _ => MouseButton.Left,
    };

    private List<ushort> BuildIgnoreKeysForRecordHotkey()
    {
        HotkeyCombo combo = _settings.ToggleRecordHotkey;
        List<ushort> list = new();
        if (combo.KeyCode != 0) list.Add((ushort)combo.KeyCode);
        if (combo.Ctrl) list.Add(0x11);
        if (combo.Shift) list.Add(0x10);
        if (combo.Alt) list.Add(0x12);
        if (combo.Win)
        {
            list.Add(0x5B);
            list.Add(0x5C);
        }

        return list;
    }

    private void SetSpeedComboFromValue(double value)
    {
        for (int i = 0; i < _cboSpeed.Items.Count; i++)
        {
            string text = (string)_cboSpeed.Items[i]!;
            if (double.TryParse(text.TrimEnd('x'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double parsed) &&
                Math.Abs(parsed - value) < 0.001)
            {
                _cboSpeed.SelectedIndex = i;
                return;
            }
        }

        _cboSpeed.SelectedIndex = 2; // 1x
    }

    private double ParseSpeed()
    {
        string text = (_cboSpeed.SelectedItem as string) ?? _cboSpeed.Text;
        text = text.TrimEnd('x', 'X');
        return double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v)
            ? v
            : 1.0;
    }

    private void AddTargetToList(WindowTargetInfo info)
    {
        ListViewItem item = new(string.IsNullOrEmpty(info.WindowTitle) ? Loc.T("Common.Untitled") : info.WindowTitle)
        {
            Checked = info.Enabled,
            Tag = info,
        };
        item.SubItems.Add(info.ClassName);
        item.SubItems.Add($"({info.ClientX}, {info.ClientY})");
        _lvTargets.Items.Add(item);
    }

    private List<WindowTargetInfo> GetBackgroundTargetInfos()
    {
        List<WindowTargetInfo> list = new();
        foreach (ListViewItem item in _lvTargets.Items)
        {
            if (item.Tag is WindowTargetInfo info)
            {
                list.Add(info);
            }
        }

        return list;
    }

    // ================================================================
    // UI-thread marshaling
    // ================================================================

    private void UiInvoke(Action action)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        try
        {
            BeginInvoke(action);
        }
        catch (ObjectDisposedException)
        {
            // Handle was destroyed between the check and the call; ignore.
        }
        catch (InvalidOperationException)
        {
            // Handle not yet created / being torn down; ignore.
        }
    }

    // ================================================================
    // Shutdown
    // ================================================================

    private void OnMainFormClosing(object? sender, FormClosingEventArgs e)
    {
        SaveControlsToSettings();
        SettingsStore.Save(_settings);
    }

    private void OnMainFormClosed(object? sender, FormClosedEventArgs e)
    {
        Loc.Changed -= OnLanguageChanged;
        DisposeResponsiveFonts();
        _engine.Dispose();
        _player.Dispose();
        _recorder.Dispose();
        _hotkeyManager.Dispose();
        _holdMonitor.Dispose();
    }
}
