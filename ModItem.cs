using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

#nullable enable
namespace DbdModManager
{
    public class ModItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _name = "";
        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        private string _path = "";
        public string Path
        {
            get => _path;
            set => SetField(ref _path, value);
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetField(ref _isSelected, value))
                {
                    OnPropertyChanged(nameof(SelectionBrush));
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(StatusBrush));
                }
            }
        }

        public Brush SelectionBrush => IsSelected
            ? (Brush)System.Windows.Application.Current.Resources["AccentBrush"]
            : new SolidColorBrush(Colors.Transparent);

        public string StatusText => IsSelected ? "ACTIVE" : "DISABLED";

        public Color StatusColor => IsSelected
            ? (Color)System.Windows.Application.Current.Resources["AccentColor"]
            : (Color)ColorConverter.ConvertFromString("#444444");

        public Brush StatusBrush => new SolidColorBrush(StatusColor);

        public void RefreshThemeBrushes()
        {
            OnPropertyChanged(nameof(SelectionBrush));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(StatusBrush));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
