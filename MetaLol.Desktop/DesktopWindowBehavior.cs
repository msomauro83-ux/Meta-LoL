using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MetaLol.Desktop;

/// <summary>Shared sizing and background dragging for the borderless desktop windows.</summary>
public static class DesktopWindowBehavior
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(DesktopWindowBehavior), new PropertyMetadata(false, EnabledChanged));
    private static readonly DependencyProperty DragStartProperty = DependencyProperty.RegisterAttached(
        "DragStart", typeof(Point?), typeof(DesktopWindowBehavior), new PropertyMetadata(null));

    public static void SetEnabled(DependencyObject element, bool value) => element.SetValue(EnabledProperty, value);
    public static bool GetEnabled(DependencyObject element) => (bool)element.GetValue(EnabledProperty);

    private static void EnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not Window window) return;
        if ((bool)args.NewValue)
        {
            window.SourceInitialized += SourceInitialized;
            window.PreviewMouseLeftButtonDown += BackgroundMouseDown;
            window.PreviewMouseMove += BackgroundMouseMove;
            window.PreviewMouseLeftButtonUp += BackgroundMouseUp;
        }
        else
        {
            window.SourceInitialized -= SourceInitialized;
            window.PreviewMouseLeftButtonDown -= BackgroundMouseDown;
            window.PreviewMouseMove -= BackgroundMouseMove;
            window.PreviewMouseLeftButtonUp -= BackgroundMouseUp;
        }
    }

    private static void SourceInitialized(object? sender, EventArgs args)
    {
        if (sender is Window window) FitToWorkArea(window, SystemParameters.WorkArea);
    }

    internal static void FitToWorkArea(Window window, Rect workArea)
    {
        const double margin = 8;
        var availableWidth = Math.Max(1, workArea.Width - margin * 2);
        var availableHeight = Math.Max(1, workArea.Height - margin * 2);
        window.MinWidth = Math.Min(window.MinWidth, availableWidth);
        window.MinHeight = Math.Min(window.MinHeight, availableHeight);
        window.Width = Math.Clamp(double.IsNaN(window.Width) ? availableWidth : window.Width, window.MinWidth, availableWidth);
        window.Height = Math.Clamp(double.IsNaN(window.Height) ? availableHeight : window.Height, window.MinHeight, availableHeight);
        var owner = window.Owner;
        var centerX = owner is null ? workArea.Left + workArea.Width / 2 : owner.Left + owner.ActualWidth / 2;
        var centerY = owner is null ? workArea.Top + workArea.Height / 2 : owner.Top + owner.ActualHeight / 2;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = Math.Clamp(centerX - window.Width / 2, workArea.Left + margin, workArea.Right - margin - window.Width);
        window.Top = Math.Clamp(centerY - window.Height / 2, workArea.Top + margin, workArea.Bottom - margin - window.Height);
    }

    internal static bool CanDragFrom(DependencyObject? source, Func<TextBlock, bool>? hitsText = null)
    {
        for (var current = source; current is not null; current = Parent(current))
        {
            // Test the complete ancestry, including templates and inline text inside controls.
            if (current is ButtonBase or Selector or TextBoxBase or PasswordBox or ScrollBar or Thumb or Slider or Hyperlink)
                return false;
            if (current is TextBlock text && !string.IsNullOrWhiteSpace(new TextRange(text.ContentStart, text.ContentEnd).Text) &&
                (hitsText is null || hitsText(text))) return false;
        }
        return source is not null;
    }

    private static DependencyObject? Parent(DependencyObject element) => element switch
    {
        Visual or Visual3D => VisualTreeHelper.GetParent(element) ?? LogicalTreeHelper.GetParent(element),
        FrameworkContentElement content => content.Parent ?? ContentOperations.GetParent(content),
        _ => LogicalTreeHelper.GetParent(element)
    };

    private static void BackgroundMouseDown(object sender, MouseButtonEventArgs args)
    {
        if (sender is not Window window || args.OriginalSource is not DependencyObject source ||
            args.LeftButton != MouseButtonState.Pressed ||
            !CanDragFrom(source, text => !text.IsMeasureValid || !text.IsArrangeValid ||
                text.GetPositionFromPoint(args.GetPosition(text), false) is not null)) return;

        if (args.ClickCount == 2 && window.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip)
        {
            window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            args.Handled = true;
            return;
        }
        window.SetValue(DragStartProperty, args.GetPosition(window));
    }

    private static void BackgroundMouseUp(object sender, MouseButtonEventArgs args) =>
        ((Window)sender).ClearValue(DragStartProperty);

    private static void BackgroundMouseMove(object sender, MouseEventArgs args)
    {
        var window = (Window)sender;
        if (args.LeftButton != MouseButtonState.Pressed)
        {
            window.ClearValue(DragStartProperty);
            return;
        }
        if (window.GetValue(DragStartProperty) is not Point start) return;
        var relative = args.GetPosition(window);
        if (Math.Abs(relative.X - start.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(relative.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        window.ClearValue(DragStartProperty);
        if (window.WindowState == WindowState.Maximized)
        {
            var screen = window.PointToScreen(relative);
            var transform = PresentationSource.FromVisual(window)?.CompositionTarget?.TransformFromDevice;
            if (transform is { } matrix) screen = matrix.Transform(screen);
            var fraction = Math.Clamp(start.X / Math.Max(1, window.ActualWidth), 0, 1);
            var verticalFraction = Math.Clamp(start.Y / Math.Max(1, window.ActualHeight), 0, 1);
            var restoreWidth = window.RestoreBounds.Width;
            var restoreHeight = window.RestoreBounds.Height;
            window.WindowState = WindowState.Normal;
            window.Left = screen.X - restoreWidth * fraction;
            window.Top = screen.Y - restoreHeight * verticalFraction;
        }
        args.Handled = true;
        // Windows owns the actual move loop, preserving snapping and normal mouse release behavior.
        try
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed) window.DragMove();
        }
        catch (InvalidOperationException) when (Mouse.LeftButton != MouseButtonState.Pressed) { }
    }
}
