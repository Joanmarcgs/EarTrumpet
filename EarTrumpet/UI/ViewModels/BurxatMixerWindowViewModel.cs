using EarTrumpet.UI.Helpers;
using System.Windows.Input;

namespace EarTrumpet.UI.ViewModels
{
    // The window itself only ever shows one of these two mixers at a time - Output or Input -
    // switched by the toggle at the top. Both are kept alive the whole time so switching back
    // and forth doesn't lose fader state or re-scan devices.
    public class BurxatMixerWindowViewModel : BindableBase
    {
        public BurxatMixerViewModel Output { get; }
        public BurxatMixerViewModel Input { get; }
        public BurxatMixerViewModel SelectedMixer => IsShowingInput ? Input : Output;

        public bool IsShowingOutput => !IsShowingInput;
        public bool IsShowingInput
        {
            get => _isShowingInput;
            set
            {
                if (_isShowingInput != value)
                {
                    _isShowingInput = value;
                    RaisePropertyChanged(nameof(IsShowingInput));
                    RaisePropertyChanged(nameof(IsShowingOutput));
                    RaisePropertyChanged(nameof(SelectedMixer));
                }
            }
        }

        public ICommand ShowOutput { get; }
        public ICommand ShowInput { get; }

        private bool _isShowingInput;

        public BurxatMixerWindowViewModel(DeviceCollectionViewModel outputDevices, DeviceCollectionViewModel inputDevices)
        {
            Output = new BurxatMixerViewModel(outputDevices, MixerDeviceKind.Output);
            Input = new BurxatMixerViewModel(inputDevices, MixerDeviceKind.Input);
            ShowOutput = new RelayCommand(() => IsShowingInput = false);
            ShowInput = new RelayCommand(() => IsShowingInput = true);
        }

        public void Cleanup()
        {
            Output.Cleanup();
            Input.Cleanup();
        }
    }
}
