using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EBISX_POS.API.Services.Interfaces;
using EBISX_POS.ViewModels.Manager;
using Microsoft.Extensions.DependencyInjection;

namespace EBISX_POS;

public partial class AddCouponPromoWindow : Window
{
    private AddCouponPromoViewModel ViewModel => (AddCouponPromoViewModel)DataContext!;

    public AddCouponPromoWindow()
    {
        InitializeComponent();

        // Get the data service from DI container
        var menuService = App.Current.Services.GetRequiredService<IMenu>();

        // Create and set the ViewModel
        DataContext = new AddCouponPromoViewModel(menuService, this);
    }
} 