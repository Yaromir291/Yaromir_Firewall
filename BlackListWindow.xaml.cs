using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace Yaromir_Firewall_FINAL1
{
    public partial class BlackListWindow : Window
    {
        private SettingsManager _settings;

        public BlackListWindow()
        {
            InitializeComponent();
            _settings = SettingsManager.Instance;
            UpdateLanguageFromService();
            RefreshList();
        }

        private void RefreshList()
        {
            ItemsList.Items.Clear();
            foreach (var item in _settings.BlackList)
                ItemsList.Items.Add(item);
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "*.exe|*.exe|*.*|*.*";
            dialog.Title = LanguageService.Instance.Get("BlackListWindow_AddDialog_Title");

            if (dialog.ShowDialog(this) == true)
            {
                var fullPath = dialog.FileName;
                var name = System.IO.Path.GetFileName(fullPath);

                if (!_settings.BlackList.Contains(name))
                {
                    FirewallService.Instance.BlockProgram(name, killRunning: true);
                    RefreshList();
                }
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (ItemsList.SelectedItem != null)
            {
                var name = ItemsList.SelectedItem.ToString();
                _settings.BlackList.Remove(name);
                _settings.Save();
                RefreshList();
                FirewallService.Instance.RemoveBlockRule(name);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(LanguageService.Instance.Get("BlackListWindow_ClearConfirm_Message"), 
                LanguageService.Instance.Get("BlackListWindow_ClearConfirm_Title"),
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _settings.BlackList.Clear();
                _settings.Save();
                RefreshList();
                FirewallService.Instance.RemoveAllBlockRules();
            }
        }

        public void UpdateLanguageFromService()
        {
            Title = LanguageService.Instance.Get("BlackListWindow_Title");
            
            var buttons = FindVisualChildren<System.Windows.Controls.Button>(this);
            foreach (var btn in buttons)
            {
                if (btn.Content.ToString() == "Добавить" || btn.Content.ToString() == "Add")
                    btn.Content = LanguageService.Instance.Get("BlackListWindow_AddButton");
                else if (btn.Content.ToString() == "Удалить" || btn.Content.ToString() == "Remove")
                    btn.Content = LanguageService.Instance.Get("BlackListWindow_RemoveButton");
                else if (btn.Content.ToString() == "Очистить всё" || btn.Content.ToString() == "Clear All")
                    btn.Content = LanguageService.Instance.Get("BlackListWindow_ClearButton");
            }
        }

        private List<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            List<T> list = new List<T>();
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                if (child is T tChild)
                    list.Add(tChild);
                list.AddRange(FindVisualChildren<T>(child));
            }
            return list;
        }
    }
}