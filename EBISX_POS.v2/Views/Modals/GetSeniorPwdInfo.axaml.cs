using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using EBISX_POS.API.Services.DTO.Order;
using EBISX_POS.Models;
using EBISX_POS.Services;
using EBISX_POS.State;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MsBox.Avalonia.Dto;
using Avalonia.Interactivity;
using EBISX_POS.API.Services.DTO.Journal;

namespace EBISX_POS.Views
{
    public partial class GetSeniorPwdInfo : Window
    {
        private int _inputCount;
        private List<string> _selectedIDs;
        private bool _isPwdSelected;

        public GetSeniorPwdInfo(List<string> SelectedIDs, bool IsPwdSelected, int inputCount)
        {
            InitializeComponent();

            _inputCount = inputCount;
            _selectedIDs = SelectedIDs;
            _isPwdSelected = IsPwdSelected;

            CreateInputFields();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void CreateInputFields()
        {
            // Get the StackPanel from XAML
            var inputStackPanel = this.FindControl<StackPanel>("InputStackPanel");
            if (inputStackPanel == null)
            {
                throw new Exception("InputStackPanel not found in the XAML.");
            }

            // Loop and create each horizontal panel with two TextBoxes
            for (int i = 1; i <= _inputCount; i++)
            {
                // Create a horizontal panel to hold the TextBoxes
                var horizontalPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 5
                };

                // Create the Name TextBox using the provided style
                var textBoxName = new TextBox
                {
                    Width = 200,
                    Name = $"Name{i}",
                    Watermark = "Name",
                    UseFloatingWatermark = true,
                    Height = 40,
                    Margin = new Thickness(0, 0, 0, 40)
                };

                // Create the Osca Number TextBox using the provided style
                var textBoxOsca = new TextBox
                {
                    Width = 200,
                    Name = $"Osca{i}",
                    Watermark = "Osca Number",
                    UseFloatingWatermark = true,
                    Height = 40,
                    Margin = new Thickness(0, 0, 0, 40)
                };

                horizontalPanel.Children.Add(textBoxName);
                horizontalPanel.Children.Add(textBoxOsca);

                // Add the horizontal panel to the main StackPanel
                inputStackPanel.Children.Add(horizontalPanel);
            }
        }

        private async void SubmitButton_Click(object? sender, RoutedEventArgs e)
        {
            if (Submit_Button == null)
                Submit_Button = this.FindControl<Button>("Submit_Button");

            Submit_Button.IsEnabled = false;

            var inputStackPanel = this.FindControl<StackPanel>("InputStackPanel");
            if (inputStackPanel == null)
            {
                Submit_Button.IsEnabled = true;
                return;
            }

            List<string> names = new List<string>();
            List<string> oscaNumbers = new List<string>();
            List<PwdScInfoDTO> pwdScInfos = new List<PwdScInfoDTO>();


            // Loop through each horizontal panel in the InputStackPanel

            foreach (var child in inputStackPanel.Children)
            {
                if (child is StackPanel panel &&
                    panel.Children.Count >= 2 &&
                    panel.Children[0] is TextBox nameBox &&
                    panel.Children[1] is TextBox oscaBox)
                {
                    // Validate that both fields are filled; if not, exit the method.
                    if (string.IsNullOrWhiteSpace(nameBox.Text) || string.IsNullOrWhiteSpace(oscaBox.Text))
                    {
                        await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                        {
                            ContentHeader = "Required fields",
                            ContentMessage = "You must input all the fields",
                            ButtonDefinitions = ButtonEnum.Ok,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            CanResize = false,
                            SizeToContent = SizeToContent.WidthAndHeight,
                            Width = 400,
                            ShowInCenter = true,
                            Icon = MsBox.Avalonia.Enums.Icon.Error
                        }).ShowAsPopupAsync(this);
                        names.Clear();
                        oscaNumbers.Clear();
                        pwdScInfos.Clear();

                        Submit_Button.IsEnabled = true;
                        return;
                    }

                    names.Add(nameBox.Text.ToUpper());
                    oscaNumbers.Add(oscaBox.Text.ToUpper());
                    pwdScInfos.Add(new PwdScInfoDTO { Name = nameBox.Text.ToUpper(), OscaNum = oscaBox.Text.ToUpper() });
                }
            }

            TenderState.ElligiblePWDSCDiscount = new List<string>(names);

            // Combine the values into comma-separated strings
            string namesCombined = string.Join(", ", names);
            string oscaCombined = string.Join(", ", oscaNumbers);

            var orderService = App.Current.Services.GetRequiredService<OrderService>();

            await orderService.AddPwdScDiscount(new AddPwdScDiscountDTO()
            {
                EntryId = _selectedIDs,
                ManagerEmail = CashierState.ManagerEmail??"",
                PwdScCount = _inputCount,
                IsSeniorDisc = !_isPwdSelected,
                EligiblePwdScNames = namesCombined,
                OSCAIdsNum = oscaCombined,
                CashierEmail = CashierState.CashierEmail ?? "",
                PwdScInfo = pwdScInfos

            }); // Fetch the pending orders (grouped by EntryId) from the API.

            var ordersDto = await orderService.GetCurrentOrderItems();

            // If the items collection has empty items, exit.
            if (!ordersDto.Any())
            {
                Submit_Button.IsEnabled = true;
                return;
            }

            OrderState.CurrentOrder.Clear();

            names.Clear();
            oscaNumbers.Clear();

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
            CashierState.ManagerEmail = null;



            TenderState.tenderOrder.Reset();
            TenderState.tenderOrder.HasScDiscount = OrderState.CurrentOrder.Any(d => d.IsSeniorDiscounted);
            TenderState.tenderOrder.HasPwdDiscount = OrderState.CurrentOrder.Any(d => d.IsPwdDiscounted);

            // Select the PromoDiscountAmount from the first order that has a non-null value
            TenderState.tenderOrder.PromoDiscountAmount = OrderState.CurrentOrder
                .Where(d => d.PromoDiscountAmount != null)
                .Select(d => d.PromoDiscountAmount)
                .FirstOrDefault() ?? 0m;
            TenderState.tenderOrder.CalculateTotalAmount();
            Close();
            Submit_Button.IsEnabled = true;

        }
    }
}
