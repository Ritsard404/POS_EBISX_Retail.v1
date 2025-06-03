using Avalonia.Controls;
using Avalonia.Interactivity;
using EBISX_POS.API.Services.Interfaces;
using EBISX_POS.Services;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System.ComponentModel;
using EBISX_POS.State;
using System.Collections.Generic;
using EBISX_POS.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;

namespace EBISX_POS.Views
{

    public partial class AddSeniorPwdDiscountWindow : Window, INotifyPropertyChanged
    {
        private string _managerEmail;

        private bool _isPwdSelected = false;

        // Public property for PWD selection.
        public bool IsPwdSelected
        {
            get => _isPwdSelected;
            set
            {
                if (_isPwdSelected != value)
                {
                    _isPwdSelected = value;
                    OnPropertyChanged(nameof(IsPwdSelected));
                    // Also update the complementary property.
                    OnPropertyChanged(nameof(IsSeniorSelected));
                }
            }
        }

        // Derived property: true when Senior is selected (i.e. when IsPwdSelected is false).
        public bool IsSeniorSelected
        {
            get => !_isPwdSelected;
            set
            {
                // When binding sets IsSeniorSelected, update IsPwdSelected accordingly.
                // If Senior is set to true, then PWD is false, and vice versa.
                if (value != (!_isPwdSelected))
                {
                    IsPwdSelected = !value;
                }
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public AddSeniorPwdDiscountWindow(string managerEmail)
        {
            InitializeComponent();
            DataContext = this;
            _managerEmail = managerEmail;
        }

        private async void SaveButton_Click(object? sender, RoutedEventArgs e)
        {
            LoadingOverlay.IsVisible = true;
            var orderService = App.Current.Services.GetRequiredService<IOrder>();

            var nameText = Name.Text?.Trim();
            var oscaNum = OscaNumTextBox.Text?.Trim();

            // both fields required
            if (string.IsNullOrWhiteSpace(nameText) || string.IsNullOrWhiteSpace(oscaNum))
            {
                await MessageBoxManager
                    .GetMessageBoxStandard("Invalid Input", "Both Name and Discount are required.", ButtonEnum.Ok)
                    .ShowAsPopupAsync(this);
                LoadingOverlay.IsVisible = false;
                Close();
                return;
            }

            await orderService.AddSinglePwdScDiscount(isPWD: IsPwdSelected, oscaNum: oscaNum, elligibleName: nameText, _managerEmail, cashierEmail: CashierState.CashierEmail);


            var ordersDto = await orderService.GetCurrentOrderItems(cashierEmail: CashierState.CashierEmail);

            if (!ordersDto.Any())
            {
                SaveButton.IsEnabled = true;
                Close();
                return;
            }

            OrderState.CurrentOrder.Clear();

            TenderState.ElligiblePWDSCDiscount = new List<string>
            {
                nameText
            };

            Name.Clear();
            OscaNumTextBox.Clear();

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
};
