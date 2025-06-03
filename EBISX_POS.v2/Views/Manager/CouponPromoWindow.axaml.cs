using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EBISX_POS.API.Models;
using EBISX_POS.API.Services.Interfaces;
using EBISX_POS.ViewModels.Manager;
using Microsoft.Extensions.DependencyInjection;

namespace EBISX_POS;

public partial class CouponPromoWindow : Window
{
    private CouponPromoViewModel ViewModel => (CouponPromoViewModel)DataContext!;
    
    public CouponPromoWindow()
    {
        InitializeComponent();

        // Get the data service from DI container
        var menuService = App.Current.Services.GetRequiredService<IMenu>();

        // Create and set the ViewModel
        DataContext = new CouponPromoViewModel(menuService, this);
    }

    private async void DeleteButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is CouponPromo couponPromo)
        {
            await ViewModel.RemoveCouponPromo(couponPromo);
        }
    }

    private async void EditButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is CouponPromo couponPromo)
        {
            await ViewModel.EditCouponPromo(couponPromo);
        }
    }
} 