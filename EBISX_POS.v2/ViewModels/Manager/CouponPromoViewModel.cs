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
    public partial class CouponPromoViewModel : ObservableObject
    {
        private readonly IMenu _menuService;
        private readonly Window _window;

        [ObservableProperty]
        private ObservableCollection<CouponPromo> _couponPromos = new ObservableCollection<CouponPromo>();

        [ObservableProperty]
        private CouponPromo? _selectedCouponPromo;

        [ObservableProperty]
        private bool _isLoading;

        public CouponPromoViewModel(IMenu menuService, Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));

            _ = LoadCouponPromos();
        }

        private async Task LoadCouponPromos()
        {
            try
            {
                IsLoading = true;
                var couponPromos = await _menuService.GetAllCouponPromos();
                if (couponPromos != null)
                {
                    CouponPromos.Clear();
                    foreach (var couponPromo in couponPromos)
                    {
                        CouponPromos.Add(couponPromo);
                    }
                    OnPropertyChanged(nameof(CouponPromos));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading Coupon/Promos: {ex}");
                ShowError("Failed to load coupons/promos. Please try again.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task RemoveCouponPromo(CouponPromo couponPromo)
        {
            if (couponPromo == null) return;

            try
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    new MessageBoxStandardParams
                    {
                        ContentHeader = $"Coupon/Promo Deletion",
                        ContentMessage = "Press Ok to proceed deletion.",
                        ButtonDefinitions = ButtonEnum.OkCancel,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        CanResize = false,
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Width = 400,
                        ShowInCenter = true,
                        SystemDecorations = SystemDecorations.BorderOnly,
                        Icon = Icon.Warning
                    });

                var result = await box.ShowAsPopupAsync(_window);
                if (result == ButtonResult.Ok)
                {
                    var (isSuccess, message) = await _menuService.DeleteCouponPromo(couponPromo.Id, CashierState.ManagerEmail!);

                    if (isSuccess)
                    {
                        CouponPromos.Remove(couponPromo);
                        OnPropertyChanged(nameof(CouponPromos));
                        await ShowSuccess(message);
                        return;
                    }

                    ShowError(message);
                    return;
                }
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting coupon/promo: {ex}");
                ShowError("An error occurred while deleting the coupon/promo.");
            }
        }

        [RelayCommand]
        private void CloseWindow()
        {
            _window.Close();
        }

        [RelayCommand]
        private async Task AddNewCouponPromo()
        {
            var window = new AddCouponPromoWindow();
            window.DataContext = new AddCouponPromoViewModel(_menuService, window);
            var result = await window.ShowDialog<bool?>(_window);
            
            if (result == true)
            {
                Debug.WriteLine("New coupon/promo added successfully.");
                await LoadCouponPromos();
            }
        }

        public async Task EditCouponPromo(CouponPromo couponPromo)
        {
            var window = new AddCouponPromoWindow();
            window.DataContext = new AddCouponPromoViewModel(_menuService, window, couponPromo);
            var result = await window.ShowDialog<bool?>(_window);
            
            if (result == true)
            {
                Debug.WriteLine($"Coupon/Promo {couponPromo.Description} updated successfully.");
                await LoadCouponPromos();
            }
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