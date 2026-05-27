using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace TestCreatorWpfApp
{
    public class PreviewRow : INotifyPropertyChanged
    {
        private string _text = string.Empty;
        private Brush _rowColor = Brushes.Black;
        private double _progress = 0;

        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); }
        }

        public Brush RowColor
        {
            get => _rowColor;
            set { _rowColor = value; OnPropertyChanged(); }
        }

        public double Progress
        {
            get => _progress;
            set { _progress = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
