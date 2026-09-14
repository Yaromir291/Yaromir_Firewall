using System.Windows;
using System.Windows.Controls;

namespace Yaromir_Firewall_FINAL1
{
    public enum PromptResult
    {
        Allow,
        Block,
        BlockAndKill
    }

    public partial class PromptWindow : Window
    {
        public PromptResult Result { get; private set; } = PromptResult.Block;
        public bool OnlyOnce { get; private set; } = true;

        public PromptWindow(string programName, string programPath, string targetInfo)
        {
            InitializeComponent();
            ProgramName.Text = programName;
            ProgramPath.Text = programPath;
            TargetInfo.Text = targetInfo;
            UpdateLanguage();
        }

        public void UpdateLanguageFromService()
        {
            UpdateLanguage();
        }

        private void UpdateLanguage()
        {
            var lang = LanguageService.Instance;
            Title = lang.Get("PromptWindow_Title");
            ((Button)((Grid)Content).Children[2]).Content = lang.Get("PromptWindow_AllowButton");
            ((Button)((Grid)Content).Children[3]).Content = lang.Get("PromptWindow_BlockButton");
            ((Button)((Grid)Content).Children[4]).Content = lang.Get("PromptWindow_BlockAndKillButton");
            ((CheckBox)((Grid)Content).Children[5]).Content = lang.Get("PromptWindow_OnceCheckBox");
            
            // Обновляем текст цели, если он уже установлен
            if (!string.IsNullOrEmpty(TargetInfo.Text) && TargetInfo.Text.Contains(":"))
            {
                var parts = TargetInfo.Text.Split(':');
                if (parts.Length >= 2)
                {
                    TargetInfo.Text = lang.Get("PromptWindow_TargetInfo", parts[parts.Length - 2] + ":" + parts[parts.Length - 1]);
                }
            }
        }

        private void Finish(PromptResult result)
        {
            Result = result;
            OnlyOnce = OnceCheckBox.IsChecked == true;
            DialogResult = true;
            Close();
        }

        private void Allow_Click(object sender, RoutedEventArgs e) => Finish(PromptResult.Allow);
        private void Block_Click(object sender, RoutedEventArgs e) => Finish(PromptResult.Block);
        private void BlockAndKill_Click(object sender, RoutedEventArgs e) => Finish(PromptResult.BlockAndKill);
    }
}