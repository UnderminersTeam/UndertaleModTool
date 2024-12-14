# New Theme Library

I decided to upgrade the theme library so that, instead of having each theme
contain all of the control styles, there is instead just a global style file.

I may extract them into their own files at some point, e.g. ButtonStyles.xaml,
ListBoxStyles.xaml, etc, just so that it's easier to find stuff

## Adjustable styles

- You can switch between a thin and thick scroll bar style. In `Controls.xaml`, search
  for the text: "Switch between automatic thin and thick scroll bars here" and change the
  BasedOn part between `ScrollBarStyle_THIN` and `ScrollBarStyle_THICK`

# Files and file structures

## Controls.xaml

Contains all of the control styles. This is a big file, so it might take a while
for code analysis to load for Rider

## ControlColours.xaml

This is where I (mostly attempted to) keep control-specific brushes and stuff.
However, I still sometimes used the resource keys directly, which is fine because each theme
should contain the exact same resource key names, but their colours should change

I may attempt to make a "LightThemeControlColours" and "DarkThemeControlColours", because sometimes
there are colour differences between light and dark themes that just might not work out and will look weird

## Colour Dictionaries

These stores all of the colour keys that are accessed throughout the application.

- All colours are prefixed with "AColour". The 'A' at the start is just for quick searching.
- All brushes are prefixed with "ABrush", the 'A' used for the same reason as colours.

Foreground has a static, deeper and disabled colour.
Static for regular text colour
Deeper is just a slightly darker/less visible colour
Disabled is a much darker/less visible colour

Glyphs have static, disabled, mouse over/down, selected + inactive selected colours.
There's also a colourful glyph which has the same keys but with "Colourful" added

### Tones

Tones are the different colour phases for different controls. The lower tone number
means darker (in dark themes) and typically lighter in light themes

Accent tones follow the same rules but they use a colour instead of
moving towards black or white (for dark/light themes)

Tone 0, 1, 2 and 3 are useful for containers/panels (e.g. Grid or a Border that
contains a Grid, DockPanels, etc.)

The rest of the tones are used for standard controls (e.g. buttons). Most controls
use Tone 4 and 5, but some may use higher tones. You can obviously changes this
if you want and make a button use a higher tone in order to stand out more

> All Color keys have a corresponding SolidColourBrush key
> (e.g. "AColour.Glyph.Static" -> "ABrush.Glyph.Static"), which is useful
> if you want to animate a colour which I think requires Colour not Brush
> (that might be gradients though, can't remember)
