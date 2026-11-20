using OpenClick.Core;
using OpenClick.Localization;

namespace OpenClick;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // The saved language must be applied before any control is created so the first
        // layout pass already uses the right texts and font.
        Loc.Initialize(SettingsStore.Load().Language);
        ApplicationConfiguration.Initialize();
        Application.Run(new UI.MainForm());
    }
}
