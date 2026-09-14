using System;
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
        public PromptResult Result { get; private set; }
        public bool OnlyOnce => OnceCheckBox.IsChecked ?? true;

        public PromptWindow(string programName, string programPath, string targetInfo)
        {
            InitializeComponent();
            ProgramName.Text = programName;
            ProgramPath.Text = programPath;
            TargetInfo.Text = targetInfo;
            Result = PromptResult.Block;
        }

        private void Allow_Click(object sender, RoutedEventArgs e)
        {
            Result = PromptResult.Allow;
            DialogResult = true;
            Close();
        }

        private void Block_Click(object sender, RoutedEventArgs e)
        {
            Result = PromptResult.Block;
            DialogResult = true;
            Close();
        }

        private void BlockAndKill_Click(object sender, RoutedEventArgs e)
        {
            Result = PromptResult.BlockAndKill;
            DialogResult = true;
            Close();
        }

        public void UpdateLanguageFromService()
        {
            var lang = LanguageService.Instance.CurrentLanguage;
            
            Title = LanguageService.Instance.GetResource("PromptWindowTitle", lang);
            AllowButton.Content = LanguageService.Instance.GetResource("AllowButton", lang);
            BlockButton.Content = LanguageService.Instance.GetResource("BlockButton", lang);
            BlockAndKillButton.Content = LanguageService.Instance.GetResource("BlockAndKillButton", lang);
            OnceCheckBox.Content = LanguageService.Instance.GetResource("OnlyOnceCheckBox", lang);
        }
    }
}