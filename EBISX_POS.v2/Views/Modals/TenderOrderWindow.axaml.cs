using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using EBISX_POS.API.Models;
using EBISX_POS.Models;
using EBISX_POS.State;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System.Diagnostics;
using EBISX_POS.Services;
using Microsoft.Extensions.DependencyInjection;
using EBISX_POS.API.Services.DTO.Order;
using System.Linq;
using EBISX_POS.ViewModels;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Globalization;
using System.Collections.Generic;
using EBISX_POS.Util;
using Microsoft.Extensions.Options;
using EBISX_POS.API.Services.DTO.Payment;
using EBISX_POS.Helper;

namespace EBISX_POS.Views
{
    public partial class TenderOrderWindow : Window
    {
        public TenderOrderWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        private void BackButton_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void EnterButton_Click(object? sender, RoutedEventArgs e)
        {
            ShowLoader(true);
            try
            {// calculate totals up front
                var order = TenderState.tenderOrder;
                var cash = order.CashTenderAmount;
                var otherSum = order.OtherPayments?.Sum(x => x.Amount) ?? 0m;
                var paidWithAlter = cash + otherSum;
                var due = order.AmountDue;

                // no zero or negative tender at all
                if (order.TenderAmount <= 0)
                {
                    await ShowWarningAsync("Invalid Tender",
                        "Please enter an amount greater than zero.");
                    return;
                }

                // insufficient total tender
                if (order.TenderAmount < due)
                {
                    await ShowWarningAsync("Insufficient Funds",
                        $"Total tendered ₱{order.TenderAmount:N2} is less than the amount due ₱{due:N2}.");
                    return;
                }

                // if and only if there are alternative payments, block any overpay
                if ((order.OtherPayments?.Any() ?? false) && paidWithAlter > due)
                {
                    await ShowWarningAsync("Excess Payment",
                        $"Combined payments ₱{paidWithAlter:N2} exceed the amount due ₱{due:N2}. " +
                        "Please adjust your cash or other payment amounts.");
                    return;
                }

                // prepare your DTO
                var finalizeDto = new FinalizeOrderDTO
                {
                    TotalAmount = order.TotalAmount,
                    CashTendered = order.CashTenderAmount,
                    OrderType = order.OrderType,
                    DiscountAmount = order.DiscountAmount,
                    ChangeAmount = order.ChangeAmount,
                    DueAmount = due,
                    VatExempt = order.VatExemptSales,
                    VatAmount = order.VatAmount,
                    VatSales = order.VatSales,
                    VatZero = order.VatZero,
                    TotalTendered = order.TenderAmount,
                    CashierEmail = CashierState.CashierEmail ?? ""
                };

                // services
                var paymentSvc = App.Current.Services.GetRequiredService<PaymentService>();
                var orderSvc = App.Current.Services.GetRequiredService<OrderService>();

                // record other payments, finalize, print

                var otherPayments = order.OtherPayments?.ToList()
                                    ?? new List<AddAlternativePaymentsDTO>();

                await paymentSvc.AddAlternativePayments(otherPayments);
                var posInfo = await orderSvc.FinalizeOrder(finalizeDto);
                await GenerateAndPrintReceiptAsync(posInfo.Response);

                // reset
                OrderState.CurrentOrderItem = new();
                OrderState.CurrentOrder.Clear();
                OrderState.CurrentOrderItem.RefreshDisplaySubOrders();
                TenderState.tenderOrder.Reset();

                Close();
            }
            finally
            {
                ShowLoader(false);
            }
        }
        private async void ExactAmount_Click(object? sender, RoutedEventArgs e)
        {
            ShowLoader(true);
            try
            {// calculate totals up front
                var order = TenderState.tenderOrder;
                var due = order.AmountDue;

                if (order.TenderAmount > 0m)
                {
                    await ShowWarningAsync("Invalid Tender",
                        "Exact‑amount payment can’t be processed because a prior tender already exists.");
                    return;
                }

                order.ApplyExactAmount();


                // prepare your DTO
                var finalizeDto = new FinalizeOrderDTO
                {
                    TotalAmount = order.TotalAmount,
                    CashTendered = order.CashTenderAmount,
                    OrderType = order.OrderType,
                    DiscountAmount = order.DiscountAmount,
                    ChangeAmount = order.ChangeAmount,
                    DueAmount = due,
                    VatExempt = order.VatExemptSales,
                    VatAmount = order.VatAmount,
                    VatSales = order.VatSales,
                    VatZero = order.VatZero,
                    TotalTendered = order.TenderAmount,
                    CashierEmail = CashierState.CashierEmail ?? ""
                };

                // services
                var paymentSvc = App.Current.Services.GetRequiredService<PaymentService>();
                var orderSvc = App.Current.Services.GetRequiredService<OrderService>();

                // record other payments, finalize, print

                var otherPayments = order.OtherPayments?.ToList()
                                    ?? new List<AddAlternativePaymentsDTO>();

                await paymentSvc.AddAlternativePayments(otherPayments);
                var posInfo = await orderSvc.FinalizeOrder(finalizeDto);
                await GenerateAndPrintReceiptAsync(posInfo.Response);

                // reset
                OrderState.CurrentOrderItem = new();
                OrderState.CurrentOrder.Clear();
                OrderState.CurrentOrderItem.RefreshDisplaySubOrders();
                TenderState.tenderOrder.Reset();

                Close();
            }
            finally
            {
                ShowLoader(false);
            }
        }

        /// Centralized warning dialog for insufficient/invalid tenders.
        private Task ShowWarningAsync(string header, string message)
        {
            return MessageBoxManager
                .GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentHeader = header,
                    ContentMessage = message,
                    ButtonDefinitions = ButtonEnum.Ok,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Width = 400,
                    ShowInCenter = true,
                    Icon = MsBox.Avalonia.Enums.Icon.Warning
                })
                .ShowAsPopupAsync(this);
        }


        //private async void PwdScDiscount_Click(object? sender, RoutedEventArgs e)
        //{
        //    if (TenderState.tenderOrder.HasOrderDiscount)
        //    {
        //        await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        //        {
        //            ContentHeader = "Discounted Already",
        //            ContentMessage = "This order is already discounted.",
        //            ButtonDefinitions = ButtonEnum.Ok,
        //            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        //            CanResize = false,
        //            SizeToContent = SizeToContent.WidthAndHeight,
        //            Width = 400,
        //            ShowInCenter = true,
        //            Icon = MsBox.Avalonia.Enums.Icon.Warning
        //        }).ShowAsPopupAsync(this);
        //        return;
        //    }

        //    var result = await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        //    {
        //        ContentHeader = "PWD/SC Discount",
        //        ContentMessage = "Please ask the manager to swipe.",
        //        ButtonDefinitions = ButtonEnum.OkCancel,
        //        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        //        CanResize = false,
        //        SizeToContent = SizeToContent.WidthAndHeight,
        //        Width = 400,
        //        ShowInCenter = true,
        //        Icon = MsBox.Avalonia.Enums.Icon.Warning
        //    }).ShowAsPopupAsync(this);

        //    if (result == ButtonResult.Ok)
        //        TenderState.tenderOrder.HasPwdScDiscount = !TenderState.tenderOrder.HasPwdScDiscount;
        //}

        private async void PromoAndCouponDiscount_Click(object? sender, RoutedEventArgs e)
        {
            // Prevent applying multiple discounts to the same order.
            if (TenderState.tenderOrder.HasScDiscount || TenderState.tenderOrder.HasPwdDiscount || TenderState.tenderOrder.HasPromoDiscount)
            {
                await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentHeader = "Discount Already Applied",
                    ContentMessage = "A discount has already been applied to this order. Please complete or cancel the current order before applying another discount.",
                    ButtonDefinitions = ButtonEnum.Ok,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Width = 400,
                    ShowInCenter = true,
                    Icon = MsBox.Avalonia.Enums.Icon.Warning
                }).ShowAsPopupAsync(this);
                return;
            }

            if (sender is Button btn)
            {
                string discountType = GetDiscountType(btn);

                // Handle actions based on the discount type.
                switch (discountType)
                {
                    case "PROMO":
                        {
                            if (OrderState.CurrentOrder.Any(d => d.CouponCode != null))
                            {
                                await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                                {
                                    ContentHeader = "Coupon Discount Detected",
                                    ContentMessage = "This order already has a coupon discount applied.",
                                    ButtonDefinitions = ButtonEnum.Ok,
                                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                                    CanResize = false,
                                    SizeToContent = SizeToContent.WidthAndHeight,
                                    Width = 400,
                                    ShowInCenter = true,
                                    Icon = MsBox.Avalonia.Enums.Icon.Warning
                                }).ShowAsPopupAsync(this);
                                return;
                            }

                            var swipeManager = new ManagerSwipeWindow(header: "Apply Promo Discount", message: "Please enter manager email.", ButtonName: "Submit");
                            var (success, email) = await swipeManager.ShowDialogAsync(this);

                            if (success)
                            {
                                var promoWindow = new DiscountCodeWindow("PROMO", email);
                                await promoWindow.ShowDialog((Window)this.VisualRoot);
                            }

                            return;
                        }
                    case "COUPON":
                        {
                            if (OrderState.CurrentOrder.Count(d => d.CouponCode != null) >= 3)
                            {
                                await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                                {
                                    ContentHeader = "Maximum Coupon Discount Reached",
                                    ContentMessage = "This order already has the maximum allowed coupon discounts applied (3 coupons). You cannot apply any additional coupon discounts.",
                                    ButtonDefinitions = ButtonEnum.Ok,
                                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                                    CanResize = false,
                                    SizeToContent = SizeToContent.WidthAndHeight,
                                    Width = 400,
                                    ShowInCenter = true,
                                    Icon = MsBox.Avalonia.Enums.Icon.Warning
                                }).ShowAsPopupAsync(this);
                                return;
                            }

                            var swipeManager = new ManagerSwipeWindow(header: "Apply Coupon Discount", message: "Please enter manager email.", ButtonName: "Submit");
                            var (success, email) = await swipeManager.ShowDialogAsync(this);

                            if (success)
                            {
                                var promoWindow = new DiscountCodeWindow("COUPON", email);
                                await promoWindow.ShowDialog((Window)this.VisualRoot);
                            }

                            return;
                        }
                    default:
                        {
                            await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                            {
                                ContentHeader = "Unknown Discount Type",
                                ContentMessage = "The selected discount type is not recognized. Please try again or contact support.",
                                ButtonDefinitions = ButtonEnum.Ok,
                                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                                CanResize = false,
                                SizeToContent = SizeToContent.WidthAndHeight,
                                Width = 400,
                                ShowInCenter = true,
                                Icon = MsBox.Avalonia.Enums.Icon.Warning
                            }).ShowAsPopupAsync(this);
                            return;
                        }
                }
            }
        }

        private async void OtherPayment_Click(object? sender, RoutedEventArgs e)
        {
            if (TenderState.tenderOrder.CashTenderAmount >= TenderState.tenderOrder.AmountDue && TenderState.tenderOrder.TenderAmount > 0 || TenderState.tenderOrder.TotalAmount < 1)
                return;

            var tenderOrderViewModel = App.Current.Services.GetRequiredService<TenderOrderViewModel>();
            var otherPayment = new AlternativePaymentsWindow();
            await otherPayment.ShowDialog((Window)this.VisualRoot);
        }

        private string GetDiscountType(ContentControl control)
        {
            // Check if the Content is a TextBlock.
            if (control.Content is TextBlock textBlock)
            {
                return textBlock.Text;
            }
            // If the Content is a Panel (e.g., a StackPanel), attempt to get the first TextBlock.
            else if (control.Content is Panel panel)
            {
                var childTextBlock = panel.Children.OfType<TextBlock>().FirstOrDefault();
                return childTextBlock?.Text ?? string.Empty;
            }
            // Fallback: return the Content's string representation.
            return control.Content?.ToString() ?? string.Empty;
        }

        private async Task GenerateAndPrintReceiptAsync(FinalizeOrderResponseDTO finalizeOrder)
        {
            var reportOptions = App.Current.Services.GetRequiredService<IOptions<SalesReport>>();

            // Define target folder and file paths.
            string folderPath = reportOptions.Value.Receipts;
            string fileName = $"Receipt-{DateTimeOffset.UtcNow.ToString("MMMM-dd-yyyy-HH-mm-ss")}.txt";

            //// Ensure the target directory exists.
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, fileName);

            try
            {

                // Write receipt content to file.
                //WriteReceiptContent(filePath, finalizeOrder);
                ReceiptPrinterUtil.PrintInvoice(folderPath, filePath, finalizeOrder);
                TenderState.ElligiblePWDSCDiscount?.Clear();

                // Open the receipt file automatically.
            }
            catch (UnauthorizedAccessException)
            {
                await MessageBoxManager
                    .GetMessageBoxStandard("Error", "Access Denied! Run the app as Administrator or choose another folder.", ButtonEnum.Ok)
                    .ShowAsPopupAsync(this);
            }
            catch (Exception ex)
            {
                await MessageBoxManager
                    .GetMessageBoxStandard("Error", ex.Message, ButtonEnum.Ok)
                    .ShowAsPopupAsync(this);
            }
        }
        private void ShowLoader(bool show)
        {
            var overlay = this.FindControl<Grid>("LoadingOverlay");
            if (overlay != null)
                overlay.IsVisible = show;
        }
    }
};
