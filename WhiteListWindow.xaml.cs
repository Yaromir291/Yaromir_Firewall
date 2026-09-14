using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace Yaromir_Firewall_FINAL1
{
    public partial class WhiteListWindow : Window
    {
        private SettingsManager _settings;

        public WhiteListWindow()
        {
            InitializeComponent();
            _settings = SettingsManager.Instance;
            UpdateLanguageFromService();
            RefreshList();
        }

        private void RefreshList()
        {
            ItemsList.Items.Clear();
            foreach (var item in _settings.WhiteList)
                ItemsList.Items.Add(item);
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "*.exe|*.exe|*.*|*.*";
            dialog.Title = LanguageService.Instance.Get("WhiteListWindow_AddDialog_Title");

            if (dialog.ShowDialog(this) == true)
            {
                var name = System.IO.Path.GetFileName(dialog.FileName);
                if (!_settings.WhiteList.Contains(name))
                {
                    _settings.WhiteList.Add(name);
                    _settings.Save();
                    RefreshList();
                }
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (ItemsList.SelectedItem != null)
            {
                var name = ItemsList.SelectedItem.ToString();
                _settings.WhiteList.Remove(name);
                _settings.Save();
                RefreshList();
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(LanguageService.Instance.Get("WhiteListWindow_ClearButton"), LanguageService.Instance.Get("WhiteListWindow_Title"),
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _settings.WhiteList.Clear();
                _settings.Save();
                RefreshList();
            }
        }

        public void UpdateLanguageFromService()
        {
            Title = LanguageService.Instance.Get("WhiteListWindow_Title");
            
            var buttons = FindVisualChildren<System.Windows.Controls.Button>(this);
            foreach (var btn in buttons)
            {
                if (btn.Content.ToString() == "Добавить" || btn.Content.ToString() == "Add")
                    btn.Content = LanguageService.Instance.Get("WhiteListWindow_AddButton");
                else if (btn.Content.ToString() == "Удалить" || btn.Content.ToString() == "Remove")
                    btn.Content = LanguageService.Instance.Get("WhiteListWindow_RemoveButton");
                else if (btn.Content.ToString() == "Очистить всё" || btn.Content.ToString() == "Clear All")
                    btn.Content = LanguageService.Instance.Get("WhiteListWindow_ClearButton");
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