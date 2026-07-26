using EarTrumpet.Interop.MMDeviceAPI;
using EarTrumpet.UI.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace EarTrumpet.UI.ViewModels
{
    // A single channel strip in Burxat's Mixer: one output device, its fader,
    // and the apps currently routed to it.
    public class MixerChannelViewModel : BindableBase
    {
        public event EventHandler<int> VolumeSetByUser;

        public DeviceViewModel Device { get; }
        public string DisplayName => Device.DisplayName;
        public ICommand MakeDefault { get; }
        public ObservableCollection<ContextMenuItem> DeviceMenu { get; }
        public ObservableCollection<MixerAppViewModel> Apps { get; } = new ObservableCollection<MixerAppViewModel>();

        // 1-based position among the channel strips, kept in sync by BurxatMixerViewModel,
        // purely so each column can be labeled "Output Device 1", "Output Device 2", etc.
        public int Ordinal
        {
            get => _ordinal;
            set
            {
                if (_ordinal != value)
                {
                    _ordinal = value;
                    RaisePropertyChanged(nameof(Ordinal));
                    RaisePropertyChanged(nameof(OrdinalLabel));
                }
            }
        }
        public string OrdinalLabel => $"Output Device {Ordinal}";

        public bool IsDefault
        {
            get => _isDefault;
            set
            {
                if (_isDefault != value)
                {
                    _isDefault = value;
                    RaisePropertyChanged(nameof(IsDefault));
                }
            }
        }

        // Setting this directly (rather than through VolumeSetByUser) is how the
        // master fader drives this channel without disturbing its own baseline.
        public int Volume
        {
            get => Device.Volume;
            set
            {
                Device.Volume = value;
                VolumeSetByUser?.Invoke(this, value);
            }
        }

        private readonly DeviceCollectionViewModel _mainViewModel;
        private bool _isDefault;
        private int _ordinal;

        public MixerChannelViewModel(DeviceCollectionViewModel mainViewModel, DeviceViewModel device)
        {
            _mainViewModel = mainViewModel;
            Device = device;
            MakeDefault = new RelayCommand(Device.MakeDefaultDevice);
            DeviceMenu = new ObservableCollection<ContextMenuItem>
            {
                new ContextMenuItem { DisplayName = "Set as Default Output Device", Command = MakeDefault },
                new ContextMenuItem { DisplayName = "Disable Device", Command = new RelayCommand(ConfirmAndDisableDevice) },
            };

            Device.PropertyChanged += OnDevicePropertyChanged;
        }

        private void ConfirmAndDisableDevice()
        {
            var result = MessageBox.Show(
                $"Are you sure you want to disable \"{DisplayName}\"?\n\nYou can enable the device again from the settings menu or using the Windows Control Panel.",
                "Disable Device",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                new AutoPolicyConfigClientWin7().SetEndpointVisibility(Device.Id, false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"MixerChannelViewModel ConfirmAndDisableDevice Failed: {ex}");
            }
        }

        public void Cleanup()
        {
            Device.PropertyChanged -= OnDevicePropertyChanged;
        }

        private void OnDevicePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DeviceViewModel.Volume))
            {
                RaisePropertyChanged(nameof(Volume));
            }
            else if (e.PropertyName == nameof(DeviceViewModel.DisplayName))
            {
                RaisePropertyChanged(nameof(DisplayName));
            }
        }

        // The owning BurxatMixerViewModel decides, across every device at once, which channel
        // each app currently belongs to - see its RefreshAllApps - so a stale entry never lingers
        // here just because the underlying audio session hasn't caught up yet.
        public void SetApps(IEnumerable<IAppItemViewModel> apps)
        {
            Apps.Clear();
            foreach (var app in apps.OrderBy(a => a.DisplayName, StringComparer.CurrentCultureIgnoreCase))
            {
                Apps.Add(new MixerAppViewModel(_mainViewModel, app));
            }
        }
    }
}
