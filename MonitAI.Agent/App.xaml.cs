using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using MonitAI.Core;
// FormsのScreenクラスを使うためにエイリアスを指定
using WinForms = System.Windows.Forms;

namespace MonitAI.Agent
{
    public partial class App : Application
    {
        // null許容にして警告を消す
        private GeminiService? _geminiService;
        private ScreenshotService? _screenshotService;
        private InterventionService? _interventionService;
        private DispatcherTimer? _timer;
        private string _userRules = "";
        private string _apiKey = "";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (!LoadSettings())
            {
                // 設定がなければ終了
                Shutdown();
                return;
            }

            _geminiService = new GeminiService();
            _screenshotService = new ScreenshotService();
            _interventionService = new InterventionService();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(45);
            _timer.Tick += async (s, args) => await MonitoringLoop();
            _timer.Start();
        }

        private async Task MonitoringLoop()
        {
            if (_screenshotService == null || _geminiService == null || _interventionService == null) return;

            try
            {
                // 1. スクショ撮影 (Coreの引数要求に合わせる)
                // 引数: Screen, index, 保存先フォルダ, ファイル名
                string saveDir = Path.Combine(Path.GetTempPath(), "MonitAI_Screenshots");
                Directory.CreateDirectory(saveDir);

                string imagePath = _screenshotService.CaptureScreen(
                    WinForms.Screen.PrimaryScreen,
                    0,
                    saveDir,
                    "monitor"
                );

                if (string.IsNullOrEmpty(imagePath)) return;

                // 2. 分析 (Coreの引数要求に合わせる)
                // CoreにはAPIキーを渡す場所がないようですが、AnalyzeImageの第2引数(prompt)にルール等を渡します
                // ※もしAPIキーが必要ならCore側の改修が必要ですが、まずはメソッド定義通りに呼びます
                string result = await _geminiService.AnalyzeImageAsync(imagePath, _userRules);

                // 3. 介入 (Coreの引数要求に合わせる)
                // ExecuteIntervention は string を要求している
                if (result.Contains("違反") || result.Contains("×"))
                {
                    await _interventionService.ExecuteInterventionAsync(1);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            }
        }

        private bool LoadSettings()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                    if (settings != null && settings.TryGetValue("ApiKey", out var key))
                    {
                        _apiKey = key; // APIキーは読み込むが、渡す先がCoreに見当たらないため一旦保持
                        if (settings.TryGetValue("Rules", out var rules)) _userRules = rules;
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }
    }
}