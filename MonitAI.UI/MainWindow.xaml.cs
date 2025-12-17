using System.Windows;
using MonitAI.UI.Pages; 
namespace MonitAI.UI
{
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow // 親クラスを正しく指定
    {
        public MainWindow()
        {
            InitializeComponent();

            // アプリ起動時にホーム画面を表示する
            RootNavigation.Navigate(typeof(Pattern1Page));
        }

        // テーマ切り替えボタン用（もしXAMLに残っていれば必要）
        private void OnToggleThemeClick(object sender, RoutedEventArgs e)
        {
            // とりあえず空でOK
        }

        // ウィンドウが閉じる時（もしXAMLに残っていれば必要）
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // とりあえず空でOK
        }
    }
}