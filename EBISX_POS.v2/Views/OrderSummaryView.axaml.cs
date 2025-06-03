using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EBISX_POS.Models;
using EBISX_POS.State;
using EBISX_POS.ViewModels;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia;
using System.Diagnostics;
using MsBox.Avalonia.Enums;
using Avalonia.Controls.ApplicationLifetimes;
using EBISX_POS.Services;
using EBISX_POS.API.Services.DTO.Order;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace EBISX_POS.Views
{
    public partial class OrderSummaryView : UserControl
    {
        private readonly OrderService _orderService;

        public OrderSummaryView(OrderService orderService)
        {
            InitializeComponent();
            _orderService = orderService; // Assign the injected service
            DataContext = new OrderSummaryViewModel();
        }

        public OrderSummaryView() : this(App.Current.Services.GetRequiredService<OrderService>())
        {
            // This constructor is required for Avalonia to instantiate the view in XAML.
        }

        private async void EditOrder_Button(object? sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton && clickedButton.DataContext is OrderItemState SelectedCurrentOrderItem)
            {
                var detailsWindow = new OrderItemEditWindow()
                {
                    DataContext = new OrderItemEditWindowViewModel(SelectedCurrentOrderItem)
                };

                if (SelectedCurrentOrderItem.CouponCode != null || SelectedCurrentOrderItem.SubOrders.Any(s => s.IsOtherDisc) || SelectedCurrentOrderItem.HasDiscount)
                    return;

                await detailsWindow.ShowDialog((Window)this.VisualRoot);
            }
        }

        private async void VoidCurrentOrder_Button(object? sender, RoutedEventArgs e)
        {
            var parentWindow = this.VisualRoot as Window; // Find the parent window

            var swipeManager = new ManagerSwipeWindow(header: "Void Order", message: "Please swipe your manager card to void the order.", ButtonName: "Void");
            var (isValid, email) = await swipeManager.ShowDialogAsync(parentWindow);

            //var box = MessageBoxManager.GetMessageBoxStandard(
            //    new MessageBoxStandardParams
            //    {
            //        ContentHeader = "Void Order",
            //        ContentMessage = "Please ask the manager to swipe.",
            //        ButtonDefinitions = ButtonEnum.OkCancel,
            //        WindowStartupLocation = WindowStartupLocation.CenterOwner,
            //        CanResize = false,
            //        SizeToContent = SizeToContent.WidthAndHeight,
            //        Width = 300,
            //        ShowInCenter = true,
            //        Icon = Icon.Warning
            //    });

            //var result = await box.ShowAsPopupAsync(owner);

            var subOrders = OrderState.CurrentOrderItem?.DisplaySubOrders;

            var voidOrder = new AddCurrentOrderVoidDTO
            {
                qty = OrderState.CurrentOrderItem.Quantity,
                menuId = subOrders?.FirstOrDefault(m => m.MenuId != null)?.MenuId ?? 0,
                drinkId = subOrders?.FirstOrDefault(m => m.DrinkId != null)?.DrinkId ?? 0,
                addOnId = subOrders?.FirstOrDefault(m => m.AddOnId != null)?.AddOnId ?? 0,
                drinkPrice = subOrders?.FirstOrDefault(m => m.DrinkId != null)?.ItemPrice ?? 0,
                addOnPrice = subOrders?.FirstOrDefault(m => m.AddOnId != null)?.ItemPrice ?? 0,
                managerEmail = "user1@example.com",
                cashierEmail = CashierState.CashierEmail ?? ""
            };


            if (isValid)
            {

                if (OrderState.CurrentOrderItem.TotalPrice > 0 || OrderState.CurrentOrderItem.Quantity > 0 || OrderState.CurrentOrderItem.SubOrders.Any(i => i.Name != "Select Menu"))
                {
                    var (success, message) = await _orderService.AddCurrentOrderVoid(voidOrder);


                    if (success)
                    {
                        Debug.WriteLine("VoidCurrentOrder_Button: Order voided successfully.");
                        OrderState.CurrentOrderItem = new OrderItemState();
                        OrderState.CurrentOrderItem.RefreshDisplaySubOrders();
                    }
                    else
                    {
                        OrderState.CurrentOrderItem = new OrderItemState();
                        OrderState.CurrentOrderItem.RefreshDisplaySubOrders();
                    }
                }

                return;
            }
            else
            {
                // Handle failed swipe logic here
                Console.WriteLine("Manager card not swiped. Order voiding cancelled.");
            }

            //switch (isSwiped)
            //{
            //    case ButtonResult.Ok:
            //        OrderState.CurrentOrderItem = new OrderItemState();
            //        OrderState.CurrentOrderItem.RefreshDisplaySubOrders();

            //        if (OrderState.CurrentOrderItem.TotalPrice > 0 || OrderState.CurrentOrderItem.Quantity > 0 || OrderState.CurrentOrderItem.SubOrders.Any(i => i.Name != "Select Menu"))
            //        {
            //            var (success, message) = await _orderService.AddCurrentOrderVoid(voidOrder);


            //            if (success)
            //            {
            //                Debug.WriteLine("VoidCurrentOrder_Button: Order voided successfully.");
            //            }
            //            else
            //            {
            //                Debug.WriteLine($"VoidCurrentOrder_Button: Error - {message}");
            //            }
            //        }

            //        return;
            //    case ButtonResult.Cancel:
            //        Debug.WriteLine("VoidCurrentOrder_Button: Order voiding canceled.");
            //        return;
            //    default:
            //        return;
            //}

        }
    }
};
