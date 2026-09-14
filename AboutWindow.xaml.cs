using System.Windows;

namespace Yaromir_Firewall_FINAL1
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void UpdateLanguageFromService()
        {
            var lang = LanguageService.Instance.CurrentLanguage;
            
            Title = LanguageService.Instance.GetResource("AboutWindowTitle", lang);
        }
    }
}