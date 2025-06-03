using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EBISX_POS.Models;
using EBISX_POS.State;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace EBISX_POS.ViewModels
{
    public partial class TenderOrderViewModel : ViewModelBase
    {
        [ObservableProperty]
        private TenderOrder tenderCurrentOrder;

        [ObservableProperty]
        private string tenderInput = "";

        // Event to notify changes in other payments
        public event Action OnOtherPaymentsChanged;

        //public string TenderInputDisplay
        //{
        //    get
        //    {
        //        var otherPayment = TenderState.tenderOrder.OtherPayments?.Sum(p => p.Amount) ?? 0m;

        //        if (TenderInput.EndsWith("."))
        //        {
        //            string intPart = TenderInput.TrimEnd('.');
        //            if (decimal.TryParse(intPart, out decimal amt))
        //            {
        //                string formattedInt = (otherPayment + amt).ToString("N0");
        //                return $"₱ {formattedInt}.";
        //            }
        //            return $"₱ {TenderInput}";
        //        }
        //        else if (decimal.TryParse(TenderInput, out decimal amt2))
        //        {
        //            string format = TenderInput.Contains(".") ? "N2" : "N0";
        //            string formatted = (otherPayment + amt2).ToString(format);
        //            return $"₱ {formatted}";
        //        }
        //        return $"₱ {TenderInput}";
        //    }
        //}

        public string TenderInputDisplay
        {
            get
            {
                decimal total = TenderState.tenderOrder.TenderAmount;
                if (TenderInput.EndsWith("."))
                {
                    // Format using no decimals since the dot indicates an incomplete input.
                    return $"₱ {total.ToString("N0")}.";
                }
                // If TenderInput contains a decimal point, format with 2 decimal places.
                else if (TenderInput.Contains("."))
                {
                    return $"₱ {total.ToString("N2")}";
                }
                else
                {
                    // For integers (or when TenderInput is empty) use no decimal places.
                    return $"₱ {total.ToString("N0")}";
                }
            }
        }


        // This method will trigger when OtherPayments is updated from another viewmodel
        //public void HandleOtherPaymentsChanged()
        //{
        //    OnPropertyChanged(nameof(TenderInputDisplay));
        //}

        public TenderOrderViewModel()
        {
            TenderCurrentOrder = TenderState.tenderOrder;

            TenderCurrentOrder.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TenderCurrentOrder.TenderAmount))
                {
                    OnPropertyChanged(nameof(TenderInputDisplay));
                }
            };
        }

        [RelayCommand]
        private void AddPresetAmount(string content)
        {
            Debug.WriteLine($"Preset button clicked: {content}");
            if (decimal.TryParse(content, out decimal preset))
            {
                TenderCurrentOrder.CashTenderAmount += preset;

                // Update TenderInput to reflect the new amount with 2 decimal places.
                TenderInput = TenderCurrentOrder.CashTenderAmount.ToString("F2");
                // Optionally update any input string if you’re using one.
                OnPropertyChanged(nameof(TenderCurrentOrder));
                OnPropertyChanged(nameof(TenderInput));
                OnPropertyChanged(nameof(TenderInputDisplay));
                Debug.WriteLine($"New Tender Amount: {TenderCurrentOrder.CashTenderAmount}");
            }
            else
            {
                Debug.WriteLine("Failed to parse preset amount.");
            }
        }

        [RelayCommand]
        private void TenderButtonClick(string content)
        {
            Debug.WriteLine($"Button clicked: {content}");

            if (content == "CLEAR")
            {
                TenderInput = "";
                TenderCurrentOrder.CashTenderAmount = 0m;
                TenderState.tenderOrder.OtherPayments = null;
                OnPropertyChanged(nameof(TenderInput));
                OnPropertyChanged(nameof(TenderInputDisplay));
                OnPropertyChanged(nameof(TenderCurrentOrder));
                return;
            }

            // Append the content to the raw input string.
            if (content == ".")
            {
                if (!TenderInput.Contains("."))
                {
                    TenderInput += ".";
                }
            }
            else if (content == "00")
            {
                if (TenderInput.Contains("."))
                {
                    int index = TenderInput.IndexOf(".");
                    string decimals = TenderInput.Substring(index + 1);
                    int available = 2 - decimals.Length;
                    if (available > 0)
                    {
                        TenderInput += "00".Substring(0, available);
                    }
                }
                else
                {
                    TenderInput += "00";
                }
            }
            else if (int.TryParse(content, out int _))
            {
                if (TenderInput.Contains("."))
                {
                    int index = TenderInput.IndexOf(".");
                    string decimals = TenderInput.Substring(index + 1);
                    if (decimals.Length < 2)
                    {
                        TenderInput += content;
                    }
                }
                else
                {
                    TenderInput += content;
                }
            }

            Debug.WriteLine($"Tender input string: {TenderInput}");

            if (decimal.TryParse(TenderInput, out decimal newAmount))
            {
                newAmount = Math.Round(newAmount, 2);
                TenderCurrentOrder.CashTenderAmount = newAmount;
            }

            // Notify the UI that both the raw input and its display have changed.
            OnPropertyChanged(nameof(TenderInput));
            OnPropertyChanged(nameof(TenderInputDisplay));
            OnPropertyChanged(nameof(TenderCurrentOrder));

            Debug.WriteLine($"Tender amount updated to: {TenderCurrentOrder.CashTenderAmount}");
        }
    }
}
