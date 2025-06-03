using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using EBISX_POS.Models;
using EBISX_POS.Services;
using EBISX_POS.State;

namespace EBISX_POS.ViewModels
{
    public partial class OrderSummaryViewModel : ViewModelBase
    {
        // Expose the static current order item through a property.
        public OrderItemState CurrentOrderItem => OrderState.CurrentOrderItem;

        public ObservableCollection<OrderItemState> CurrentOrder { get; } = OrderState.CurrentOrder;

        [ObservableProperty]
        private string totalDue = "₱ 0.00";

        public OrderSummaryViewModel()
        {
            // Subscribe to changes of the static property.
            OrderState.StaticPropertyChanged += OnOrderStateStaticPropertyChanged;

            OrderState.CurrentOrderItem.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CurrentOrderItem));
                UpdateTotalDue();
            };

            OrderState.CurrentOrder.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CurrentOrder));
                UpdateTotalDue();
            };

            // Subscribe to TenderState changes
            TenderState.tenderOrder.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TenderOrder.AmountDue) ||
                    e.PropertyName == nameof(TenderOrder.TotalAmount) ||
                    e.PropertyName == nameof(TenderOrder.DiscountAmount))
                {
                    UpdateTotalDue();
                }
            };

            // Initial total amount update
            UpdateTotalDue();
        }

        private void UpdateTotalDue()
        {
            // No need to call CalculateTotalAmount here as it's already called in the discount windows
            TotalDue = $"₱ {TenderState.tenderOrder.AmountDue:N2}";
        }

        private void OnOrderStateStaticPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OrderState.CurrentOrderItem))
            {
                OnPropertyChanged(nameof(CurrentOrderItem));
                UpdateTotalDue();
            }
        }
    }
}

