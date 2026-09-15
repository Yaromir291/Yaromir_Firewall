using System.Windows;

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
            TargetInfo.Text = LanguageService.Instance.GetResource("PromptWindow_TargetInfo", targetInfo);
            UpdateLanguageFromService();
        }

        public void UpdateLanguageFromService()
        {
            var lang = LanguageService.Instance;
            this.Title = lang.GetResource("PromptWindow_Title");
            AllowButton.Content = lang.GetResource("PromptWindow_AllowButton");
            BlockButton.Content = lang.GetResource("PromptWindow_BlockButton");
            BlockAndKillButton.Content = lang.GetResource("PromptWindow_BlockAndKillButton");
            OnceCheckBox.Content = lang.GetResource("PromptWindow_OnceCheckBox");
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