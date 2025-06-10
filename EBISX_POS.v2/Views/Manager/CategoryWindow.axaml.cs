using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EBISX_POS.API.Models;
using EBISX_POS.API.Services.Interfaces;
using EBISX_POS.ViewModels.Manager;
using Microsoft.Extensions.DependencyInjection;

namespace EBISX_POS;

public partial class CategoryWindow : Window
{
    private CategoryViewModel ViewModel => (CategoryViewModel)DataContext!;

    public CategoryWindow()
    {
        InitializeComponent();

        // Get the data service from DI container
        var menuService = App.Current.Services.GetRequiredService<IMenu>();

        // Create and set the ViewModel
        DataContext = new CategoryViewModel(menuService, this);
    }

    private async void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Category category)
        {
            await ViewModel.RemoveCategory(category);
        }
    }

    private async void EditButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Category category)
        {
            await ViewModel.EditCategory(category);
        }
    }
}