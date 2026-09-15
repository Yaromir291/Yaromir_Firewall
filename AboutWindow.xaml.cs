using System.Windows;

namespace Yaromir_Firewall_FINAL1
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            UpdateLanguageFromService();
        }

        public void UpdateLanguageFromService()
        {
            var lang = LanguageService.Instance;
            this.Title = lang.GetResource("AboutWindow_Title");
            AppNameText.Text = lang.GetResource("AboutWindow_AppName");
            VersionText.Text = lang.GetResource("AboutWindow_Version");
            CopyrightText.Text = lang.GetResource("AboutWindow_Copyright");
            LicenseInfoText.Text = lang.GetResource("AboutWindow_LicenseInfo");
            TrademarkInfoText.Text = lang.GetResource("AboutWindow_TrademarkInfo");
            CloseButton.Content = lang.GetResource("AboutWindow_CloseButton");
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}