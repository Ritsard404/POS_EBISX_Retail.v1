using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using EBISX_POS.Models;
using EBISX_POS.Services;
using EBISX_POS.State;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace EBISX_POS.Views
{
    public partial class DiscountCodeWindow : Window
    {
        // DiscountType should be "PROMO" or "COUPON"
        public string DiscountType { get; }
        public string ManagerEmail { get; }

        public DiscountCodeWindow(string discountType, string managerEmail)
        {
            InitializeComponent();
            DiscountType = discountType?.ToUpperInvariant() ?? "PROMO"; // default to PROMO if null
            ManagerEmail = managerEmail;
            CodeTextBox = this.FindControl<TextBox>("CodeTextBox");
            CodeTextBox.Watermark = DiscountType == "PROMO" ? "Enter Promo Code" :
                                    DiscountType == "COUPON" ? "Enter Coupon Code" :
                                    "Enter Discount Code";
            // Set the window title based on the discount type.
            Title = DiscountType == "PROMO" ? "Promo Code" :
                    DiscountType == "COUPON" ? "Coupon Code" :
                    "Discount Code";
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public async void ApplyCodeButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate the code input.
            if (string.IsNullOrWhiteSpace(CodeTextBox?.Text))
            {
                await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentHeader = "Input Error",
                    ContentMessage = $"{DiscountType} Code cannot be empty. Please enter a valid code.",
                    ButtonDefinitions = ButtonEnum.Ok,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Width = 400,
                    ShowInCenter = true,
                    Icon = MsBox.Avalonia.Enums.Icon.Error
                }).ShowAsPopupAsync(this);
                Close();
                return;
            }

            // Retrieve the OrderService from DI.
            var orderService = App.Current.Services.GetRequiredService<OrderService>();
            var trimmedCode = CodeTextBox.Text.Trim();


            (bool isSuccess, string message) result;
            // Decide which service method to call based on the discount type.
            if (DiscountType == "PROMO")
            {
                // For demo purposes, using a placeholder manager email.
                result = await orderService.PromoDiscount(managerEmail: ManagerEmail, promoCode: trimmedCode);
            }
            else if (DiscountType == "COUPON")
            {
                // For coupon, assume we use the AvailCoupon method.
                result = await orderService.AvailCoupon(managerEmail: ManagerEmail, couponCode: trimmedCode);
            }
            else
            {
                result = (false, "Invalid discount type specified.");
            }

            if (!result.isSuccess)
            {
                // Show error message returned from the service.
                await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentHeader = $"{DiscountType} Code Error",
                    ContentMessage = result.message,
                    ButtonDefinitions = ButtonEnum.Ok,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Width = 400,
                    ShowInCenter = true,
                    Icon = MsBox.Avalonia.Enums.Icon.Error
                }).ShowAsPopupAsync(this);
                Close();
                return;
            }

            // Process result based on discount type.
            if (DiscountType == "PROMO")
            {
                // Expect the promo discount to return a numeric discount amount.
                if (decimal.TryParse(result.message, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal discountAmount))
                {
                    TenderState.tenderOrder.PromoDiscountAmount = discountAmount;
                    TenderState.tenderOrder.PromoDiscountName = trimmedCode;
                }
                else
                {
                    // Optionally handle parsing error here.
                    await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                    {
                        ContentHeader = "Promo Discount Error",
                        ContentMessage = "Failed to parse the promo discount amount.",
                        ButtonDefinitions = ButtonEnum.Ok,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        CanResize = false,
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Width = 400,
                        ShowInCenter = true,
                        Icon = MsBox.Avalonia.Enums.Icon.Error
                    }).ShowAsPopupAsync(this);
                    Close();
                    return;
                }
            }
            else if (DiscountType == "COUPON")
            {
                // For coupon discount, update the current order items.
                var ordersDto = await orderService.GetCurrentOrderItems();
                if (!ordersDto.Any())
                    return;

                OrderState.CurrentOrder.Clear();
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
                    var pendingItem = new OrderItemState
                    {
                        ID = dto.EntryId,             // Using EntryId from the DTO.
                        Quantity = dto.TotalQuantity, // Total quantity from the DTO.
                        TotalPrice = dto.TotalPrice,  // Total price from the DTO.
                        HasCurrentOrder = dto.HasCurrentOrder,
                        SubOrders = subOrders,
                        HasDiscount = dto.HasDiscount, // Mapped sub-orders.
                        TotalDiscountPrice = dto.DiscountAmount,
                        IsPwdDiscounted = dto.IsPwdDiscounted,
                        IsSeniorDiscounted = dto.IsSeniorDiscounted,
                        PromoDiscountAmount = dto.PromoDiscountAmount,
                        HasPwdScDiscount = dto.HasDiscount && dto.PromoDiscountAmount == null,
                        CouponCode = dto.CouponCode
                    };

                    OrderState.CurrentOrder.Add(pendingItem);
                }

                // Refresh UI display if needed.
                OrderState.CurrentOrderItem.RefreshDisplaySubOrders();
            }

            // Recalculate the tender order total.
            TenderState.tenderOrder.CalculateTotalAmount();

            Close();
        }
    }
}
