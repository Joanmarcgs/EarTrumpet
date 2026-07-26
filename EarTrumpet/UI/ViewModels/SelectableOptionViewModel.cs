using EarTrumpet.UI.Helpers;
using System.Windows.Input;

namespace EarTrumpet.UI.ViewModels
{
    // One choice in a small settings picker (scale, theme, ...): a label, whether it's the
    // current choice, and a command that makes it so.
    public class SelectableOptionViewModel : BindableBase
    {
        public string Label { get; }
        public ICommand Select { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    RaisePropertyChanged(nameof(IsSelected));
                }
            }
        }

        private bool _isSelected;

        public SelectableOptionViewModel(string label, System.Action onSelected)
        {
            Label = label;
            Select = new RelayCommand(onSelected);
        }
    }
}
