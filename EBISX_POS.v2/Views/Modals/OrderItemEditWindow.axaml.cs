using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using EBISX_POS.Models;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using EBISX_POS.State;
using EBISX_POS.ViewModels;
using System.Linq;
using EBISX_POS.Services;
using Microsoft.Extensions.DependencyInjection;
using EBISX_POS.API.Services.DTO.Order;
using EBISX_POS.API.Models;

namespace EBISX_POS.Views
{
    public partial class OrderItemEditWindow : Window
    {
        public OrderItemEditWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void SaveButton_Click(object? sender, RoutedEventArgs e)
        {
            var orderService = App.Current.Services.GetRequiredService<OrderService>();

            // Get the view model from DataContext
            if (DataContext is not OrderItemEditWindowViewModel viewModel)
                return;

            if (viewModel.OriginalQuantity == viewModel.OrderItem.Quantity)
            {
                Close();
                return;
            }

            // Retrieve the order item from the view model
            var orderItem = viewModel.OrderItem;

            var newQty = new EditOrderItemQuantityDTO()
            {
                entryId = orderItem.ID,
                qty = orderItem.Quantity,
                CashierEmail = CashierState.CashierEmail ?? ""

            };
            
            await orderService.EditQtyOrderItem(newQty);

            // Close the current window
            Close();
        }

        private async void VoidButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the view model from DataContext
            var viewModel = DataContext as OrderItemEditWindowViewModel;
            if (viewModel == null)
                return;

            // Retrieve the order item from the view model
            var orderItem = viewModel.OrderItem;

            var swipeManager = new ManagerSwipeWindow(header: "Manager", message: "Please ask the manager to enter email.", ButtonName: "Void");
            var (success, email) = await swipeManager.ShowDialogAsync(this);

            if (success)
            {
                OrderState.VoidCurrentOrder(orderItem, email);
                Close();
            }
            return;

            //var box = MessageBoxManager.GetMessageBoxStandard(
            //    new MessageBoxStandardParams
            //    {
            //        ContentHeader = $"Void Order",
            //        ContentMessage = "Please ask the manager to swipe.",
            //        ButtonDefinitions = ButtonEnum.OkCancel, // Defines the available buttons
            //        WindowStartupLocation = WindowStartupLocation.CenterOwner,
            //        CanResize = false,
            //        SizeToContent = SizeToContent.WidthAndHeight,
            //        Width = 400,
            //        ShowInCenter = true,
            //        Icon = MsBox.Avalonia.Enums.Icon.Warning
            //    });

            //var result = await box.ShowAsPopupAsync(this);
            //switch (result)
            //{
            //    case ButtonResult.Ok:
            //        OrderState.VoidCurrentOrder(orderItem);
            //        Close();
            //        return;
            //    case ButtonResult.Cancel:
            //        return;
            //    default:
            //        return;
            //}
        }
    }
};