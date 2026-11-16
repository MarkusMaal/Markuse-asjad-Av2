using Avalonia.Controls;

namespace AvParty;

public partial class MessageEntry : Window
{
    public MessageEntry()
    {
        InitializeComponent();
    }

    private void OK_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void Window_Loaded_2(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        MessageField.Focus();
    }

    private void TextBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter) Close();
    }
}