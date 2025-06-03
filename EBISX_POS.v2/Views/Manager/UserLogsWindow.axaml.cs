using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace EBISX_POS;

public partial class UserLogsWindow : Window
{
    public UserLogsWindow()
    {
        InitializeComponent();
    }
    private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}