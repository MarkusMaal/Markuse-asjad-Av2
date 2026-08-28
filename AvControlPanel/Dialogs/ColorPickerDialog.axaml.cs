using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AvControlPanel.Dialogs;

public partial class ColorPickerDialog : Window
{
    public delegate void OkHandler(ColorPickerEventArgs e);
    public event OkHandler? DialogOk;
    public Color CurrentColor { get; set; }

    public class ColorPickerEventArgs(Color color) : EventArgs
    {
        public Color? Color { get; } = color;
    }
    public ColorPickerDialog()
    {
        InitializeComponent();
    }

    private void Confirm(object? sender, RoutedEventArgs e)
    {
        DialogOk?.Invoke(new ColorPickerEventArgs(Color.Color));
        Close();
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        Color.Color = CurrentColor;
    }
}