using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using WpfApp = System.Windows.Application;

namespace Screenshoter;

public partial class App : WpfApp
{
    private NotifyIcon? _tray;
    private HotkeyManager? _hotkey;
    private OverlayWindow? _overlay;
    private SettingsWindow? _settings;

    private AppSettings _cfg = new();
    private ToolStripMenuItem? _captureItem;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _cfg = AppSettings.Load();
        InitTray();
        RegisterHotkey();

        _ = CheckForUpdatesAsync(silent: true);
    }

    private void InitTray()
    {
        var menu = new ContextMenuStrip
        {
            Renderer = new DarkMenuRenderer(),
            BackColor = DarkColorTable.Background,
            ForeColor = System.Drawing.Color.White,
            ShowImageMargin = false,
            Font = new System.Drawing.Font("Segoe UI", 9.5f),
            Padding = new Padding(4)
        };
        _captureItem = new ToolStripMenuItem(CaptureMenuText(), null, (_, __) => StartCapture());
        menu.Items.Add(_captureItem);
        menu.Items.Add("Настройки…", null, (_, __) => OpenSettings());
        menu.Items.Add("Проверить обновления", null, (_, __) => _ = CheckForUpdatesAsync(silent: false));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, __) => ExitApp());
        foreach (ToolStripItem item in menu.Items)
            if (item is ToolStripMenuItem mi) { mi.Padding = new Padding(8, 2, 8, 2); mi.AutoSize = true; }

        _tray = new NotifyIcon
        {
            Icon = LoadAppIcon(),
            Visible = true,
            Text = "SnapFlow",
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += (_, __) => StartCapture();
    }

    private string CaptureMenuText() => $"Скриншот области ({_cfg.ToDisplayString()})";

    private static System.Drawing.Icon LoadAppIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/app.ico");
            var info = GetResourceStream(uri);
            if (info != null) return new System.Drawing.Icon(info.Stream, new System.Drawing.Size(32, 32));
        }
        catch { /* fallback */ }
        return System.Drawing.SystemIcons.Application;
    }

    private void RegisterHotkey()
    {
        _hotkey?.Dispose();
        _hotkey = null;
        try
        {
            _hotkey = new HotkeyManager(_cfg.ToWin32Mod(), _cfg.ToVirtualKey());
            _hotkey.HotkeyPressed += StartCapture;
        }
        catch (Exception ex)
        {
            _tray?.ShowBalloonTip(4000, "SnapFlow",
                $"Горячая клавиша {_cfg.ToDisplayString()} недоступна. Используйте меню в трее.\n{ex.Message}",
                ToolTipIcon.Warning);
        }
    }

    private void OpenSettings()
    {
        if (_settings != null) { _settings.Activate(); return; }

        _settings = new SettingsWindow(_cfg);
        var ok = _settings.ShowDialog() == true;
        var result = _settings;
        _settings = null;

        if (!ok) return;

        _cfg.Modifiers = result.SelectedModifiers;
        _cfg.Key = result.SelectedKey;
        _cfg.Save();

        if (_captureItem != null) _captureItem.Text = CaptureMenuText();
        RegisterHotkey();
    }

    private void StartCapture()
    {
        if (_overlay != null && _overlay.IsVisible) return;

        var shot = ScreenshotHelper.CaptureVirtualScreen(
            out double left, out double top, out double width, out double height);

        _overlay = new OverlayWindow(shot, left, top, width, height);
        _overlay.Closed += (_, __) => _overlay = null;
        _overlay.Show();
        _overlay.Activate();
    }

    private bool _updating;

    private async Task CheckForUpdatesAsync(bool silent)
    {
        if (_updating) return;

        var release = await Updater.GetLatestAsync();
        if (release == null)
        {
            if (!silent)
                _tray?.ShowBalloonTip(4000, "SnapFlow",
                    "Не удалось проверить обновления. Проверьте подключение к интернету.",
                    ToolTipIcon.Warning);
            return;
        }

        if (!Updater.IsNewer(release))
        {
            if (!silent)
                _tray?.ShowBalloonTip(3000, "SnapFlow",
                    $"У вас последняя версия ({Updater.CurrentVersion.ToString(3)}).",
                    ToolTipIcon.Info);
            return;
        }

        if (string.IsNullOrEmpty(release.DownloadUrl))
        {
            _tray?.ShowBalloonTip(5000, "SnapFlow",
                $"Доступна версия {release.Tag}, но файл для загрузки не найден. Откройте страницу релиза вручную.",
                ToolTipIcon.Info);
            return;
        }

        var answer = System.Windows.MessageBox.Show(
            $"Доступна новая версия {release.Tag}\nТекущая: {Updater.CurrentVersion.ToString(3)}\n\nОбновить сейчас? Приложение перезапустится.",
            "SnapFlow — обновление",
            MessageBoxButton.YesNo, MessageBoxImage.Information);

        if (answer != MessageBoxResult.Yes) return;

        try
        {
            _updating = true;
            _tray?.ShowBalloonTip(3000, "SnapFlow", "Загрузка обновления…", ToolTipIcon.Info);
            var ok = await Updater.DownloadAndApplyAsync(release);
            if (ok)
            {
                if (_tray != null) { _tray.Visible = false; _tray.Dispose(); }
                _hotkey?.Dispose();
                Shutdown();
            }
            else
            {
                _updating = false;
                _tray?.ShowBalloonTip(4000, "SnapFlow", "Не удалось применить обновление.", ToolTipIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _updating = false;
            _tray?.ShowBalloonTip(4000, "SnapFlow", "Ошибка обновления: " + ex.Message, ToolTipIcon.Error);
        }
    }

    private void ExitApp()
    {
        if (_tray != null) { _tray.Visible = false; _tray.Dispose(); }
        _hotkey?.Dispose();
        Shutdown();
    }
}
