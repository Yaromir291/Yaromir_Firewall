using System;
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

            Localization.LanguageChanged += OnLanguageChanged;

            ProgramName.Text = programName;
            ProgramPath.Text = programPath;
            TargetInfo.Text = targetInfo;

            RefreshLocalization();
        }

        private void OnLanguageChanged()
        {
            Dispatcher.Invoke(RefreshLocalization);
        }

        private void RefreshLocalization()
        {
            Title = Localization.T("NewConnection");
            AllowButton.Content = Localization.T("Allow");
            BlockButton.Content = Localization.T("Block");
            BlockAndKillButton.Content = Localization.T("BlockAndKill");
            OnceCheckBox.Content = Localization.T("OnlyOnce");
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

        protected override void OnClosed(EventArgs e)
        {
            Localization.LanguageChanged -= OnLanguageChanged;
            base.OnClosed(e);
        }
    }
}