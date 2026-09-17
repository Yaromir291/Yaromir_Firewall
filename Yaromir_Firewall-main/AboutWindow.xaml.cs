using System;
using System.Windows;

namespace Yaromir_Firewall_FINAL1
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            Localization.LanguageChanged += OnLanguageChanged;
            RefreshLocalization();
        }

        private void OnLanguageChanged()
        {
            Dispatcher.Invoke(RefreshLocalization);
        }

        private void RefreshLocalization()
        {
            Title = Localization.T("About");
            VersionText.Text = Localization.T("Version");
            CopyrightText.Text = Localization.T("Copyright");
            LicenseAboutText.Text = Localization.T("LicenseAbout");
            TrademarkText.Text = Localization.T("Trademark");
            CloseButton.Content = Localization.T("Close");
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            Localization.LanguageChanged -= OnLanguageChanged;
            base.OnClosed(e);
        }
    }
}