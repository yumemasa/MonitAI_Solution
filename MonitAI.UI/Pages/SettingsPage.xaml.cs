using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MonitAI.Core; // Coreを参照

namespace MonitAI.UI.Pages
{
    public partial class SettingsPage : Page
    {
        private string ConfigPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        public SettingsPage()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void SaveApiKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = LoadSettingsFromFile();
                settings["ApiKey"] = ApiKeyBox.Password;

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);

                MessageBox.Show("APIキーを保存しました。\nAgentを再起動すると反映されます。", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void TestCli_Click(object sender, RoutedEventArgs e)
        {
            CliStatusText.Text = "⏳ テスト中...";
            CliStatusText.Foreground = Brushes.Gray;

            try
            {
                // ★修正箇所: 引数なしで初期化する
                var service = new GeminiService();

                // Coreに実装されている接続確認メソッドを呼ぶ
                bool isConnected = await service.CheckCliConnectionAsync();

                if (isConnected)
                {
                    CliStatusText.Text = "✅ Gemini CLI (gemini command) は正常に動作しています。";
                    CliStatusText.Foreground = Brushes.Green;
                }
                else
                {
                    CliStatusText.Text = "❌ Gemini CLI が見つかりません。\nPATHに 'gemini' が通っているか、Python環境を確認してください。";
                    CliStatusText.Foreground = Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                CliStatusText.Text = $"❌ エラー: {ex.Message}";
                CliStatusText.Foreground = Brushes.Red;
            }
        }

        private Dictionary<string, string> LoadSettingsFromFile()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                }
                catch { }
            }
            return new Dictionary<string, string>();
        }

        private void LoadSettings()
        {
            var settings = LoadSettingsFromFile();
            if (settings.ContainsKey("ApiKey"))
            {
                ApiKeyBox.Password = settings["ApiKey"];
            }
        }
    }
}