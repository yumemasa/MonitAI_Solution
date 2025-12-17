using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Threading;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace MonitAI.UI
{
    // データモデル
    public class SessionItem
    {
        public string Title { get; set; }
        public string NgText { get; set; }
        public int Minutes { get; set; }
        public string Timestamp { get; set; }

        // JSON保存対象外にする一時フラグ
        [JsonIgnore]
        public bool IsTransient { get; set; } = false;
    }

    // 保存用データ構造
    public class AppData
    {
        public List<SessionItem> Favorites { get; set; } = new List<SessionItem>();
        public List<SessionItem> Histories { get; set; } = new List<SessionItem>();
    }

    public partial class MainWindow : FluentWindow
    {
        private const string DataFileName = "user_data.json";

        public ObservableCollection<SessionItem> Favorites { get; set; } = new ObservableCollection<SessionItem>();
        public ObservableCollection<SessionItem> Histories { get; set; } = new ObservableCollection<SessionItem>();

        private SessionItem _undoBackup;

        public MainWindow()
        {
            InitializeComponent();

            FavoritesList.ItemsSource = Favorites;
            HistoryList.ItemsSource = Histories;
            RootNavigation.Navigate(typeof(Pages.Pattern1Page));
            LoadUserData();
            UpdateTimerDisplay();
        }

        // アプリ終了時
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveUserData();
        }

        // データ読み込み
        private void LoadUserData()
        {
            try
            {
                if (File.Exists(DataFileName))
                {
                    string json = File.ReadAllText(DataFileName);
                    var data = JsonSerializer.Deserialize<AppData>(json);
                    if (data != null)
                    {
                        Favorites.Clear();
                        foreach (var item in data.Favorites) Favorites.Add(item);

                        Histories.Clear();
                        foreach (var item in data.Histories) Histories.Add(item);
                    }
                }
                else
                {
                    // 初回起動時のサンプルデータ
                    Favorites.Add(new SessionItem { Title = "英単語暗記", NgText = "スマホ", Minutes = 30 });
                    Histories.Add(new SessionItem { Title = "読書", Minutes = 45, Timestamp = "サンプル" });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load Error: {ex.Message}");
            }
        }

        // データ保存 (一時アイテムは履歴から除外)
        private void SaveUserData()
        {
            try
            {
                var data = new AppData
                {
                    Favorites = Favorites.ToList(),
                    // Transientフラグが立っていないものだけ保存
                    Histories = Histories.Where(h => !h.IsTransient).ToList()
                };

                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(DataFileName, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save Error: {ex.Message}");
            }
        }

        private void OnTimeSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateTimerDisplay();
        }

        private void OnPresetTimeClick(object sender, RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.Button btn && btn.Tag is string minutesStr && double.TryParse(minutesStr, out double minutes))
            {
                TimeSlider.Value = minutes;
            }
        }

        private void UpdateTimerDisplay()
        {
            if (TimeDisplay == null || EndTimeText == null) return;
            int minutes = (int)TimeSlider.Value;
            TimeDisplay.Text = minutes.ToString();
            DateTime endTime = DateTime.Now.AddMinutes(minutes);
            EndTimeText.Text = $"End {endTime:HH:mm}";
        }

        private void OnToggleThemeClick(object sender, RoutedEventArgs e)
        {
            var currentTheme = ApplicationThemeManager.GetAppTheme();
            var newTheme = currentTheme == ApplicationTheme.Light ? ApplicationTheme.Dark : ApplicationTheme.Light;
            ApplicationThemeManager.Apply(newTheme);
        }

        // 反映 (Undo対応)
        private void OnQuickItemClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is SessionItem item)
            {
                _undoBackup = new SessionItem
                {
                    Title = GoalInput.Text,
                    NgText = NgInput.Text,
                    Minutes = (int)TimeSlider.Value
                };

                GoalInput.Text = item.Title;
                NgInput.Text = item.NgText ?? "";
                TimeSlider.Value = item.Minutes;

                UndoBar.Visibility = Visibility.Visible;
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (s, args) => { UndoBar.Visibility = Visibility.Collapsed; timer.Stop(); };
                timer.Start();
            }
        }

        private void OnUndoClick(object sender, RoutedEventArgs e)
        {
            if (_undoBackup != null)
            {
                GoalInput.Text = _undoBackup.Title;
                NgInput.Text = _undoBackup.NgText;
                TimeSlider.Value = _undoBackup.Minutes;
                UndoBar.Visibility = Visibility.Collapsed;
            }
        }

        // [お気に入り解除] ピン留め解除 -> 履歴へ移動 (一時)
        private void OnUnpinClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is SessionItem item)
            {
                // お気に入りから削除
                Favorites.Remove(item);

                // 履歴の先頭に追加 (保存対象外フラグON)
                item.IsTransient = true;
                Histories.Insert(0, item);

                // お気に入りリストの状態は即時保存
                SaveUserData();
            }
        }

        // [お気に入り登録] ピン留め -> 履歴から削除してお気に入りへ移動
        private void OnPinClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is SessionItem item)
            {
                // 履歴から削除
                Histories.Remove(item);

                // お気に入りに追加 (保存対象外フラグOFF)
                item.IsTransient = false;
                Favorites.Add(item);

                // 両方のリストの状態を即時保存
                SaveUserData();
            }
        }

        private void OnStartClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GoalInput.Text))
            {
                System.Windows.MessageBox.Show("目標を入力してください。", "monitAI", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // 履歴に追加 (Transientなし、永続化対象)
            var newItem = new SessionItem
            {
                Title = GoalInput.Text,
                NgText = NgInput.Text,
                Minutes = (int)TimeSlider.Value,
                Timestamp = DateTime.Now.ToString("MM/dd HH:mm"),
                IsTransient = false
            };

            Histories.Insert(0, newItem);

            if (Histories.Count > 15) Histories.RemoveAt(Histories.Count - 1);

            // 保存
            SaveUserData();

            string ngText = string.IsNullOrWhiteSpace(NgInput.Text) ? "なし" : NgInput.Text;
            System.Windows.MessageBox.Show($"監視を開始します！\n目标: {GoalInput.Text}\n時間: {TimeSlider.Value}分", "monitAI");
        }
    }
}