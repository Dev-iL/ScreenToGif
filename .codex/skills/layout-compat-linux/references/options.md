# Options layout parity

Use this reference for the Options shell or any of its settings pages.

## Source map

| Surface | Windows source | Linux target or evidence |
| --- | --- | --- |
| Options shell and navigation | `ScreenToGif/Windows/Options.xaml` | `ScreenToGif.Linux/OptionsWindow.axaml` |
| Page content | `ScreenToGif/Views/Settings/*Settings.xaml` | Named page controls in `OptionsWindow.axaml` |
| Rendered page references | Supplied Windows captures | `.codex/skills/options-<page>.png` |

The captures cover Application, Recorder, Editor, Tasks, Shortcuts, Language, Storage, Cloud, Extras, Donate, and About. Match each page against its own capture; shared shell alignment does not prove page-level parity.

The WPF shell declares `Width="800"`, `Height="610"`, `MinWidth="780"`, and `MinHeight="460"`. Screenshot pixel dimensions include capture and non-client differences, so use the WPF logical values to establish the shell and compare the client area. Preserve the left navigation, page content region, and bottom confirmation band as separate grid regions.

## Settings groups

WPF settings pages use compact section headers with a separator line. Avalonia's stock `Expander` header is materially taller, so use one Options-local `ControlTheme` for the repeated 24-unit header/content shape and verify both expanded content and collapse interaction in a real render. In the tested Avalonia version, layering a second custom `ToggleButton` theme inside that template disturbed measure/render behavior; add nested control themes only with rendered evidence.

Keep unavailable features in their Windows positions with disabled affordances and tooltips. The Donate page content exists for parity evidence, but its navigation item is deliberately disabled and must remain inaccessible.

## Initialization and navigation

`SelectedIndex="0"` on the navigation `ListBox` can raise `SelectionChanged` during `InitializeComponent`, before later named page fields have been assigned. Gate page switching until initialization and settings loading complete, or attach the handler afterward. A successful compile does not detect this failure; launch the window and visit every enabled page.

When headless interaction appears to miss a navigation item, follow the focus guidance in [verification](verification.md) before changing hit targets or selection logic.
