using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EBISX_POS.API.Services.Interfaces;
using EBISX_POS.ViewModels;
using EBISX_POS.ViewModels.Manager;
using Microsoft.Extensions.DependencyInjection;

namespace EBISX_POS;

public partial class RefundItemWindow : Window
{
    public RefundItemWindow()
    {
        InitializeComponent();
        // Get the data service from DI container
        var orderService = App.Current.Services.GetRequiredService<IOrder>();

        // Create and set the ViewModel
        DataContext = new RefundItemViewModel(orderService, this);
    }
}