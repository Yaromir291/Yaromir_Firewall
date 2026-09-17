using System;
using System.Windows;

namespace Yaromir_Firewall_FINAL1
{
    public partial class AchievementsWindow : Window
    {
        public AchievementsWindow()
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
            Title = Localization.T("Achievements");
            HeaderText.Text = Localization.T("Achievements");
            EmptyText.Text = Localization.T("InDevelopment");
            VeteranTitle.Text = Localization.T("Veteran_Title");
            VeteranDescription.Text = Localization.T("Veteran_Description");
            VeteranUnlocked.Text = Localization.T("Veteran_Unlocked");

            // Показываем нужную панель в зависимости от флага
            if (SettingsManager.Instance.HadVersion1_0)
            {
                EmptyPanel.Visibility = Visibility.Collapsed;
                VeteranPanel.Visibility = Visibility.Visible;
            }
            else
            {
                EmptyPanel.Visibility = Visibility.Visible;
                VeteranPanel.Visibility = Visibility.Collapsed;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            Localization.LanguageChanged -= OnLanguageChanged;
            base.OnClosed(e);
        }
    }
}