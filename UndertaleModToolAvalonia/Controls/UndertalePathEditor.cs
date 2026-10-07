using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public class UndertalePathEditor : Control
{
    readonly double padding = 4;

    protected override Size MeasureOverride(Size availableSize)
    {
        Rect bounds = GetBounds();
        return new Size(bounds.Width, bounds.Height);
    }

    public override void Render(DrawingContext context)
    {
        if (DataContext is not UndertalePathViewModel vm)
            return;
        if (vm.Path.Points.Count == 0)
            return;

        SolidColorBrush axisBrush = this.GetSolidColorBrushResource("SystemControlBackgroundBaseLowBrush");
        SolidColorBrush pathBrush = this.GetSolidColorBrushResource("SystemControlForegroundAccentBrush");

        Rect bounds = GetBounds();

        context.PushTransform(Matrix.CreateTranslation(-bounds.Left, -bounds.Top));

        Pen axisPen = new(axisBrush);
        context.DrawLine(axisPen, new(bounds.Left + padding + 0.5, 0 + 0.5), new(bounds.Right - padding + 0.5, 0 + 0.5));
        context.DrawLine(axisPen, new(0 + 0.5, bounds.Top + padding + 0.5), new(0 + 0.5, bounds.Bottom - padding + 0.5));

        PathGeometry geometry = new();
        PathFigure pathFigure = new() { StartPoint = new(vm.Path.Points[0].X, vm.Path.Points[0].Y), IsClosed = vm.Path.IsClosed, IsFilled = false };

        // TODO: vm.Path.IsSmooth
        foreach (UndertalePath.PathPoint point in vm.Path.Points)
        {
            pathFigure.Segments?.Add(new LineSegment() { Point = new(point.X, point.Y) });
        }

        geometry.Figures?.Add(pathFigure);

        context.DrawGeometry(null, new Pen(pathBrush, thickness: 2), geometry);

        TopLevel topLevel = TopLevel.GetTopLevel(this)!;
        topLevel.RequestAnimationFrame(_ =>
        {
            InvalidateMeasure();
            InvalidateVisual();
        });
    }

    Rect GetBounds()
    {
        if (DataContext is not UndertalePathViewModel vm)
            return new(0, 0, 0, 0);

        float left = 0, top = 0, right = 0, bottom = 0;

        foreach (UndertalePath.PathPoint point in vm.Path.Points)
        {
            if (point.X < left)
                left = point.X;
            if (point.Y < top)
                top = point.Y;
            if (point.X > right)
                right = point.X;
            if (point.Y > bottom)
                bottom = point.Y;
        }

        return new Rect(left - padding, top - padding, -left + right + padding * 2, -top + bottom + padding * 2);
    }
}
