using EarTrumpet.Extensions;
using EarTrumpet.Interop;
using EarTrumpet.Interop.MMDeviceAPI;
using EarTrumpet.UI.Helpers;
using EarTrumpet.UI.Views;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

namespace EarTrumpet.UI.ViewModels
{
    public class BurxatMixerSettingsViewModel : BindableBase
    {
        private static readonly double[] ScaleValues = { 1.0, 1.25, 1.5, 1.75, 2.0 };

        // Plain buttons rather than a ComboBox: EarTrumpet's own App.xaml defines an app-wide
        // implicit style for every ComboBox (its Settings page's search-as-you-type box), so a
        // bare <ComboBox/> here would silently inherit that behavior instead of acting like a
        // normal dropdown.
        public ObservableCollection<SelectableOptionViewModel> ScaleChoices { get; } = new ObservableCollection<SelectableOptionViewModel>();
        public ObservableCollection<SelectableOptionViewModel> ThemeChoices { get; } = new ObservableCollection<SelectableOptionViewModel>();

        public ObservableCollection<DisabledDeviceViewModel> DisabledDevices { get; } = new ObservableCollection<DisabledDeviceViewModel>();
        public ICommand RefreshDisabledDevices { get; }

        public string DeveloperCredit => "Interface by Burxat (info@burxat.dev)";
        public string OriginalCredit => "EarTrumpet by File-New-Project - github.com/File-New-Project/EarTrumpet";
        public string AssistantCredit => "Built with Claude (Anthropic)";

        public BurxatMixerSettingsViewModel()
        {
            foreach (var scale in ScaleValues)
            {
                var label = FormatScale(scale);
                ScaleChoices.Add(new SelectableOptionViewModel(label, () => App.Settings.BurxatMixerScale = scale));
            }
            foreach (var theme in ThemePalette.Names)
            {
                ThemeChoices.Add(new SelectableOptionViewModel(theme, () => App.Settings.BurxatMixerTheme = theme));
            }
            UpdateSelection();
            App.Settings.BurxatMixerScaleChanged += OnScaleChanged;
            App.Settings.BurxatMixerThemeChanged += OnThemeChanged;

            RefreshDisabledDevices = new RelayCommand(LoadDisabledDevices);
            LoadDisabledDevices();
        }

        public void Cleanup()
        {
            App.Settings.BurxatMixerScaleChanged -= OnScaleChanged;
            App.Settings.BurxatMixerThemeChanged -= OnThemeChanged;
        }

        private void OnScaleChanged(double _) => UpdateSelection();
        private void OnThemeChanged(string _) => UpdateSelection();

        private void UpdateSelection()
        {
            var currentScaleLabel = FormatScale(App.Settings.BurxatMixerScale);
            foreach (var choice in ScaleChoices)
            {
                choice.IsSelected = choice.Label == currentScaleLabel;
            }
            foreach (var choice in ThemeChoices)
            {
                choice.IsSelected = choice.Label == App.Settings.BurxatMixerTheme;
            }
        }

        private static string FormatScale(double scale) => $"{scale * 100:0}%";

        private void LoadDisabledDevices()
        {
            DisabledDevices.Clear();
            try
            {
                var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
                var devices = enumerator.EnumAudioEndpoints(EDataFlow.eRender, DeviceState.DISABLED);
                var count = devices.GetCount();
                for (uint i = 0; i < count; i++)
                {
                    var device = devices.Item(i);
                    var id = device.GetId();
                    var name = device.OpenPropertyStore(STGM.STGM_READ).GetValue<string>(PropertyKeys.PKEY_Device_FriendlyName);
                    DisabledDevices.Add(new DisabledDeviceViewModel(id, name, EnableDevice));
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"BurxatMixerSettingsViewModel LoadDisabledDevices Failed: {ex}");
            }
        }

        private void EnableDevice(string id)
        {
            try
            {
                new AutoPolicyConfigClientWin7().SetEndpointVisibility(id, true);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"BurxatMixerSettingsViewModel EnableDevice Failed: {ex}");
            }

            LoadDisabledDevices();
        }
    }
}
