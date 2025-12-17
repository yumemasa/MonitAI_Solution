using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using MonitAI.Core;

namespace MonitAI.Agent
{
    public partial class App : Application
    {
        private GeminiService _geminiService;
        private ScreenshotService _screenshotService;
        private InterventionService _interventionService;
        private DispatcherTimer _timer;
        private string _userRules = "";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. 設定の読み込み
            if (!LoadSettings())
            {
                MessageBox.Show("設定(APIキー等)が見つかりません。\n先にMonitAI.UIで設定を行ってください。", "MonitAI Agent", MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown();
                return;
            }

            // 2. サービスの初期化
            _screenshotService = new ScreenshotService();
            _interventionService = new InterventionService();

            // ログの受け取り（デバッグ用：本来はファイルに出力などが良い）
            _geminiService.OnLog += (msg) => System.Diagnostics.Debug.WriteLine($"[Gemini] {msg}");
            _interventionService.OnLog += (msg) => System.Diagnostics.Debug.WriteLine($"[Intervention] {msg}");

            // 3. 監視タイマーの開始 (例: 45秒間隔)
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(45);
            _timer.Tick += async (s, args) => await MonitoringLoop();
            _timer.Start();

            // 最初の1回を実行
            _ = MonitoringLoop();
        }

        private async Task MonitoringLoop()
        {
            try
            {
                // A. 撮影
                string imagePath = _screenshotService.CaptureScreen();

                // B. 分析
                // (CLIモードを使うかAPIモードを使うかはCoreの実装によりますが、ここではAPIモードの例)
                string analysisResult = await _geminiService.AnalyzeImageAsync(imagePath, _userRules);

                // C. 判定と介入
                if (analysisResult.Contains("×") || analysisResult.Contains("違反"))
                {
                    // 違反時の処理
                    await _interventionService.ExecuteInterventionAsync(1); // レベル1からスタート等のロジック
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
                if (!File.Exists(configPath)) return false;

                string json = File.ReadAllText(configPath);
                var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                if (settings != null && settings.ContainsKey("ApiKey"))
                {
                    _geminiService = new GeminiService(settings["ApiKey"]);
                    if (settings.ContainsKey("Rules")) _userRules = settings["Rules"];
                    return true;
                }
            }
            catch { }
            return false;
        }
    }
}