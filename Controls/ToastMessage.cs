using System.ComponentModel;
using System.Runtime.CompilerServices;

#nullable enable
namespace DbdModManager.Controls
{
    public enum ToastKind
    {
        Success,
        Error,
        Warning,
        Info
    }

    public class ToastMessage : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Message { get; }
        public ToastKind Kind { get; }

        private bool _isClosing;
        public bool IsClosing
        {
            get => _isClosing;
            set
            {
                if (_isClosing == value) return;
                _isClosing = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsClosing)));
            }
        }

        public ToastMessage(string message, ToastKind kind)
        {
            Message = message;
            Kind = kind;
        }
    }
}
