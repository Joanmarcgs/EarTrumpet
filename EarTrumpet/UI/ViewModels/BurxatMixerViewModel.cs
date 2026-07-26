using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace EarTrumpet.UI.ViewModels
{
    public class BurxatMixerViewModel : BindableBase
    {
        public ObservableCollection<MixerChannelViewModel> Channels { get; } = new ObservableCollection<MixerChannelViewModel>();

        public int MasterVolume
        {
            get => _masterVolume;
            set
            {
                value = Math.Max(0, Math.Min(100, value));
                if (_masterVolume != value)
                {
                    _masterVolume = value;
                    RaisePropertyChanged(nameof(MasterVolume));
                    ApplyMasterVolume();
                }
            }
        }

        private readonly DeviceCollectionViewModel _mainViewModel;

        // The volume each channel would be at if the master fader were at 100%.
        // Moving the master fader scales every channel relative to this baseline;
        // moving a channel's own fader re-establishes its baseline in turn. This is
        // only ever touched by direct user input (MixerChannelViewModel.VolumeSetByUser)
        // or when a device first appears, never by the master fader itself - otherwise
        // fast or repeated master moves would drift the baseline instead of round-tripping.
        private readonly Dictionary<string, int> _baseVolumes = new Dictionary<string, int>();
        private int _masterVolume = 100;

        public BurxatMixerViewModel(DeviceCollectionViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            foreach (var device in _mainViewModel.AllDevices)
            {
                AddChannel(device);
            }
            _mainViewModel.AllDevices.CollectionChanged += OnDevicesChanged;
            _mainViewModel.DefaultChanged += OnDefaultChanged;
            ReassignOrdinals();
            RefreshAllApps();
        }

        public void Cleanup()
        {
            _mainViewModel.AllDevices.CollectionChanged -= OnDevicesChanged;
            _mainViewModel.DefaultChanged -= OnDefaultChanged;
            foreach (var channel in Channels)
            {
                channel.VolumeSetByUser -= OnChannelVolumeSetByUser;
                channel.Device.Apps.CollectionChanged -= OnAnyDeviceAppsChanged;
                channel.Cleanup();
            }
        }

        private void OnDevicesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (DeviceViewModel device in e.OldItems)
                {
                    var channel = Channels.FirstOrDefault(c => c.Device.Id == device.Id);
                    if (channel != null)
                    {
                        channel.VolumeSetByUser -= OnChannelVolumeSetByUser;
                        channel.Device.Apps.CollectionChanged -= OnAnyDeviceAppsChanged;
                        channel.Cleanup();
                        Channels.Remove(channel);
                        _baseVolumes.Remove(device.Id);
                    }
                }
            }
            if (e.NewItems != null)
            {
                foreach (DeviceViewModel device in e.NewItems)
                {
                    AddChannel(device);
                }
            }
            ReassignOrdinals();
            RefreshAllApps();
        }

        private void ReassignOrdinals()
        {
            for (var i = 0; i < Channels.Count; i++)
            {
                Channels[i].Ordinal = i + 1;
            }
        }

        private void OnDefaultChanged(object sender, DeviceViewModel newDefault)
        {
            foreach (var channel in Channels)
            {
                channel.IsDefault = newDefault != null && channel.Device.Id == newDefault.Id;
            }
            RefreshAllApps();
        }

        private void OnAnyDeviceAppsChanged(object sender, NotifyCollectionChangedEventArgs e) => RefreshAllApps();

        private void AddChannel(DeviceViewModel device)
        {
            RecordBaseVolume(device.Id, device.Volume);

            var channel = new MixerChannelViewModel(_mainViewModel, device)
            {
                IsDefault = _mainViewModel.Default != null && _mainViewModel.Default.Id == device.Id,
            };
            channel.VolumeSetByUser += OnChannelVolumeSetByUser;
            device.Apps.CollectionChanged += OnAnyDeviceAppsChanged;
            Channels.Add(channel);
        }

        private void OnChannelVolumeSetByUser(object sender, int newVolume) => RecordBaseVolume(((MixerChannelViewModel)sender).Device.Id, newVolume);

        private void RecordBaseVolume(string deviceId, int currentVolume)
        {
            var scale = _masterVolume / 100.0;
            _baseVolumes[deviceId] = scale > 0 ? (int)Math.Round(currentVolume / scale) : currentVolume;
        }

        private void ApplyMasterVolume()
        {
            var scale = _masterVolume / 100.0;
            foreach (var channel in Channels)
            {
                if (_baseVolumes.TryGetValue(channel.Device.Id, out var baseVolume))
                {
                    channel.Device.Volume = Math.Max(0, Math.Min(100, (int)Math.Round(baseVolume * scale)));
                }
            }
        }

        public void MoveAppToChannel(MixerChannelViewModel channel, IAppItemViewModel app)
        {
            _mainViewModel.MoveAppToDevice(app, channel.Device);
            RefreshAllApps();
        }

        public void MoveAppToDefault(IAppItemViewModel app)
        {
            _mainViewModel.MoveAppToDevice(app, null);
            RefreshAllApps();
        }

        // Every device's raw session list can lag behind what the user just asked for (an
        // explicit move, or a default-device switch), since Windows doesn't always mark an app's
        // old session as expired/hidden right away. So instead of trusting each device's own app
        // list, every app is resolved to exactly one channel here - by its pinned device if it has
        // one and that device still exists, otherwise the current default - and each channel's
        // list is fully replaced from that. This runs after every move and every relevant change,
        // so the split is always self-consistent even if the underlying session state hasn't
        // settled yet.
        private void RefreshAllApps()
        {
            // Keyed by the app's real-world identity (its exe/package), not its session or
            // group id - those can regenerate as EarTrumpet's own session grouping settles
            // (most visibly right at startup), which briefly produced two entries for what is
            // really the same running app.
            var allApps = new Dictionary<string, IAppItemViewModel>();
            foreach (var device in _mainViewModel.AllDevices)
            {
                foreach (var app in device.Apps)
                {
                    if (app.IsMovable)
                    {
                        allApps[GetAppIdentity(app)] = app;
                    }
                }
            }

            var defaultDeviceId = _mainViewModel.Default?.Id;
            foreach (var channel in Channels)
            {
                var appsForChannel = allApps.Values.Where(app => ResolveDeviceId(app, defaultDeviceId) == channel.Device.Id);
                channel.SetApps(appsForChannel);
            }
        }

        private static string GetAppIdentity(IAppItemViewModel app)
        {
            if (!string.IsNullOrWhiteSpace(app.AppId))
            {
                return app.AppId;
            }
            if (!string.IsNullOrWhiteSpace(app.ExeName))
            {
                return app.ExeName;
            }
            return app.Id;
        }

        private string ResolveDeviceId(IAppItemViewModel app, string defaultDeviceId)
        {
            var persisted = app.PersistedOutputDevice;
            if (!string.IsNullOrWhiteSpace(persisted) && Channels.Any(c => c.Device.Id == persisted))
            {
                return persisted;
            }
            return defaultDeviceId;
        }
    }
}
