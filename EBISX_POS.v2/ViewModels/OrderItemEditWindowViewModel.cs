using CommunityToolkit.Mvvm.Input;
using EBISX_POS.Models;
using EBISX_POS.State;
using System.Diagnostics;
using System.Linq;

namespace EBISX_POS.ViewModels
{
    public partial class OrderItemEditWindowViewModel : ViewModelBase
    {
        // Store original quantity
        public decimal OriginalQuantity { get; }
        public OrderItemState OrderItem { get; }
        
        // Store original total price for comparison
        public decimal OriginalTotalPrice { get; }
        
        // Store the base item price (price per unit)
        public decimal BaseItemPrice { get; private set; }

        public OrderItemEditWindowViewModel(OrderItemState orderItem)
        {
            OrderItem = orderItem;
            OriginalQuantity = orderItem.Quantity;
            OriginalTotalPrice = orderItem.TotalPrice;
            
            // Calculate and store the base item price
            var firstSubOrder = orderItem.SubOrders.FirstOrDefault(s => s.IsFirstItem);
            BaseItemPrice = firstSubOrder?.ItemPrice ?? 0;
        }

        // Using object as parameter to safely convert the parameter value.
        [RelayCommand]
        private void EditQuantity(object delta)
        {
            decimal intDelta = 0;

            // If delta is an integer directly.
            if (delta is int directValue)
            {
                intDelta = directValue;
            }
            // If it's coming as a string (common in XAML bindings).
            else if (delta is string s && decimal.TryParse(s, out decimal parsedValue))
            {
                intDelta = parsedValue;
            }
            else
            {
                return;
            }

            // Ensure quantity does not fall below 1.
            if (OrderItem.Quantity + intDelta >= 1)
            {
                OrderItem.Quantity += intDelta;
                // Update total price when quantity changes
                UpdateTotalPriceFromQuantity();
            }

            TenderState.tenderOrder.CalculateTotalAmount();
        }

        /// <summary>
        /// Updates the total price when quantity changes (Total = BasePrice * Quantity)
        /// </summary>
        public void UpdateTotalPriceFromQuantity()
        {
            if (OrderItem.Quantity > 0)
            {
                OrderItem.TotalPrice = BaseItemPrice * OrderItem.Quantity;
                OrderItem.RefreshDisplaySubOrders();
            }
        }

        /// <summary>
        /// Updates the base item price when total price changes (BasePrice = Total / Quantity)
        /// </summary>
        public void UpdateBasePriceFromTotal()
        {
            if (OrderItem.Quantity > 0)
            {
                BaseItemPrice = OrderItem.TotalPrice / OrderItem.Quantity;
                
                // Update the item price in the first sub-order
                var firstSubOrder = OrderItem.SubOrders.FirstOrDefault(s => s.IsFirstItem);
                if (firstSubOrder != null)
                {
                    firstSubOrder.ItemPrice = BaseItemPrice;
                }
                
                OrderItem.RefreshDisplaySubOrders();
            }
        }

        /// <summary>
        /// Validates that the quantity is valid (greater than 0)
        /// </summary>
        public bool IsQuantityValid()
        {
            return OrderItem.Quantity > 0;
        }

        /// <summary>
        /// Validates that the total price is valid (greater than 0)
        /// </summary>
        public bool IsTotalPriceValid()
        {
            return OrderItem.TotalPrice > 0;
        }
    }
}
