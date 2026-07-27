# Developing Burxat's Mixer

This document explains what this fork adds on top of stock [EarTrumpet](https://github.com/File-New-Project/EarTrumpet), code-wise, so anyone who wants to branch this out and keep building on it has a map to start from. It assumes you're already comfortable building the base project — see [COMPILING.md](./COMPILING.md) for that.

Everything described here lives under `EarTrumpet/UI/Views/Burxat*`, `EarTrumpet/UI/ViewModels/{BurxatMixer,Mixer,SelectableOption,DisabledDevice}*`, `EarTrumpet/Interop/Helpers/StartupHelper.cs`, and the separate `BurxatMixerLauncher` project. A handful of original EarTrumpet files were touched too, listed below with why.

## File map

**New, Burxat-only:**
- `UI/Views/BurxatMixerWindow.xaml(.cs)` — the main mixer window.
- `UI/Views/BurxatMixerSettingsWindow.xaml(.cs)` — the settings window opened from the gear icon.
- `UI/Views/ThemePalette.cs` — the six color palettes and the code that applies one live.
- `UI/ViewModels/BurxatMixerWindowViewModel.cs` — holds the Output and Input mixers shown in one window.
- `UI/ViewModels/BurxatMixerViewModel.cs` — one device-kind's worth of channels + master fader logic.
- `UI/ViewModels/MixerChannelViewModel.cs` — one device column.
- `UI/ViewModels/MixerAppViewModel.cs` — one app chip within a device column.
- `UI/ViewModels/MixerDeviceKind.cs` — `Output`/`Input` enum threaded through the three view models above so labels ("Output Device 1" vs "Input Device 1", etc.) are generated, not duplicated per file.
- `UI/ViewModels/BurxatMixerSettingsViewModel.cs` — backs the settings window.
- `UI/ViewModels/SelectableOptionViewModel.cs` — a label + `IsSelected` + command, used for every chip-button group (scale, theme, start-with-Windows, stay-on-top).
- `UI/ViewModels/DisabledDeviceViewModel.cs` — one row in a disabled-devices list.
- `Interop/Helpers/StartupHelper.cs` — the HKCU Run-key toggle behind "Start with Windows".
- `BurxatMixerLauncher/` — a separate, minimal project that builds `BurxatMixer.exe`.

**Modified original files, and why:**
- `App.xaml.cs` — tray menu entry, window creation, the `--burxat-mixer` launcher protocol (see below).
- `AppSettings.cs` — every `BurxatMixer*` setting (scale, theme, stay-on-top, start-with-Windows, window placement) lives here, following the exact same `ISettingsBag`-backed property pattern as the original settings above it.
- `DataModel/WindowsAudio/Internal/AudioDeviceManager.cs` — `Default` setter now sets `eConsole`, `eMultimedia`, *and* `eCommunications` roles (was `eMultimedia` only), matching what Windows' own Sound settings does. Needed so double-click/drag default-device actions actually reroute audio for apps using other roles.
- `UI/ViewModels/DeviceCollectionViewModel.cs` — one-line fix, see "Known gotchas" below.

## Architecture

### One window, two mixers, one template

`BurxatMixerWindowViewModel` holds `Output` and `Input`, each a `BurxatMixerViewModel` wrapping EarTrumpet's own `DeviceCollectionViewModel` — one built from `WindowsAudioFactory.Create(AudioDeviceKind.Playback)`, the other from `AudioDeviceKind.Recording` (see `App.xaml.cs`, `ContinueStartup()`).

The window shows both, stacked, via one `DataTemplate` (`MixerZoneTemplate` in `BurxatMixerWindow.xaml`) used twice — `<ContentControl Content="{Binding Output}" ContentTemplate="{StaticResource MixerZoneTemplate}" />` and the same for `Input`. Add a third device kind and you'd add a third `ContentControl` the same way; you would not need a new template.

Every label that differs between the two ("Output Device 1" vs "Input Device 1", "Set as Default Output Device" vs "...Input...", the drag-and-drop hint text) is a computed property on `MixerChannelViewModel`/`MixerAppViewModel`/`BurxatMixerViewModel` driven by a `MixerDeviceKind Kind` field passed down from `BurxatMixerWindowViewModel` at construction. Nothing in the XAML hardcodes "Output" or "Input" as a literal string.

### Input-device support was mostly free

EarTrumpet's audio layer was already flow-agnostic before this fork touched it: `AudioDeviceManager` picks `EDataFlow.eRender` vs `eCapture` from a single `Flow` property based on its `AudioDeviceKind`, `AudioPolicyConfigService` takes a flow in its constructor, and `DeviceCollectionViewModel`/`AppItemViewModel`/the Win32 policy-config wrappers never assume render specifically (`PersistedOutputDevice` is named after its original only use, but works identically for capture sessions). Adding full input-device mixing was therefore a case of instantiating a second `DeviceCollectionViewModel` and reusing the entire existing view-model/view stack — not new plumbing. If you're extending this further, check whether the piece you need already takes a flow/kind parameter before assuming you need to duplicate anything.

### Theming

Each Burxat window defines its own default palette inline as `<SolidColorBrush x:Key="BurxatWindowBackground" .../>` etc. in `Window.Resources`. `ThemePalette.ApplyTo(Resources, themeName)` overwrites those same keys live when the user picks a theme — see `ApplyTheme` in each window's code-behind, called both at construction and on `AppSettings.BurxatMixerThemeChanged`.

This is deliberately a separate system from EarTrumpet's own `Theme:` attached-property system (`EarTrumpet.UI.Themes`), which follows the OS light/dark setting — Burxat's theme is picked manually and shouldn't fight with that.

To add a theme or a new themed resource: add a property to `ThemePalette`, a value per `case` in `ThemePalette.For()`, a line in `ThemePalette.ApplyTo()`, and a matching default `<SolidColorBrush>`/`<FontFamily>` in every Burxat window's `Window.Resources` (so the window isn't blank before the theme is applied on construction).

### Settings and persistence

Every Burxat setting is a property on `AppSettings` backed by the app's existing `ISettingsBag` (registry-backed for unpackaged builds). Settings that other windows need to react to live-follow (theme, scale, stay-on-top) also raise a `Changed` event that windows subscribe to in their constructor and unsubscribe on `Closed`.

Window position and size persistence (`BurxatMixerWindowPlacement`) follows the exact pattern the original `FullWindow`/`SettingsWindow` already use: a `WINDOWPLACEMENT?` property, restored via `User32.SetWindowPlacement` on `SourceInitialized`, saved via `User32.GetWindowPlacement` on `Closing`.

"Start with Windows" is a plain `HKCU\...\CurrentVersion\Run` key (`StartupHelper.cs`), not EarTrumpet's Store `StartupTask` API — this build has no package identity to register one under.

### The launcher and the activation protocol

`BurxatMixerLauncher` is a separate, tiny project (own `.csproj`, added to `EarTrumpet.vs15.sln`) that builds `BurxatMixer.exe` into the same output folder as `EarTrumpet.exe`. All it does is find `EarTrumpet.exe` next to itself and start it with a `--burxat-mixer` argument.

`App.xaml.cs` handles that flag on both sides of the single-instance mutex:
- If this process wins the mutex (nothing else running), it proceeds through normal startup and opens the mixer once `CompleteStartup()` finishes.
- If another instance already holds the mutex, this process instead connects to a named pipe (`BurxatMixerActivationPipeName`) that the running instance is listening on, then exits. The running instance treats any connection on that pipe as "open the mixer" and raises the window — see `StartBurxatMixerActivationServer`/`RequestBurxatMixerFromRunningInstance`.

This means `BurxatMixer.exe` behaves the same whether EarTrumpet is already running or not, without needing to check that itself.

## Known gotchas

- **EarTrumpet's own `App.xaml` defines app-wide *implicit* styles** (no `x:Key`) for `ComboBox`, `CheckBox`, and `Button`. A plain `<ComboBox>` dropped into a Burxat window silently becomes the Settings page's search-as-you-type box instead of a normal dropdown — this is why every choice group in this fork (scale, theme, etc.) is a row of plain buttons bound to `SelectableOptionViewModel`, each with an explicit `Style="{StaticResource ...}"`. **Always set `Style` explicitly on any control you add to a Burxat window** — never rely on the platform default, since the platform default here isn't neutral.
- **The `Window` element's own `Background` must stay whatever `DialogWindowStyle` sets** (`Transparent`) — `AllowsTransparency="True"` plus the custom `WindowChrome` need that for correct hit-testing. Painting the theme background directly on `<Window Background="...">` makes the *entire window click-through*. Paint it on a wrapping `<Border>` instead (see the root `<Border>` in `BurxatMixerWindow.xaml`).
- **`DeviceCollectionViewModel.OnDefaultDevicePropertyChanged` calls `TrayPropertyChanged?.Invoke()`** (fixed in this fork — it was a bare `.Invoke()` in the original, which threw `NullReferenceException` the moment a `DeviceCollectionViewModel` existed with no subscriber, which is exactly what the new Recording-kind collection is). If you introduce another `DeviceCollectionViewModel` instance anywhere, don't assume every event on it has a guaranteed subscriber the way the original single instance did.
- **Screenshots for testing**: Windows UI Automation gives false positives for some of these custom controls (e.g. it reported a `ComboBox` as "expanded" with nothing visibly open). A real screenshot is more trustworthy than automation state when something looks broken.

## Adding a new toggle setting (worked example)

To add something like "Stay on top" end-to-end:
1. `AppSettings.cs` — a `bool` property (+ a `Changed` event if other windows need to react live).
2. `BurxatMixerSettingsViewModel.cs` — an `ObservableCollection<SelectableOptionViewModel>` for the Off/On choices, populated in the constructor, kept in sync in `UpdateSelection()`.
3. `BurxatMixerSettingsWindow.xaml` — a label + `ItemsControl` bound to that collection using the existing `ChoiceChipTemplate`.
4. Wherever the setting needs to take effect (a window's code-behind, usually) — subscribe to the `Changed` event in the constructor, apply it once at construction, unsubscribe on `Closed`.
