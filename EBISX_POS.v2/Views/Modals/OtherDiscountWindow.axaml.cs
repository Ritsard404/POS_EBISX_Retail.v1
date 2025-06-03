using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using EBISX_POS.Services;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System.Text.RegularExpressions;
using EBISX_POS.API.Services.DTO.Order;
using EBISX_POS.Models;
using EBISX_POS.State;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

namespace EBISX_POS;

public partial class OtherDiscountWindow : Window
{
    private string _managerEmail;
    public OtherDiscountWindow(string managerEmail)
    {
        InitializeComponent();
        _managerEmail = managerEmail;
    }
    private void PercentDiscountTextBox_OnTextInput(object sender, TextInputEventArgs e)
    {
        if (sender is TextBox tb)
        {
            // what the text *would* be if we accepted this keystroke
            var current = tb.Text ?? "";
            var prospective = current.Insert(tb.CaretIndex, e.Text);

            // 1) only digits (no decimal point)
            // 2) max length 3 (to allow “100”)
            if (!Regex.IsMatch(prospective, @"^\d{0,3}$"))
            {
                e.Handled = true;
                return;
            }

            // 3) parse and enforce <= 100
            if (int.TryParse(prospective, out var val) && val > 100)
            {
                e.Handled = true;
            }
        }
    }

    private async void Apply_Click(object? sender, RoutedEventArgs e)
    {
        LoadingOverlay.IsVisible = true;
        var orderService = App.Current.Services.GetRequiredService<OrderService>();

        var nameText = Name.Text?.Trim();
        var percentText = PercentDiscountTextBox.Text?.Trim();

        // both fields required
        if (string.IsNullOrWhiteSpace(nameText) || string.IsNullOrWhiteSpace(percentText))
        {
            await MessageBoxManager
                .GetMessageBoxStandard("Invalid Input", "Both Name and Discount are required.", ButtonEnum.Ok)
                .ShowAsPopupAsync(this);
            LoadingOverlay.IsVisible = false;
            return;
        }

        // parse as integer and enforce 0–100
        if (!int.TryParse(percentText, out var discount)
            || discount < 0
            || discount > 100)
        {
            await MessageBoxManager
                .GetMessageBoxStandard("Invalid Discount", "Enter a whole-number percent between 0 and 100.", ButtonEnum.Ok)
                .ShowAsPopupAsync(this);
            LoadingOverlay.IsVisible = false;
            return;
        }

        await orderService.AddOtherDiscount(new AddOtherDiscountDTO()
        {
            DiscPercent = discount,
            DiscountName = nameText,
            CashierEmail = CashierState.CashierEmail,
            ManagerEmail = _managerEmail
        });

        var ordersDto = await orderService.GetCurrentOrderItems();

        // If the items collection has empty items, exit.
        if (!ordersDto.Any())
        {
            Submit_Button.IsEnabled = true;
            return;
        }
        OrderState.CurrentOrder.Clear();

        TenderState.ElligiblePWDSCDiscount = new List<string>
            {
                nameText
            };
        
        Name.Clear();
        PercentDiscountTextBox.Clear();

        foreach (var dto in ordersDto)
        {
            // Map the DTO's SubOrders to an ObservableCollection<SubOrderItem>
            var subOrders = new ObservableCollection<SubOrderItem>(
                dto.SubOrders.Select(s => new SubOrderItem
                {
                    MenuId = s.MenuId,
                    DrinkId = s.DrinkId,
                    AddOnId = s.AddOnId,
                    Name = s.Name,
                    ItemPrice = s.ItemPrice,
                    Size = s.Size,
                    Quantity = s.Quantity,
                    IsFirstItem = s.IsFirstItem,
                    IsOtherDisc = s.IsOtherDisc
                })
            );

            // Create a new OrderItemState from the DTO.
            var pendingItem = new OrderItemState()
            {
                ID = dto.EntryId,             // Using EntryId from the DTO.
                Quantity = dto.TotalQuantity, // Total quantity from the DTO.
                TotalPrice = dto.TotalPrice,  // Total price from the DTO.
                HasCurrentOrder = dto.HasCurrentOrder,
                SubOrders = subOrders,
                HasDiscount = dto.HasDiscount,// Mapped sub-orders.
                TotalDiscountPrice = dto.DiscountAmount,
                IsPwdDiscounted = dto.IsPwdDiscounted,
                IsSeniorDiscounted = dto.IsSeniorDiscounted,
                PromoDiscountAmount = dto.PromoDiscountAmount,
                HasPwdScDiscount = dto.HasDiscount && dto.PromoDiscountAmount == null,
                CouponCode = dto.CouponCode

            };

            // Add the mapped OrderItemState to the static collection.
            OrderState.CurrentOrder.Add(pendingItem);
        }

        // Refresh UI display (if needed by your application).
        OrderState.CurrentOrderItem.RefreshDisplaySubOrders();


        TenderState.tenderOrder.Reset();
        TenderState.tenderOrder.HasScDiscount = OrderState.CurrentOrder.Any(d => d.IsSeniorDiscounted);
        TenderState.tenderOrder.HasPwdDiscount = OrderState.CurrentOrder.Any(d => d.IsPwdDiscounted);

        // Select the PromoDiscountAmount from the first order that has a non-null value
        TenderState.tenderOrder.PromoDiscountAmount = OrderState.CurrentOrder
            .Where(d => d.PromoDiscountAmount != null)
            .Select(d => d.PromoDiscountAmount)
            .FirstOrDefault() ?? 0m;
        TenderState.tenderOrder.CalculateTotalAmount();
        LoadingOverlay.IsVisible = false;
        Close();
    }
}
