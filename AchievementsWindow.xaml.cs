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

            WarriorTitle.Text = Localization.T("Warrior_Title");
            WarriorDescription.Text = Localization.T("Warrior_Description");
            WarriorUnlocked.Text = Localization.T("Warrior_Unlocked");

            bool hasVeteran = AchievementFlags.Get(AchievementFlags.Version1_0);
            bool hasWarrior = AchievementFlags.Get(AchievementFlags.Version2_0);

            VeteranBorder.Visibility = hasVeteran ? Visibility.Visible : Visibility.Collapsed;
            WarriorBorder.Visibility = hasWarrior ? Visibility.Visible : Visibility.Collapsed;

            if (!hasVeteran && !hasWarrior)
            {
                EmptyPanel.Visibility = Visibility.Visible;
                AchievementsPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyPanel.Visibility = Visibility.Collapsed;
                AchievementsPanel.Visibility = Visibility.Visible;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            Localization.LanguageChanged -= OnLanguageChanged;
            base.OnClosed(e);
        }
    }
}