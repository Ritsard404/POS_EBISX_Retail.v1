using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace EBISX_POS.Models
{
    public partial class OrderItemState : ObservableObject
    {
        private static long _counter = 0;
        public string ID { get; set; }  // ID is set in constructor, no change notification needed

        [ObservableProperty]
        private decimal quantity;

        [ObservableProperty]
        private string? orderType;

        [ObservableProperty]
        private bool hasCurrentOrder;

        [ObservableProperty]
        private decimal totalPrice;

        //[ObservableProperty]
        //private decimal totalDiscountPrice;

        public decimal TotalDiscountPrice { get; set; }
        public bool HasDiscount { get; set; }
        public bool HasPwdScDiscount { get; set; }
        public bool IsEnableEdit { get; set; } = true;
        public bool IsPwdDiscounted { get; set; } = false;
        public bool IsSeniorDiscounted { get; set; } = false;
        public decimal? PromoDiscountAmount { get; set; }
        public string? CouponCode { get; set; }

        public bool HasDrinks { get; set; } = false;
        public bool HasAddOns { get; set; } = false;
        public bool IsVatExempt { get; set; }

        // Using ObservableCollection so UI is notified on add/remove.
        [ObservableProperty]
        private ObservableCollection<SubOrderItem> subOrders = new ObservableCollection<SubOrderItem>();

        // Computed property: UI must be notified manually if you need it to update dynamically.
        public ObservableCollection<SubOrderItem> DisplaySubOrders =>
            new ObservableCollection<SubOrderItem>(subOrders
                .Select((s, index) => new SubOrderItem
                {
                    MenuId = s.MenuId,
                    DrinkId = s.DrinkId,
                    AddOnId = s.AddOnId,
                    Name = s.Name,
                    ItemPrice = s.ItemPrice,
                    Size = s.Size,
                    IsFirstItem = index == 0, // True for the first item
                    Quantity = Quantity,  // Only show Quantity for the first item
                    IsOtherDisc = s.IsOtherDisc
                }));

        public OrderItemState()
        {
            RegenerateID();
            subOrders.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(DisplaySubOrders));
                UpdateTotalPrice();
            };
        }

        // Call this to generate a new unique ID.
        public void RegenerateID()
        {
            long ticks = DateTime.UtcNow.Ticks; // high resolution
            long count = Interlocked.Increment(ref _counter);
            ID = $"{ticks}-{Guid.NewGuid().ToString()}-{count}";
        }

        partial void OnQuantityChanged(decimal oldValue, decimal newValue)
        {
            OnPropertyChanged(nameof(DisplaySubOrders));
            OnPropertyChanged(nameof(Quantity));
            UpdateTotalPrice();
            //UpdateTotalDiscountPrice();
        }

        public void RefreshDisplaySubOrders(bool regenerateId = false)
        {
            if (regenerateId)
            {
                RegenerateID();
            }

            OnPropertyChanged(nameof(DisplaySubOrders));
            UpdateTotalPrice();
            UpdateHasCurrentOrder();
        }
        private void UpdateHasCurrentOrder()
        {
            HasCurrentOrder = SubOrders.Any();
        }
        private void UpdateTotalPrice()
        {
            TotalPrice = DisplaySubOrders
            .Where(i => !(i.AddOnId == null && i.MenuId == null && i.DrinkId == null))
            .Sum(p => p.ItemSubTotal);

        }

    }

    public class SubOrderItem
    {
        public int? MenuId { get; set; }
        public int? DrinkId { get; set; }
        public int? AddOnId { get; set; }

        public string Name { get; set; } = string.Empty;
        public decimal ItemPrice { get; set; } = 0;
        public decimal ItemSubTotal => AddOnId == null && MenuId == null && DrinkId == null
            ? ItemPrice :
            ItemPrice * Quantity;
        public string? Size { get; set; }

        public bool IsFirstItem { get; set; } = false;
        public bool IsOtherDisc { get; set; }
        public decimal Quantity { get; set; } = 0; // Store Quantity for first item

        public string DisplayName
        {
            get
            {
                return Name;
            }
        }


        private readonly CultureInfo PesoCulture = new CultureInfo("en-PH");

        // Opacity Property (replaces a converter)
        public double Opacity => IsFirstItem ? 1.0 : 0.0;

        public bool IsUpgradeMeal => ItemPrice > 0;

        public string ItemPriceString
        {
            get
            {
                // 0) if the item’s price (or its subtotal) is zero, show nothing:
                if (ItemPrice == 0m || ItemSubTotal == 0m)
                    return string.Empty;

                // 1) explicit “other” → percent
                if (IsOtherDisc)
                    return $"{ItemPrice:0}%";

                // Prepare the currency string (e.g. “₱1,234.00”)
                var currency = ItemSubTotal.ToString("C", PesoCulture);

                // 2) pure-discount & not “other” → negative ₱ (only if not zero)
                //    (Here “pure discount” means no MenuId, no DrinkId, no AddOnId)
                if (MenuId == null && DrinkId == null && AddOnId == null)
                    return $"- {currency}";

                // 3) first item → positive ₱ (no sign prefix needed; ToString("C") already includes “₱”)
                if (IsFirstItem)
                    return currency;

                // 4) upgrade → +₱ (only if ItemPrice > 0)
                if (IsUpgradeMeal)
                    return $"+ {currency}";

                // 5) otherwise (i.e. a non-first, non-upgrade) → – ₱
                return $"- {currency}";
            }
        }

    }
}
