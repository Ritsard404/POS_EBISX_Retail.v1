using CommunityToolkit.Mvvm.ComponentModel;
using EBISX_POS.API.Services.DTO.Payment;
using EBISX_POS.State;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace EBISX_POS.Models
{
    public partial class TenderOrder : ObservableObject
    {
        [ObservableProperty] private decimal vatSales = 0m;
        [ObservableProperty] private decimal vatAmount = 0m;
        [ObservableProperty] private decimal vatExemptSales = 0m;

        [ObservableProperty] private decimal totalAmount = 0m;
        [ObservableProperty] private decimal tenderAmount = 0m;
        [ObservableProperty] private decimal cashTenderAmount = 0m;
        [ObservableProperty] private decimal discountAmount = 0m;
        [ObservableProperty] private int discountPercent = 0;
        [ObservableProperty] private decimal promoDiscountAmount = 0m;
        [ObservableProperty] private decimal promoDiscountPercent = 0m;
        [ObservableProperty] private string promoDiscountName = string.Empty;
        [ObservableProperty] private decimal changeAmount = 0m;
        [ObservableProperty] private decimal amountDue = 0m;

        [ObservableProperty] private bool hasPromoDiscount;
        [ObservableProperty] private bool hasCouponDiscount;
        [ObservableProperty] private bool hasPwdDiscount;
        [ObservableProperty] private bool hasScDiscount;
        [ObservableProperty] private bool hasOrderDiscount;
        [ObservableProperty] private bool hasOtherDiscount;

        [ObservableProperty] private bool hasOtherPayments;

        [ObservableProperty]
        private ObservableCollection<AddAlternativePaymentsDTO>? otherPayments;
        public string OrderType { get; set; } = "";

        // Trigger recalculations when key properties change
        partial void OnTotalAmountChanged(decimal oldValue, decimal newValue) => UpdateComputedValues();
        partial void OnTenderAmountChanged(decimal oldValue, decimal newValue) => UpdateComputedValues();
        partial void OnCashTenderAmountChanged(decimal oldValue, decimal newValue) => UpdateComputedValues();
        partial void OnDiscountAmountChanged(decimal oldValue, decimal newValue) => UpdateComputedValues();
        partial void OnDiscountPercentChanged(int oldValue, int newValue) => UpdateComputedValues();
        partial void OnPromoDiscountAmountChanged(decimal oldValue, decimal newValue) => UpdateComputedValues();
        partial void OnPromoDiscountPercentChanged(decimal oldValue, decimal newValue) => UpdateComputedValues();
        partial void OnHasPwdDiscountChanged(bool oldValue, bool newValue) => UpdateComputedValues();
        partial void OnHasScDiscountChanged(bool oldValue, bool newValue) => UpdateComputedValues();
        partial void OnHasPromoDiscountChanged(bool oldValue, bool newValue) => UpdateComputedValues();
        partial void OnHasCouponDiscountChanged(bool oldValue, bool newValue) => UpdateComputedValues();
        partial void OnOtherPaymentsChanged(ObservableCollection<AddAlternativePaymentsDTO>? oldValue, ObservableCollection<AddAlternativePaymentsDTO>? newValue) => UpdateComputedValues();

        public void Reset()
        {
            TotalAmount = TenderAmount = CashTenderAmount = DiscountAmount = PromoDiscountAmount = PromoDiscountPercent = 0m;
            HasPromoDiscount = HasScDiscount = HasPwdDiscount = HasOrderDiscount = false;
            OtherPayments = null;
            PromoDiscountName = string.Empty;
            UpdateComputedValues();
        }

        public bool CalculateTotalAmount()
        {
            var oldTotalAmount = TotalAmount;
            var oldAmountDue = AmountDue;

            TotalAmount = OrderState.CurrentOrder
                .Sum(orderItem => orderItem.TotalPrice);

            VatExemptSales = (HasScDiscount || HasPwdDiscount) ? DiscountAmount : 0m;

            VatSales = !HasOrderDiscount ? TotalAmount / 1.12m : OrderState.CurrentOrder
                .Where(d => !d.IsPwdDiscounted && !d.IsSeniorDiscounted)
                .Sum(orderItem => orderItem.TotalPrice) / 1.12m;

            VatAmount = (!HasOrderDiscount ? TotalAmount - (TotalAmount / 1.12m) : VatSales - (VatSales / 1.12m));

            UpdateComputedValues();

            // Explicitly notify changes if values have changed
            if (oldTotalAmount != TotalAmount)
            {
                OnPropertyChanged(nameof(TotalAmount));
            }
            if (oldAmountDue != AmountDue)
            {
                OnPropertyChanged(nameof(AmountDue));
            }

            return TotalAmount <= 0;
        }

        private void UpdateComputedValues()
        {
            var oldAmountDue = AmountDue;
            var oldDiscountAmount = DiscountAmount;

            HasPromoDiscount = PromoDiscountAmount > 0 || PromoDiscountPercent > 0;
            HasCouponDiscount = OrderState.CurrentOrder
                .Any(orderItem => orderItem.CouponCode != null);
            HasOtherDiscount = OrderState.CurrentOrder
                .Any(orderItem => orderItem.HasDiscount);
            HasOrderDiscount = HasPromoDiscount || HasScDiscount || HasPwdDiscount || HasCouponDiscount || HasOtherDiscount;
            HasOtherPayments = OtherPayments != null && OtherPayments.Count > 0;

            if (HasPromoDiscount)
            {
                DiscountAmount = PromoDiscountAmount;
            }
            else if (HasPwdDiscount || HasScDiscount)
            {
                DiscountAmount = OrderState.CurrentOrder
                        .Sum(orderItem => orderItem.TotalDiscountPrice);
            }
            else if (HasOtherDiscount)
            {
                DiscountPercent = OrderState.CurrentOrder
                    .Where(orderItem => orderItem.HasDiscount)
                    .SelectMany(orderItem => orderItem.SubOrders)
                    .Where(sub => sub.IsOtherDisc)
                    .Sum(sub => (int)sub.ItemPrice);

                var baseAmt = TotalAmount >= 500m ? 500m : TotalAmount;
                DiscountAmount = Math.Round(baseAmt * DiscountPercent / 100m, 2);
            }
            else
            {
                DiscountAmount = 0m;
            }

            var otherPaymentsTotal = OtherPayments?.Sum(payment => payment.Amount) ?? 0m;
            TenderAmount = CashTenderAmount + otherPaymentsTotal;

            if (PromoDiscountAmount >= TotalAmount)
            {
                AmountDue = 0;
                ChangeAmount = 0;
            }
            else
            {
                AmountDue = TotalAmount - DiscountAmount;
                ChangeAmount = TenderAmount - AmountDue;
            }

            // Explicitly notify changes if values have changed
            if (oldAmountDue != AmountDue)
            {
                OnPropertyChanged(nameof(AmountDue));
            }
            if (oldDiscountAmount != DiscountAmount)
            {
                OnPropertyChanged(nameof(DiscountAmount));
            }
        }

        public void ApplyExactAmount()
        {
            // clear any partial tenders or alt payments
            CashTenderAmount = 0m;
            OtherPayments = null;

            // force exact tender
            CashTenderAmount = AmountDue;
            // TenderAmount is calculated in UpdateComputedValues()
            UpdateComputedValues();
        }
    }
}
