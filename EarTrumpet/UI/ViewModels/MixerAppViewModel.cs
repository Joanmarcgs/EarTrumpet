using EarTrumpet.UI.Helpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EarTrumpet.UI.ViewModels
{
    // Represents one app within a Burxat's Mixer channel strip. It can be dragged onto
    // another channel to move it there, or right-clicked for the same choice as a menu.
    public class MixerAppViewModel
    {
        public IAppItemViewModel App { get; }

        // An app with no persisted device of its own just follows whatever the current
        // default output device is, and moves automatically when the default changes.
        public string DisplayName => string.IsNullOrWhiteSpace(App.PersistedOutputDevice) ?
            $"{App.DisplayName} (by default)" : App.DisplayName;
        public bool IsMovable => App.IsMovable;
        public ObservableCollection<ContextMenuItem> MoveMenu { get; }

        public MixerAppViewModel(DeviceCollectionViewModel mainViewModel, IAppItemViewModel app, MixerDeviceKind kind)
        {
            App = app;

            if (App.IsMovable)
            {
                var persistedDeviceId = app.PersistedOutputDevice;
                var kindLabel = kind == MixerDeviceKind.Output ? "Output" : "Input";

                var items = new List<ContextMenuItem>
                {
                    new ContextMenuItem
                    {
                        DisplayName = $"Default {kindLabel} Device",
                        IsChecked = string.IsNullOrWhiteSpace(persistedDeviceId),
                        Command = new RelayCommand(() => mainViewModel.MoveAppToDevice(app, null)),
                    },
                    new ContextMenuSeparator(),
                };
                items.AddRange(mainViewModel.AllDevices.Select(dev => new ContextMenuItem
                {
                    DisplayName = dev.DisplayName,
                    IsChecked = dev.Id == persistedDeviceId,
                    Command = new RelayCommand(() => mainViewModel.MoveAppToDevice(app, dev)),
                }));

                MoveMenu = new ObservableCollection<ContextMenuItem>(items);
            }
        }
    }
}
