using Avalonia.Controls;
using EBISX_POS.API.Services.Interfaces;
using EBISX_POS.ViewModels.Manager;
using Microsoft.Extensions.DependencyInjection;

namespace EBISX_POS;

public partial class AppUsersWindow : Window
{
    private AppUsersViewModel ViewModel => (AppUsersViewModel)DataContext!;

    public AppUsersWindow()
    {
        InitializeComponent();
        
        // Get the data service from DI container
        var dataService = App.Current.Services.GetRequiredService<IData>();
        
        // Create and set the ViewModel
        DataContext = new AppUsersViewModel(dataService, this);
    }
    private void OnStatusComboLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is ComboBox combo)
        {
            combo.ItemsSource = ViewModel.StatusOptions;
        }
    }
    private void OnRoleComboLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is ComboBox combo)
        {
            combo.ItemsSource = ViewModel.Roles;
        }
    }
}