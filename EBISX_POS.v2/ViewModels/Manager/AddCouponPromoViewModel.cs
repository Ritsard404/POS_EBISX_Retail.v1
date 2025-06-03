using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using EBISX_POS.API.Models;
using EBISX_POS.API.Services.Interfaces;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using System.Linq;

namespace EBISX_POS.ViewModels.Manager
{
    public partial class AddCouponPromoViewModel : ObservableObject
    {
        private readonly IMenu _menuService;
        private readonly Window _window;

        [ObservableProperty]
        private bool _isEditMode;
        private readonly CouponPromo? _existingCouponPromo;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string? _promoCode;

        [ObservableProperty]
        private string? _couponCode;

        [ObservableProperty]
        private decimal _promoAmount;

        [ObservableProperty]
        private int? _couponItemQuantity;

        [ObservableProperty]
        private bool _isAvailable = true;

        [ObservableProperty]
        private DateTimeOffset _expirationTime = DateTimeOffset.UtcNow.AddDays(30);

        [ObservableProperty]
        private ObservableCollection<API.Models.Menu> _selectedMenus = new();

        [ObservableProperty]
        private ObservableCollection<API.Models.Menu> _availableMenus = new();

        [ObservableProperty]
        private bool _isLoading;

        public AddCouponPromoViewModel(IMenu menuService, Window window, CouponPromo? existingCouponPromo = null)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
            IsEditMode = existingCouponPromo != null;
            _existingCouponPromo = existingCouponPromo;

            if (IsEditMode && _existingCouponPromo != null)
            {
                Description = _existingCouponPromo.Description;
                PromoCode = _existingCouponPromo.PromoCode;
                CouponCode = _existingCouponPromo.CouponCode;
                PromoAmount = _existingCouponPromo.PromoAmount ?? 0;
                CouponItemQuantity = _existingCouponPromo.CouponItemQuantity;
                IsAvailable = _existingCouponPromo.IsAvailable;
                ExpirationTime = _existingCouponPromo.ExpirationTime ?? DateTimeOffset.UtcNow.AddDays(30);
                if (_existingCouponPromo.CouponMenus != null)
                {
                    foreach (var menu in _existingCouponPromo.CouponMenus)
                    {
                        SelectedMenus.Add(menu);
                    }
                }
            }

            _ = LoadMenus();
        }

        private async Task LoadMenus()
        {
            try
            {
                IsLoading = true;
                var menus = await _menuService.GetAllMenus();
                if (menus != null)
                {
                    AvailableMenus.Clear();
                    foreach (var menu in menus.Where(m => !SelectedMenus.Contains(m)))
                    {
                        AvailableMenus.Add(menu);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading menus: {ex}");
                ShowError("Failed to load menus. Please try again.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void AddMenu(API.Models.Menu menu)
        {
            if (menu != null && !SelectedMenus.Contains(menu))
            {
                SelectedMenus.Add(menu);
                AvailableMenus.Remove(menu);
            }
        }

        [RelayCommand]
        private void RemoveMenu(API.Models.Menu menu)
        {

            if (menu != null && SelectedMenus.Contains(menu))
            {
                SelectedMenus.Remove(menu);
                AvailableMenus.Add(menu);
            }
        }

        [RelayCommand]
        private async Task SaveCouponPromo()
        {
            if (!ValidateInput())
            {
                return;
            }

            try
            {
                IsLoading = true;

                var couponPromo = new CouponPromo
                {
                    Description = Description,
                    PromoCode = PromoCode,
                    CouponCode = CouponCode,
                    PromoAmount = PromoAmount,
                    CouponItemQuantity = CouponItemQuantity,
                    IsAvailable = IsAvailable,
                    ExpirationTime = ExpirationTime,
                    CouponMenus = SelectedMenus.ToList()
                };

                if (_isEditMode && _existingCouponPromo != null)
                {
                    couponPromo.Id = _existingCouponPromo.Id;
                    var (isSuccess, message) = await _menuService.UpdateCouponPromo(couponPromo, CashierState.ManagerEmail!);
                    if (isSuccess)
                    {
                        await ShowSuccess(message);
                        _window.Close(true);
                        return;
                    }
                    ShowError(message);
                }
                else
                {
                    var (isSuccess, message, _) = await _menuService.AddCouponPromo(couponPromo, CashierState.ManagerEmail!);
                    if (isSuccess)
                    {
                        await ShowSuccess(message);
                        _window.Close(true);
                        return;
                    }
                    ShowError(message);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving coupon/promo: {ex}");
                ShowError("An error occurred while saving the coupon/promo.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _window.Close(false);
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(Description))
            {
                ShowError("Description is required.");
                return false;
            }

            if (PromoAmount <= 0)
            {
                ShowError("Promo amount must be greater than 0.");
                return false;
            }

            if (ExpirationTime <= DateTimeOffset.UtcNow)
            {
                ShowError("Expiration time must be in the future.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(PromoCode) && string.IsNullOrWhiteSpace(CouponCode))
            {
                ShowError("Either Promo Code or Coupon Code must be provided.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(PromoCode) && !string.IsNullOrWhiteSpace(CouponCode))
            {
                ShowError("Please provide either a Promo Code or a Coupon Code, but not both.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(PromoCode) && (SelectedMenus.Count > 0 || CouponItemQuantity > 0))
            {
                ShowError(
                    "A promo code cannot be applied when menu items or coupon items are already selected. " +
                    "Please remove those items before using a promo code."
                ); return false;
            }

            if (!string.IsNullOrWhiteSpace(CouponCode) && SelectedMenus.Count == 0)
            {
                ShowError("At least one menu item must be selected for a coupon.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(CouponCode) && (!CouponItemQuantity.HasValue || CouponItemQuantity.Value <= 0))
            {
                ShowError("Coupon item quantity must be greater than 0.");
                return false;
            }

            return true;
        }

        private void ShowError(string message)
        {
            var msgBox = MessageBoxManager
                .GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = "Error",
                    ContentMessage = message,
                    Icon = Icon.Error
                });

            msgBox.ShowAsPopupAsync(_window);
        }

        private async Task ShowSuccess(string message)
        {
            var successBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ButtonDefinitions = ButtonEnum.Ok,
                ContentTitle = "Success",
                ContentMessage = message,
                Icon = Icon.Success
            });
            await successBox.ShowAsPopupAsync(_window);
        }
    }
}