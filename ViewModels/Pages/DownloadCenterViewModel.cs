using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.ObjectModel;
using VibrantbitLauncher.Models;
using VibrantbitLauncher.Services;
using VibrantbitLauncher.Views.Pages;

namespace VibrantbitLauncher.ViewModels.Pages
{
    public class DownloadCenterViewModel : ViewModelBase
    {
        private readonly DownloadTaskService _taskService;

        public ObservableCollection<DownloadTaskItem> Tasks => _taskService.Tasks;

        public RelayCommand<DownloadTaskItem> RemoveCommand { get; }
        public RelayCommand<DownloadTaskItem> ViewDetailCommand { get; }
        public RelayCommand ClearCompletedCommand { get; }
        public RelayCommand GoToVersionDownloadCommand { get; }
        public RelayCommand GoToModDownloadCommand { get; }

        public DownloadCenterViewModel(DownloadTaskService taskService)
        {
            _taskService = taskService;
            RemoveCommand = new RelayCommand<DownloadTaskItem>(task =>
            {
                if (task != null && !task.IsActive)
                    _taskService.RemoveTask(task);
            });
            ViewDetailCommand = new RelayCommand<DownloadTaskItem>(task =>
            {
                if (task == null) return;
                Messenger.Default.Send(typeof(DownloadHubPage), "NavigateTo");
                Messenger.Default.Send(task.TaskType == DownloadTaskType.GameInstall ? 0 : 1, "DownloadHubTab");
            });
            ClearCompletedCommand = new RelayCommand(() => _taskService.ClearCompleted());
            GoToVersionDownloadCommand = new RelayCommand(() =>
            {
                Messenger.Default.Send(typeof(DownloadHubPage), "NavigateTo");
                Messenger.Default.Send(0, "DownloadHubTab");
            });
            GoToModDownloadCommand = new RelayCommand(() =>
            {
                Messenger.Default.Send(typeof(DownloadHubPage), "NavigateTo");
                Messenger.Default.Send(1, "DownloadHubTab");
            });
        }
    }
}
