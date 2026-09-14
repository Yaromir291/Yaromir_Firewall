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

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void UpdateLanguageFromService()
        {
            Title = LanguageService.Instance.Get("AboutWindow_Title");
            
            // Находим TextBlock'ы по имени или содержимому
            var stackPanel = FindName("") as System.Windows.Controls.StackPanel;
            if (stackPanel == null)
            {
                // Альтернативный способ - ищем через VisualTreeHelper
                var textBlocks = FindVisualChildren<System.Windows.Controls.TextBlock>(this);
                foreach (var tb in textBlocks)
                {
                    if (tb.Text == "Yaromir Firewall")
                        tb.Text = LanguageService.Instance.Get("AboutWindow_AppName");
                    else if (tb.Text.StartsWith("Версия") || tb.Text.StartsWith("Version"))
                        tb.Text = LanguageService.Instance.Get("AboutWindow_Version");
                    else if (tb.Text.Contains("© 2026 Яромир") || tb.Text.Contains("© 2026 Yaromir"))
                        tb.Text = LanguageService.Instance.Get("AboutWindow_Copyright");
                    else if (tb.Text.Contains("Исходный код") || tb.Text.Contains("Source code"))
                        tb.Text = LanguageService.Instance.Get("AboutWindow_LicenseInfo");
                    else if (tb.Text.Contains("Логотип") || tb.Text.Contains("Logo"))
                        tb.Text = LanguageService.Instance.Get("AboutWindow_TrademarkInfo");
                }
            }

            var closeBtn = FindName("CloseButton") as System.Windows.Controls.Button;
            if (closeBtn != null)
            {
                closeBtn.Content = LanguageService.Instance.Get("AboutWindow_CloseButton");
            }
            else
            {
                // Ищем кнопку по содержимому
                var buttons = FindVisualChildren<System.Windows.Controls.Button>(this);
                foreach (var btn in buttons)
                {
                    if (btn.Content.ToString() == "Закрыть" || btn.Content.ToString() == "Close")
                        btn.Content = LanguageService.Instance.Get("AboutWindow_CloseButton");
                }
            }
        }

        private System.Collections.Generic.List<T> FindVisualChildren<T>(System.Windows.DependencyObject depObj) where T : System.Windows.DependencyObject
        {
            System.Collections.Generic.List<T> list = new System.Collections.Generic.List<T>();
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                System.Windows.DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                if (child is T tChild)
                    list.Add(tChild);
                var grandchildren = FindVisualChildren<T>(child);
                list.AddRange(grandchildren);
            }
            return list;
        }
    }
}