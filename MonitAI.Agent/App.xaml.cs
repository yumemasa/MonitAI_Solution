using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using MonitAI.Core;
using WinForms = System.Windows.Forms; // System.Windows.Formsのエイリアス

namespace MonitAI.Agent
{
    public partial class App : Application
    {
        private GeminiService? _geminiService;
        private ScreenshotService? _screenshotService;
        private InterventionService? _interventionService;
        private DispatcherTimer? _timer;

        private string _userRules = "サボらず作業すること";
        private string _apiKey = "";

        // 違反ポイントの管理（Coreの仕組みに合わせて追加）
        private int _violationPoints = 0;
        private const string TargetModel = "gemini-1.5-flash";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (!LoadSettings())
            {
                // 設定が見つからなくても、後で設定されることを期待して起動は継続
            }

            // サービスの初期化
            _geminiService = new GeminiService();
            _screenshotService = new ScreenshotService();
            _interventionService = new InterventionService();

            // ログイベントをデバッグ出力に繋ぐ
            if (_interventionService != null)
            {
                _interventionService.OnLog += msg => System.Diagnostics.Debug.WriteLine($"[Intervention] {msg}");
            }

            // タイマー開始 (45秒間隔)
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(45);
            _timer.Tick += async (s, args) => await MonitoringLoop();
            _timer.Start();
        }

        private async Task MonitoringLoop()
        {
            if (_screenshotService == null || _geminiService == null || _interventionService == null) return;
            if (string.IsNullOrEmpty(_apiKey)) return;

            try
            {
                // --- A. スクリーンショット撮影 ---
                string saveDir = Path.Combine(Path.GetTempPath(), "MonitAI_Screenshots");
                Directory.CreateDirectory(saveDir);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                var screen = WinForms.Screen.PrimaryScreen;
                if (screen == null) return;

                // Coreの定義に合わせて引数を渡す
                string imagePath = _screenshotService.CaptureScreen(
                    screen,
                    0,
                    timestamp,
                    saveDir
                );

                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath)) return;

                // --- B. 画像分析 ---
                var imagePaths = new List<string> { imagePath };

                // CoreのAnalyzeAsync呼び出し
                GeminiAnalysisResult result = await _geminiService.AnalyzeAsync(
                    imagePaths,
                    _userRules,
                    _apiKey,
                    TargetModel
                );

                // --- C. 判定と介入（ポイント制） ---
                if (result.IsViolation)
                {
                    // 違反時はポイントを加算 (例: +20pt)
                    // CoreのApplyLevelAsyncに合わせてロジックを実装
                    _violationPoints += 20;

                    // 上限キャップ (レベル7の強制シャットダウン手前で止めるか、突き抜けるかは調整可)
                    if (_violationPoints > 300) _violationPoints = 300;

                    System.Diagnostics.Debug.WriteLine($"違反検知！現在のポイント: {_violationPoints}");

                    // ★修正箇所: ApplyLevelAsync を呼ぶ (引数: ポイント, 目標テキスト)
                    await _interventionService.ApplyLevelAsync(_violationPoints, _userRules);
                }
                else
                {
                    // 正常時はポイントを減らす (例: -10pt)
                    if (_violationPoints > 0)
                    {
                        _violationPoints -= 10;
                        if (_violationPoints < 0) _violationPoints = 0;

                        // 0になったら介入リセットを呼ぶ（ApplyLevelAsync(0,...)でリセットされる仕様を利用）
                        await _interventionService.ApplyLevelAsync(_violationPoints, _userRules);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"監視ループエラー: {ex.Message}");
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

                    if (settings != null)
                    {
                        if (settings.TryGetValue("ApiKey", out var key)) _apiKey = key;
                        if (settings.TryGetValue("Rules", out var rules)) _userRules = rules;

                        if (settings.TryGetValue("UseCli", out var useCliVal) && bool.TryParse(useCliVal, out bool useCli))
                        {
                            if (_geminiService != null) _geminiService.UseGeminiCli = useCli;
                        }
                        return !string.IsNullOrEmpty(_apiKey);
                    }
                }
            }
            catch { }
            return false;
        }

        // アプリ終了時にリソース解放
        protected override void OnExit(ExitEventArgs e)
        {
            _interventionService?.Dispose();
            base.OnExit(e);
        }
    }
}