using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using EBISX_POS.Services;
using EBISX_POS.State;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using System.Diagnostics;
using System.Globalization;

namespace EBISX_POS.Views
{
    public partial class PromoCodeWindow : Window
    {
        public PromoCodeWindow()
        {
            InitializeComponent();
            PromoCodeTextBox = this.FindControl<TextBox>("PromoCodeTextBox");
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        public async void ApplyCodeButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate the Promo Code input
            if (string.IsNullOrWhiteSpace(PromoCodeTextBox?.Text))
            {
                await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentHeader = "Error",
                    ContentMessage = "Promo Code cannot be empty!",
                    ButtonDefinitions = ButtonEnum.Ok,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Width = 400,
                    ShowInCenter = true,
                    Icon = MsBox.Avalonia.Enums.Icon.Error
                }).ShowAsPopupAsync(this);
                return;
            }

            // Retrieve the service and execute the promo discount logic
            var orderService = App.Current.Services.GetRequiredService<OrderService>();
            var trimmedPromoCode = PromoCodeTextBox.Text.Trim();
            // TODO: Replace "dasdas" with the actual manager email if needed.
            var (isSuccess, message) = await orderService.PromoDiscount(managerEmail: "dasdas", promoCode: trimmedPromoCode);

            if (!isSuccess)
            {
                // Show error message returned from service
                await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentHeader = "Promo Discount Error",
                    ContentMessage = message,
                    ButtonDefinitions = ButtonEnum.Ok,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Width = 400,
                    ShowInCenter = true,
                    Icon = MsBox.Avalonia.Enums.Icon.Error
                }).ShowAsPopupAsync(this);
                return;
            }



            // Update the tender order if confirmed using InvariantCulture for consistent decimal parsing
            if (decimal.TryParse(message, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal discountAmount))
            {
                TenderState.tenderOrder.PromoDiscountAmount = discountAmount;
                TenderState.tenderOrder.HasPromoDiscount = true;
                TenderState.tenderOrder.CalculateTotalAmount();

            }
            Close();

            // If promo applied successfully, confirm with the user
            //var confirmationBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            //{
            //    ContentHeader = $"Promo Code: {trimmedPromoCode}",
            //    ContentMessage = "Promo applied successfully. Do you want to confirm?",
            //    ButtonDefinitions = ButtonEnum.Ok,
            //    WindowStartupLocation = WindowStartupLocation.CenterOwner,
            //    CanResize = false,
            //    SizeToContent = SizeToContent.WidthAndHeight,
            //    Width = 400,
            //    ShowInCenter = true,
            //    Icon = MsBox.Avalonia.Enums.Icon.Info
            //});

            //var result = await confirmationBox.ShowAsPopupAsync(this);
            //if (result == ButtonResult.Ok)
            //{

            //    // Update the tender order if confirmed using InvariantCulture for consistent decimal parsing
            //    if (decimal.TryParse(message, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal discountAmount))
            //    {
            //        TenderState.tenderOrder.PromoDiscountAmount = discountAmount; 
            //        TenderState.tenderOrder.CalculateTotalAmount();

            //    }
            //    Close();
            //}
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
};