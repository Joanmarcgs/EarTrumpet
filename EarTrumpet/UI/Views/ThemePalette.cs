using System.Windows;
using System.Windows.Media;

namespace EarTrumpet.UI.Views
{
    // The six color palettes offered by Burxat's Mixer settings: Dark, Standard and Chocolate,
    // each with an 8-bit ("retro") variant. Deliberately independent of EarTrumpet's own
    // Light/Dark/HighContrast theme engine, since these are picked manually rather than following
    // the system.
    public class ThemePalette
    {
        public static readonly string[] Names = { "Dark", "Standard", "Chocolate", "Dark (Retro)", "Standard (Retro)", "Chocolate (Retro)" };

        public Brush WindowBackground { get; set; }
        public Brush Text { get; set; }
        public Brush SubtleText { get; set; }
        public Brush CardBackground { get; set; }
        public Brush ZoneBackground { get; set; }
        public Brush Track { get; set; }
        public Brush FaderThumb { get; set; }
        public Brush Border { get; set; }
        public FontFamily FontFamily { get; set; } = new FontFamily("Segoe UI");

        private static readonly FontFamily PixelFont = new FontFamily("Consolas");

        public static ThemePalette For(string name)
        {
            switch (name)
            {
                case "Standard":
                    return new ThemePalette
                    {
                        WindowBackground = Brush(0xFF, 0xF3, 0xF3, 0xF3),
                        Text = Brush(0xFF, 0x1A, 0x1A, 0x1A),
                        SubtleText = Brush(0xFF, 0x6E, 0x6E, 0x6E),
                        CardBackground = Brush(0xFF, 0xFF, 0xFF, 0xFF),
                        ZoneBackground = Brush(0xFF, 0xE0, 0xE0, 0xE0),
                        Track = Brush(0xFF, 0xC0, 0xC0, 0xC0),
                        FaderThumb = Brush(0xFF, 0x2A, 0x2A, 0x2A),
                        Border = Brush(0xFF, 0x8A, 0x8A, 0x8A),
                    };
                case "Chocolate":
                    return new ThemePalette
                    {
                        WindowBackground = Brush(0xFF, 0x2E, 0x1C, 0x12),
                        Text = Brush(0xFF, 0xF0, 0xDC, 0xC8),
                        SubtleText = Brush(0xFF, 0xB5, 0x8A, 0x67),
                        CardBackground = Brush(0xFF, 0x4A, 0x2F, 0x1E),
                        ZoneBackground = Brush(0xFF, 0x3E, 0x28, 0x18),
                        Track = Brush(0xFF, 0x6B, 0x45, 0x30),
                        FaderThumb = Brush(0xFF, 0xD2, 0xA6, 0x79),
                        Border = Brush(0xFF, 0x8C, 0x5A, 0x3C),
                        FontFamily = new FontFamily("Candara"),
                    };
                case "Dark (Retro)":
                    return new ThemePalette
                    {
                        WindowBackground = Brush(0xFF, 0x0D, 0x0D, 0x0D),
                        Text = Brush(0xFF, 0x33, 0xFF, 0x33),
                        SubtleText = Brush(0xFF, 0x1F, 0xA6, 0x1F),
                        CardBackground = Brush(0xFF, 0x1A, 0x1A, 0x1A),
                        ZoneBackground = Brush(0xFF, 0x14, 0x14, 0x14),
                        Track = Brush(0xFF, 0x22, 0x44, 0x22),
                        FaderThumb = Brush(0xFF, 0xFF, 0xCC, 0x00),
                        Border = Brush(0xFF, 0x33, 0xFF, 0x33),
                        FontFamily = PixelFont,
                    };
                case "Standard (Retro)":
                    return new ThemePalette
                    {
                        WindowBackground = Brush(0xFF, 0xE8, 0xE8, 0xD0),
                        Text = Brush(0xFF, 0x2B, 0x3A, 0x1A),
                        SubtleText = Brush(0xFF, 0x5B, 0x6B, 0x4A),
                        CardBackground = Brush(0xFF, 0xC8, 0xD0, 0xA8),
                        ZoneBackground = Brush(0xFF, 0xD8, 0xD8, 0xBC),
                        Track = Brush(0xFF, 0x8B, 0x9A, 0x6B),
                        FaderThumb = Brush(0xFF, 0x2B, 0x3A, 0x1A),
                        Border = Brush(0xFF, 0x4A, 0x5A, 0x3A),
                        FontFamily = PixelFont,
                    };
                case "Chocolate (Retro)":
                    return new ThemePalette
                    {
                        WindowBackground = Brush(0xFF, 0x1A, 0x0E, 0x08),
                        Text = Brush(0xFF, 0xFF, 0x8C, 0x42),
                        SubtleText = Brush(0xFF, 0xB5, 0x62, 0x2A),
                        CardBackground = Brush(0xFF, 0x2E, 0x1C, 0x12),
                        ZoneBackground = Brush(0xFF, 0x26, 0x15, 0x09),
                        Track = Brush(0xFF, 0x4A, 0x2F, 0x1E),
                        FaderThumb = Brush(0xFF, 0xFF, 0xC1, 0x45),
                        Border = Brush(0xFF, 0xFF, 0x8C, 0x42),
                        FontFamily = PixelFont,
                    };
                case "Dark":
                default:
                    return new ThemePalette
                    {
                        WindowBackground = Brush(0xFF, 0x1B, 0x1B, 0x1B),
                        Text = Brush(0xFF, 0xF2, 0xF2, 0xF2),
                        SubtleText = Brush(0xFF, 0xAF, 0xAF, 0xAF),
                        CardBackground = Brush(0xFF, 0x2A, 0x2A, 0x2A),
                        ZoneBackground = Brush(0xFF, 0x24, 0x24, 0x24),
                        Track = Brush(0xFF, 0x4A, 0x4A, 0x4A),
                        FaderThumb = Brush(0xFF, 0xE8, 0xE8, 0xE8),
                        Border = Brush(0xFF, 0x7A, 0x7A, 0x7A),
                    };
            }
        }

        private static SolidColorBrush Brush(byte a, byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
            brush.Freeze();
            return brush;
        }

        // Shared by every Burxat's Mixer window so picking a theme in Settings re-skins all of
        // them, not just the main window.
        public static void ApplyTo(ResourceDictionary resources, string themeName)
        {
            var palette = For(themeName);
            resources["BurxatWindowBackground"] = palette.WindowBackground;
            resources["BurxatText"] = palette.Text;
            resources["BurxatSubtleText"] = palette.SubtleText;
            resources["BurxatCardBackground"] = palette.CardBackground;
            resources["BurxatZoneBackground"] = palette.ZoneBackground;
            resources["BurxatTrack"] = palette.Track;
            resources["BurxatFaderThumb"] = palette.FaderThumb;
            resources["BurxatBorder"] = palette.Border;
            resources["BurxatFontFamily"] = palette.FontFamily;
        }
    }
}
