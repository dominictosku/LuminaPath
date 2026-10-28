using MudBlazor;

namespace LuminaPath.Constants
{
    public class Theme
    {
        public static MudTheme ApplicationTheme()
        {
            var theme = new MudTheme
            {
                PaletteLight = new PaletteLight
                {
                    Primary = "#1f6f50",
                    Black = "#0c1011",
                    Success = "#2cb57e",
                    Secondary = "#16583f",
                    AppbarBackground = "rgba(255,255,255,0.8)",
                    AppbarText = "#13191a",
                    BackgroundGray = "#F9FAFC",
                    TextPrimary = "#13191a",
                    TextSecondary = "#5c6666",
                    Dark = "#0c1011",
                    DarkLighten = "#13191a",
                    GrayDefault = "#4B5563",
                    GrayLight = "#9CA3AF",
                    GrayLighter = "#adbdccff"
                },

                PaletteDark = new PaletteDark
                {
                    Primary = "#b2f5cf",
                    PrimaryContrastText = "#082419",
                    Secondary = "#5fe0a3",
                    SecondaryContrastText = "#082419",
                    Dark = "#13191a",
                    Info = "#7dc4ff",
                    InfoContrastText = "#081b29",
                    Error = "#ff9c9c",
                    Success = "#8bf2b5",
                    Warning = "#f5d491",
                    Black = "#0c1011",
                    Background = "#0c1011",
                    BackgroundGray = "#101617",
                    Surface = "#13191a",
                    DrawerBackground = "#0f1415",
                    DrawerText = "#f0f4f1",
                    DrawerIcon = "#9aa7a4",
                    AppbarBackground = "rgba(12, 16, 17, 0.88)",
                    AppbarText = "#f0f4f1",
                    TextPrimary = "#f0f4f1",
                    TextSecondary = "#858e8e",
                    ActionDefault = "#b7c4c1",
                    ActionDisabled = "rgba(240, 244, 241, 0.28)",
                    ActionDisabledBackground = "rgba(240, 244, 241, 0.08)",
                    DarkDarken = "rgba(12, 16, 17, 0.85)",
                    Divider = "#252c2d",
                    DividerLight = "rgba(255, 255, 255, 0.06)",
                    TableLines = "#22292a",
                    LinesDefault = "#252c2d",
                    LinesInputs = "#2d3739",
                    TextDisabled = "rgba(240, 244, 241, 0.35)"
                },

                LayoutProperties = new LayoutProperties
                {
                    AppbarHeight = "64px",
                    DrawerWidthLeft = "222px",
                    DefaultBorderRadius = "8px"
                },

                Typography = new Typography
                {
                    Default = new DefaultTypography
                    {
                        FontSize = ".875rem",
                        FontWeight = "400",
                        LineHeight = "1.45",
                        LetterSpacing = "normal",
                        FontFamily = ["DM Sans", "system-ui", "-apple-system", "Segoe UI", "Roboto", "sans-serif"]
                    },

                    H1 = new H1Typography { FontFamily = ["Manrope", "DM Sans", "sans-serif"], FontSize = "3.1rem", FontWeight = "800", LineHeight = "1.03", LetterSpacing = "-.035em" },
                    H2 = new H2Typography { FontFamily = ["Manrope", "DM Sans", "sans-serif"], FontSize = "2.5rem", FontWeight = "800", LineHeight = "1.06", LetterSpacing = "-.03em" },
                    H3 = new H3Typography { FontFamily = ["Manrope", "DM Sans", "sans-serif"], FontSize = "2.15rem", FontWeight = "800", LineHeight = "1.08", LetterSpacing = "-.03em" },
                    H4 = new H4Typography { FontFamily = ["Manrope", "DM Sans", "sans-serif"], FontSize = "1.65rem", FontWeight = "800", LineHeight = "1.15", LetterSpacing = "-.025em" },
                    H5 = new H5Typography { FontFamily = ["Manrope", "DM Sans", "sans-serif"], FontSize = "1.3rem", FontWeight = "700", LineHeight = "1.22", LetterSpacing = "-.02em" },
                    H6 = new H6Typography { FontFamily = ["Manrope", "DM Sans", "sans-serif"], FontSize = "1.05rem", FontWeight = "700", LineHeight = "1.3", LetterSpacing = "-.01em" },

                    Button = new ButtonTypography
                    {
                        FontFamily = ["DM Sans", "sans-serif"],
                        FontSize = ".8125rem",
                        FontWeight = "700",
                        LineHeight = "1.5",
                        LetterSpacing = ".01em",
                        TextTransform = "none"
                    },

                    Subtitle1 = new Subtitle1Typography { FontSize = "1rem" },
                    Subtitle2 = new Subtitle2Typography { FontSize = ".875rem" },
                    Body1 = new Body1Typography { FontSize = "0.875rem" },
                    Body2 = new Body2Typography { FontSize = ".8125rem" },
                    Caption = new CaptionTypography { FontSize = ".75rem" },
                    Overline = new OverlineTypography { FontSize = ".68rem", FontWeight = "700", LetterSpacing = ".14em" }
                }
            };

            return theme;
        }
    }
}
