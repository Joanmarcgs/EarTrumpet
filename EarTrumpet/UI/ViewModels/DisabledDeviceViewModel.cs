using EarTrumpet.UI.Helpers;
using System;
using System.Windows.Input;

namespace EarTrumpet.UI.ViewModels
{
    // A playback device Windows currently has hidden ("disabled" in Sound settings),
    // shown here so it can be brought back without leaving the mixer.
    public class DisabledDeviceViewModel
    {
        public string Id { get; }
        public string DisplayName { get; }
        public ICommand Enable { get; }

        public DisabledDeviceViewModel(string id, string displayName, Action<string> onEnable)
        {
            Id = id;
            DisplayName = displayName;
            Enable = new RelayCommand(() => onEnable(id));
        }
    }
}
