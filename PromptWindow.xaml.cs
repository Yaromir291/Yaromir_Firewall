using System;
using System.Windows;

namespace Yaromir_Firewall_FINAL1
{
    public enum PromptResult
    {
        /// <summary>
        /// Пользователь НЕ принял решение (закрыл окно крестиком / Alt+F4).
        ///
        /// БЫЛО: значение по умолчанию было Block. Закрытие окна крестиком молча
        /// отправляло программу в чёрный список НАВСЕГДА — без единого слова в логе.
        /// Теперь «закрыл окно» != «заблокируй».
        /// </summary>
        None,
        Allow,
        Block,
        BlockAndKill
    }

    public partial class PromptWindow : Window
    {
        public PromptResult Result { get; private set; } = PromptResult.None;
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