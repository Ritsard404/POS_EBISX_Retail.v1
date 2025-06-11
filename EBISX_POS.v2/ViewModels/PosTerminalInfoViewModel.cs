using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EBISX_POS.API.Models;
using EBISX_POS.API.Services.Interfaces;

namespace EBISX_POS.ViewModels
{
    public partial class PosTerminalInfoViewModel : ObservableValidator
    {
        private readonly IEbisxAPI _apiService;
        private readonly Window _window;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "POS Serial Number is required")]
        private string _posSerialNumber = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "MIN Number is required")]
        private string _minNumber = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Accreditation Number is required")]
        private string _accreditationNumber = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "PTU Number is required")]
        private string _ptuNumber = string.Empty;

        [ObservableProperty]
        private DateTimeOffset? _dateIssued = DateTimeOffset.Now;

        [ObservableProperty]
        private DateTimeOffset? _validUntil;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Registered Name is required")]
        private string _registeredName = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Operated By is required")]
        private string _operatedBy = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Address is required")]
        private string _address = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "VAT TIN Number is required")]
        private string _vatTinNumber = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Store Code is required")]
        private string _storeCode = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        public PosTerminalInfoViewModel(IEbisxAPI apiService, Window window)
        {
            _apiService = apiService;
            _window = window;
            LoadPosTerminalInfo();
        }

        partial void OnDateIssuedChanged(DateTimeOffset? value)
        {
            if (value.HasValue)
            {
                // Auto-calculate ValidUntil date (5 years from DateIssued)
                ValidUntil = value.Value.AddYears(5);
            }
        }

        [RelayCommand]
        private async Task CloseWindow()
        {
            _window.Close();
        }

        [RelayCommand]
        private async Task SavePosTerminalInfo()
        {
            try
            {
                ValidateAllProperties();
                if (HasErrors)
                {
                    StatusMessage = "Please fix validation errors before saving.";
                    return;
                }

                if (!DateIssued.HasValue || !ValidUntil.HasValue)
                {
                    StatusMessage = "Date Issued and Valid Until dates are required.";
                    return;
                }

                IsLoading = true;
                var terminalInfo = new PosTerminalInfo
                {
                    PosSerialNumber = PosSerialNumber,
                    MinNumber = MinNumber,
                    AccreditationNumber = AccreditationNumber,
                    PtuNumber = PtuNumber,
                    DateIssued = DateIssued.Value.DateTime,
                    ValidUntil = ValidUntil.Value.DateTime,
                    RegisteredName = RegisteredName,
                    OperatedBy = OperatedBy,
                    Address = Address,
                    VatTinNumber = VatTinNumber,
                    StoreCode = StoreCode
                };

                var (isSuccess, message) = await _apiService.SetPosTerminalInfo(terminalInfo);
                StatusMessage = message;
                if (isSuccess)
                {
                    _window.Close();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving terminal info: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void LoadPosTerminalInfo()
        {
            try
            {
                IsLoading = true;
                var terminalInfo = await _apiService.PosTerminalInfo();
                if (terminalInfo != null)
                {
                    PosSerialNumber = terminalInfo.PosSerialNumber;
                    MinNumber = terminalInfo.MinNumber;
                    AccreditationNumber = terminalInfo.AccreditationNumber;
                    PtuNumber = terminalInfo.PtuNumber;
                    DateIssued = new DateTimeOffset(terminalInfo.DateIssued);
                    ValidUntil = new DateTimeOffset(terminalInfo.ValidUntil);
                    RegisteredName = terminalInfo.RegisteredName;
                    OperatedBy = terminalInfo.OperatedBy;
                    Address = terminalInfo.Address;
                    VatTinNumber = terminalInfo.VatTinNumber;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading terminal info: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
} 