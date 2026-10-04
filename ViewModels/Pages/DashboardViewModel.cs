using Flurl.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using VibrantbitLauncher.Helpers;
namespace VibrantbitLauncher.ViewModels.Pages
{
    public class DashboardPageViewModel : ObservableObject
    {
        public ObservableCollection<NewsItem> News { get; set; } = new ObservableCollection<NewsItem>();
        public bool IsLoaded { get; set; }
        public DashboardPageViewModel()
        {
            IsLoaded = false;
        }

        public async Task LoadNewsAsync()
        {
            try
            {
                var list = await MojangNewsHelper.GetNewsListAsync();
                App.Current.Dispatcher.Invoke(() =>
                {
                    News.Clear();
                    foreach (var item in list) News.Add(item);
                });
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => { /* ignore or show snackbar if available */ });
            }
            IsLoaded = true;
        }
    }



    

}

