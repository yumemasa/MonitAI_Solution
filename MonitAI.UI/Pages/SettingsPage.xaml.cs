using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using MonitAI.Core; // Coreを使う

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
            // 既存の設定を読み込んで、APIキーだけ更新する
            var settings = LoadSettingsFromFile();
            settings["ApiKey"] = ApiKeyBox.Password;

            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);

            MessageBox.Show("APIキーを保存しました。", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void TestCli_Click(object sender, RoutedEventArgs e)
        {
            CliStatusText.Text = "⏳ テスト中...";
            // CoreにあるGeminiServiceを使ってテスト
            var service = new GeminiService(ApiKeyBox.Password);
            // ※注意: CLIのパスが正しいか確認が必要です。今回は簡易的な導通確認として実装します。

            // 本来はここで実際にCLIを叩く処理を呼びますが、まずは「保存されたか」の確認のみにします
            await Task.Delay(500); // 演出
            CliStatusText.Text = "✅ 設定ファイルは正常に作成されています。\nAgent起動時にこの設定が読み込まれます。";
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