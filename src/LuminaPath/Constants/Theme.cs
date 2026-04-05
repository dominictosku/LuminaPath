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
                    Primary = "#2d4275",
                    Black = "#0A0E19",
                    Success = "#64A70B",
                    Secondary = "#ff4081ff",
                    AppbarBackground = "rgba(255,255,255,0.8)",
                    AppbarText = "#424242",
                    BackgroundGray = "#F9FAFC",
                    TextPrimary = "rgba(66,66,66,1)",
                    TextSecondary = "#99a0b0",
                    Dark = "#110E2D",
                    DarkLighten = "#1A1643",
                    GrayDefault = "#4B5563",
                    GrayLight = "#9CA3AF",
                    GrayLighter = "#adbdccff"
                },

                PaletteDark = new PaletteDark
                {
                    Primary = "#7e6fff",
                    Dark = "#343a40",
                    PrimaryContrastText = "#c3cbe4",
                    Info = "#47bce8",
                    Error = "#f56e50",
                    Success = "#2cb57e",
                    Warning = "#f5bd58",
                    InfoContrastText = "#f6f6f6",
                    Black = "#27272f",
                    Background = "#0e1824",
                    BackgroundGray = "#27272f",
                    Surface = "#121e2d",
                    DrawerBackground = "#121e2d",
                    DrawerText = "#8fa6bf",
                    DrawerIcon = "rgba(255,255,255, 0.50)",
                    AppbarBackground = "rgba(14,24,36, 0.80)",
                    AppbarText = "rgba(255,255,255, 0.70)",
                    TextPrimary = "rgba(255,255,255,0.6980392156862745)",
                    TextSecondary = "rgba(255,255,255,0.4980392156862745)",
                    ActionDefault = "rgba(195,203,228,.80)",
                    ActionDisabled = "rgba(255,255,255, 0.26)",
                    ActionDisabledBackground = "rgba(255,255,255, 0.12)",
                    DarkDarken = "rgba(21,27,34,0.7)",
                    Divider = "#192a3f",
                    DividerLight = "rgba(255,255,255, 0.06)",
                    TableLines = "#192a3f",
                    LinesDefault = "rgba(255,255,255, 0.12)",
                    LinesInputs = "rgba(255,255,255, 0.3)",
                    TextDisabled = "rgba(255,255,255, 0.2)"
                },

                LayoutProperties = new LayoutProperties
                {
                    AppbarHeight = "80px",
                    DefaultBorderRadius = "6px"
                },

                Typography = new Typography
                {
                    Default = new DefaultTypography
                    {
                        FontSize = ".8125rem",
                        FontWeight = "400",
                        LineHeight = "1.43",
                        LetterSpacing = "normal",
                        FontFamily = new[] { "Public Sans", "Roboto", "Arial", "sans-serif" }
                    },

                    H1 = new H1Typography { FontSize = "4rem", FontWeight = "700", LineHeight = "1.167", LetterSpacing = "-.01562em" },
                    H2 = new H2Typography { FontSize = "3.75rem", FontWeight = "300", LineHeight = "1.2", LetterSpacing = "-.00833em" },
                    H3 = new H3Typography { FontSize = "3rem", FontWeight = "600", LineHeight = "1.167" },
                    H4 = new H4Typography { FontSize = "1.8rem", FontWeight = "400", LineHeight = "1.235", LetterSpacing = ".00735em" },
                    H5 = new H5Typography { FontSize = "1.5rem", FontWeight = "400", LineHeight = "1.334" },
                    H6 = new H6Typography { FontSize = "1.125rem", FontWeight = "600", LineHeight = "1.6", LetterSpacing = ".0075em" },

                    Button = new ButtonTypography
                    {
                        FontSize = ".8125rem",
                        FontWeight = "500",
                        LineHeight = "1.75",
                        LetterSpacing = ".02857em",
                        TextTransform = "uppercase"
                    },

                    Subtitle1 = new Subtitle1Typography { FontSize = "1rem" },
                    Subtitle2 = new Subtitle2Typography { FontSize = ".875rem" },
                    Body1 = new Body1Typography { FontSize = "0.875rem" },
                    Body2 = new Body2Typography { FontSize = ".8125rem" },
                    Caption = new CaptionTypography { FontSize = ".75rem" },
                    Overline = new OverlineTypography { FontSize = ".75rem" }
                }
            };

            return theme;
        }
    }
}