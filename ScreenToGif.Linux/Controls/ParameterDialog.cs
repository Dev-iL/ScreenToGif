using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace ScreenToGif.Linux.Controls;

public sealed class ParameterDialog : Window
{
    private readonly TextBox[] _inputs;

    public ParameterDialog(string title, params (string Label, string Value)[] fields)
    {
        Title = title;
        Width = 360;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Avalonia.Media.Brush.Parse("#25272A");

        var panel = new StackPanel { Margin = new Thickness(18), Spacing = 10 };
        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = Avalonia.Media.FontWeight.SemiBold
        });

        _inputs = new TextBox[fields.Length];
        for (var index = 0; index < fields.Length; index++)
        {
            panel.Children.Add(new TextBlock { Text = fields[index].Label });
            _inputs[index] = new TextBox { Text = fields[index].Value, MinHeight = 30 };
            panel.Children.Add(_inputs[index]);
        }

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 6, 0, 0)
        };
        var cancel = new Button { Content = "Cancel", MinWidth = 84 };
        cancel.Click += (_, _) => Close(null);
        var apply = new Button { Content = "Apply", MinWidth = 84 };
        apply.Click += (_, _) => Close(_inputs.Select(input => input.Text ?? string.Empty).ToArray());
        buttons.Children.Add(cancel);
        buttons.Children.Add(apply);
        panel.Children.Add(buttons);
        Content = panel;
    }

    public Task<string[]?> ShowForAsync(Window owner) => ShowDialog<string[]?>(owner);
}

public sealed class ChoiceDialog : Window
{
    private readonly ComboBox _choice;

    public ChoiceDialog(string title, IReadOnlyList<string> choices)
    {
        Title = title;
        Width = 380;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Avalonia.Media.Brush.Parse("#25272A");
        _choice = new ComboBox { ItemsSource = choices, SelectedIndex = 0, MinHeight = 32 };

        var cancel = new Button { Content = "Cancel", MinWidth = 84 };
        cancel.Click += (_, _) => Close(null);
        var apply = new Button { Content = "Apply", MinWidth = 84 };
        apply.Click += (_, _) => Close(_choice.SelectedIndex);
        Content = new StackPanel
        {
            Margin = new Thickness(18),
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = title, FontSize = 18, FontWeight = Avalonia.Media.FontWeight.SemiBold },
                new TextBlock { Text = "Operation" },
                _choice,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { cancel, apply }
                }
            }
        };
    }

    public Task<int?> ShowForAsync(Window owner) => ShowDialog<int?>(owner);
}

public sealed class ConfirmDialog : Window
{
    public ConfirmDialog(string title, string message, string confirmLabel)
    {
        Title = title;
        Width = 420;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Avalonia.Media.Brush.Parse("#25272A");
        var cancel = new Button { Content = "Cancel", MinWidth = 84 };
        cancel.Click += (_, _) => Close(false);
        var confirm = new Button { Content = confirmLabel, MinWidth = 100 };
        confirm.Click += (_, _) => Close(true);
        Content = new StackPanel
        {
            Margin = new Thickness(18),
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = title, FontSize = 18, FontWeight = Avalonia.Media.FontWeight.SemiBold },
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { cancel, confirm }
                }
            }
        };
    }

    public Task<bool> ShowForAsync(Window owner) => ShowDialog<bool>(owner);
}
