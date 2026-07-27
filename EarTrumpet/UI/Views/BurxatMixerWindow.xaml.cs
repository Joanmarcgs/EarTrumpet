using EarTrumpet.Interop;
using EarTrumpet.UI.ViewModels;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace EarTrumpet.UI.Views
{
    public partial class BurxatMixerWindow : Window
    {
        private const double BaseWidth = 700;
        private const double BaseHeight = 900;
        private const string DraggedAppFormat = "EarTrumpet.BurxatMixer.App";

        private Point _dragStartPoint;
        private IAppItemViewModel _dragCandidate;
        private BurxatMixerSettingsWindow _settingsWindow;

        public BurxatMixerWindow()
        {
            InitializeComponent();
            Closed += (_, __) =>
            {
                (DataContext as BurxatMixerWindowViewModel)?.Cleanup();
                App.Settings.BurxatMixerScaleChanged -= ApplyScale;
                App.Settings.BurxatMixerThemeChanged -= ApplyTheme;
                App.Settings.BurxatMixerStayOnTopChanged -= ApplyStayOnTop;
            };
            SourceInitialized += OnSourceInitialized;
            SourceInitialized += (sender, __) =>
            {
                if (App.Settings.BurxatMixerWindowPlacement != null)
                {
                    User32.SetWindowPlacement(new WindowInteropHelper((Window)sender).Handle, App.Settings.BurxatMixerWindowPlacement.Value);
                }
            };
            Closing += (sender, __) =>
            {
                if (User32.GetWindowPlacement(new WindowInteropHelper((Window)sender).Handle, out var placement))
                {
                    App.Settings.BurxatMixerWindowPlacement = placement;
                }
            };

            App.Settings.BurxatMixerScaleChanged += ApplyScale;
            App.Settings.BurxatMixerThemeChanged += ApplyTheme;
            App.Settings.BurxatMixerStayOnTopChanged += ApplyStayOnTop;
            ApplyScale(App.Settings.BurxatMixerScale);
            ApplyTheme(App.Settings.BurxatMixerTheme);
            ApplyStayOnTop(App.Settings.BurxatMixerStayOnTop);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsWindow == null)
            {
                _settingsWindow = new BurxatMixerSettingsWindow { Owner = this };
                _settingsWindow.Closed += (_, __) => _settingsWindow = null;
                _settingsWindow.Show();
            }
            else
            {
                _settingsWindow.Activate();
            }
        }

        private void ApplyScale(double scale)
        {
            RootContent.LayoutTransform = scale == 1.0 ? null : new ScaleTransform(scale, scale);
            Width = BaseWidth * scale;
            Height = BaseHeight * scale;
        }

        private void ApplyTheme(string theme) => ThemePalette.ApplyTo(Resources, theme);

        private void ApplyStayOnTop(bool stayOnTop) => Topmost = stayOnTop;

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            ((HwndSource)PresentationSource.FromVisual(this)).AddHook(WindowProc);
        }

        // WindowStyle=None + AllowsTransparency=True windows otherwise maximize to the monitor's
        // full physical bounds (covering the taskbar and spilling a few pixels onto adjacent
        // monitors) instead of its work area, so clamp it here.
        private static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == User32.WM_GETMINMAXINFO)
            {
                var mmi = (User32.MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(User32.MINMAXINFO));

                var monitor = User32.MonitorFromWindow(hwnd, User32.MONITOR_DEFAULTTONEAREST);
                if (monitor != IntPtr.Zero)
                {
                    var monitorInfo = new User32.MONITORINFO { cbSize = Marshal.SizeOf(typeof(User32.MONITORINFO)) };
                    User32.GetMonitorInfo(monitor, ref monitorInfo);
                    var workArea = monitorInfo.rcWork;
                    var monitorArea = monitorInfo.rcMonitor;

                    mmi.ptMaxPosition.x = workArea.Left - monitorArea.Left;
                    mmi.ptMaxPosition.y = workArea.Top - monitorArea.Top;
                    mmi.ptMaxSize.x = workArea.Right - workArea.Left;
                    mmi.ptMaxSize.y = workArea.Bottom - workArea.Top;
                }

                Marshal.StructureToPtr(mmi, lParam, true);
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void ChannelHeader_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && ((FrameworkElement)sender).DataContext is MixerChannelViewModel channel)
            {
                channel.MakeDefault.Execute(null);
            }
        }

        private void AppChip_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Don't start a drag when the press landed on the app's own fader.
            if (IsWithinSlider(e.OriginalSource as DependencyObject))
            {
                return;
            }

            _dragStartPoint = e.GetPosition(null);
            _dragCandidate = (((FrameworkElement)sender).DataContext as MixerAppViewModel)?.App;
        }

        private static bool IsWithinSlider(DependencyObject element)
        {
            while (element != null)
            {
                if (element is Slider)
                {
                    return true;
                }
                element = VisualTreeHelper.GetParent(element);
            }
            return false;
        }

        private void AppChip_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragCandidate == null || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            var position = e.GetPosition(null);
            if (Math.Abs(position.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(position.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            var app = _dragCandidate;
            _dragCandidate = null;

            if (app.IsMovable)
            {
                DragDrop.DoDragDrop((DependencyObject)sender, new DataObject(DraggedAppFormat, app), DragDropEffects.Move);
            }
        }

        private void Channel_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DraggedAppFormat) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private void Channel_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DraggedAppFormat) &&
                e.Data.GetData(DraggedAppFormat) is IAppItemViewModel app &&
                ((FrameworkElement)sender).DataContext is MixerChannelViewModel channel &&
                DataContext is BurxatMixerWindowViewModel windowViewModel)
            {
                var mixer = channel.Kind == MixerDeviceKind.Output ? windowViewModel.Output : windowViewModel.Input;
                mixer.MoveAppToChannel(channel, app);
            }
        }

        private void MasterFader_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DraggedAppFormat) &&
                e.Data.GetData(DraggedAppFormat) is IAppItemViewModel app &&
                ((FrameworkElement)sender).DataContext is BurxatMixerViewModel viewModel)
            {
                viewModel.MoveAppToDefault(app);
            }
        }
    }
}
