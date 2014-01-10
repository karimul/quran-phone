﻿using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using Microsoft.Phone.Tasks;
using Quran.Core;

namespace Quran.WindowsPhone.Views
{
    public partial class SettingsView
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        // When page is navigated to set data context to selected item in list
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            QuranApp.SettingsViewModel.SyncViewModelWithSettings();
            DataContext = QuranApp.SettingsViewModel;

            string tab;
            NavigationContext.QueryString.TryGetValue("tab", out tab);

            if (!string.IsNullOrEmpty(tab))
            {
                if (tab == "general")
                    this.MainPivot.SelectedItem = this.General;
                if (tab == "audio")
                    this.MainPivot.SelectedItem = this.Audio;
                if (tab == "about")
                    this.MainPivot.SelectedItem = this.About;
            }
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            QuranApp.SyncViewModelsWithSettings();
            await QuranApp.DetailsViewModel.RefreshCurrentPage();
            base.OnNavigatedFrom(e);
        }
        
        private void Translations_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/TranslationListView.xaml", UriKind.Relative));
        }

        private void Reciters_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/RecitersListView.xaml", UriKind.Relative));
        }

        private void LinkTap(object sender, RoutedEventArgs e)
        {
            var link = e.OriginalSource as Hyperlink;
            if (link != null)
            {
                var task = new WebBrowserTask() {Uri = link.NavigateUri};
                task.Show();
            }
        }
    }
}