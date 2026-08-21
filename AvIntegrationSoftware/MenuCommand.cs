using System;
using System.Windows.Input;
using Avalonia.Controls;

namespace AvIntegrationSoftware;

public class MenuCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public MenuCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public event EventHandler? CanExecuteChanged;
    
    public void RaiseCanExecuteChanged()
        => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}