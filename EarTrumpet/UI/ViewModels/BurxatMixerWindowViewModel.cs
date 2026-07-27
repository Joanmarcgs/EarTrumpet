using EarTrumpet.UI.Helpers;

namespace EarTrumpet.UI.ViewModels
{
    // Holds both mixers - Output and Input are shown stacked in the same window, one above the
    // other, rather than as separate windows or tabs.
    public class BurxatMixerWindowViewModel : BindableBase
    {
        public BurxatMixerViewModel Output { get; }
        public BurxatMixerViewModel Input { get; }

        public BurxatMixerWindowViewModel(DeviceCollectionViewModel outputDevices, DeviceCollectionViewModel inputDevices)
        {
            Output = new BurxatMixerViewModel(outputDevices, MixerDeviceKind.Output);
            Input = new BurxatMixerViewModel(inputDevices, MixerDeviceKind.Input);
        }

        public void Cleanup()
        {
            Output.Cleanup();
            Input.Cleanup();
        }
    }
}
