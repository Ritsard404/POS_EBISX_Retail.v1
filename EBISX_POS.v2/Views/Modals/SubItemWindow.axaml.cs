using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EBISX_POS.ViewModels;
using EBISX_POS.Models;
using EBISX_POS.Services;
using EBISX_POS.State;
using Avalonia.Interactivity;

namespace EBISX_POS.Views
{
    public partial class SubItemWindow : Window
    {
        public SubItemWindow(ItemMenu item, MenuService menuService)
        {
            InitializeComponent();
            DataContext = new SubItemWindowViewModel(item, menuService, this);

        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            // Close the current window
            this.Close();
        }

        private async void AddOrderButton_Click(object? sender, RoutedEventArgs e)
        {
            await OrderState.FinalizeCurrentOrder(isSolo: false, this);
            TenderState.tenderOrder.CalculateTotalAmount();
            Close();
        }
    }
}
