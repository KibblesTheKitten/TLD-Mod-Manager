using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TLDMM_NG.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AppData.EnsureCreated();
    }

    private void ButtonClicked(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }
}