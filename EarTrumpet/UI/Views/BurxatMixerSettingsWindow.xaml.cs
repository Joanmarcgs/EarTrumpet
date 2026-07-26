using EarTrumpet.UI.ViewModels;
using System.Windows;
using System.Windows.Media;

namespace EarTrumpet.UI.Views
{
    public partial class BurxatMixerSettingsWindow : Window
    {
        private const double BaseWidth = 360;

        public BurxatMixerSettingsWindow()
        {
            InitializeComponent();
            DataContext = new BurxatMixerSettingsViewModel();
            Closed += (_, __) =>
            {
                (DataContext as BurxatMixerSettingsViewModel)?.Cleanup();
                App.Settings.BurxatMixerThemeChanged -= ApplyTheme;
                App.Settings.BurxatMixerScaleChanged -= ApplyScale;
            };

            App.Settings.BurxatMixerThemeChanged += ApplyTheme;
            App.Settings.BurxatMixerScaleChanged += ApplyScale;
            ApplyTheme(App.Settings.BurxatMixerTheme);
            ApplyScale(App.Settings.BurxatMixerScale);
        }

        private void ApplyTheme(string theme) => ThemePalette.ApplyTo(Resources, theme);

        private void ApplyScale(double scale)
        {
            RootContent.LayoutTransform = scale == 1.0 ? null : new ScaleTransform(scale, scale);
            Width = BaseWidth * scale;
        }
    }
}
