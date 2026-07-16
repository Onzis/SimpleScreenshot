using System;
using System.Windows;
using static Screenshoter.Updater;

namespace Screenshoter
{
    public partial class UpdateWindow : Window
    {
        private readonly ReleaseInfo _release;
        public bool StartUpdate { get; private set; }

        public UpdateWindow(ReleaseInfo release)
        {
            InitializeComponent();
            _release = release;

            string title = string.IsNullOrWhiteSpace(release.Name) ? release.Tag : release.Name;
            VersionText.Text = $"Версия {title}   •   у вас {CurrentVersion.ToString(3)}";
            NotesText.Text = string.IsNullOrWhiteSpace(release.Notes)
                ? "Описание изменений отсутствует."
                : CleanNotes(release.Notes);

            BtnLater.Click += (_, __) => { DialogResult = false; Close(); };
            BtnUpdate.Click += (_, __) => { StartUpdate = true; DialogResult = true; Close(); };
        }

        // Показ прогресса загрузки (окно можно оставить открытым во время скачивания).
        public void SetDownloading(double? percent)
        {
            Progress.Visibility = Visibility.Visible;
            BtnUpdate.IsEnabled = false;
            BtnLater.IsEnabled = false;
            if (percent is double p)
            {
                Progress.IsIndeterminate = false;
                Progress.Value = p;
                StatusText.Text = $"Загрузка… {p:0}%";
            }
            else
            {
                Progress.IsIndeterminate = true;
                StatusText.Text = "Загрузка…";
            }
        }

        private static string CleanNotes(string notes)
        {
            // лёгкая чистка markdown-разметки для читаемости
            var lines = notes.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var l = lines[i].TrimEnd();
                if (l.StartsWith("### ")) l = "  " + l.Substring(4);
                else if (l.StartsWith("## ")) l = l.Substring(3);
                else if (l.StartsWith("# ")) l = l.Substring(2);
                l = l.Replace("- [ ]", "•").Replace("- [x]", "✓")
                     .Replace("* ", "• ").Replace("- ", "• ");
                l = l.Replace("**", "").Replace("`", "");
                lines[i] = l;
            }
            return string.Join(Environment.NewLine, lines).Trim();
        }
    }
}
