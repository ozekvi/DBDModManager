using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

#nullable enable
namespace DbdModManager.Controls
{
    public partial class ToastHost : UserControl
    {
        private readonly ObservableCollection<ToastMessage> _toasts = new();

        public ToastHost()
        {
            InitializeComponent();
            ToastItemsControl.ItemsSource = _toasts;
        }

        public void Show(string message, ToastKind kind = ToastKind.Info, int durationMs = 4000)
        {
            var toast = new ToastMessage(message, kind);
            _toasts.Add(toast);

            var dismissTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
            dismissTimer.Tick += (s, e) =>
            {
                dismissTimer.Stop();
                Dismiss(toast);
            };
            dismissTimer.Start();
        }

        private void Dismiss(ToastMessage toast)
        {
            if (!_toasts.Contains(toast)) return;
            toast.IsClosing = true;

            var removeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(220) };
            removeTimer.Tick += (s, e) =>
            {
                removeTimer.Stop();
                _toasts.Remove(toast);
            };
            removeTimer.Start();
        }

        private void CloseToast_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ToastMessage toast)
            {
                Dismiss(toast);
            }
        }
    }
}
