using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EBISX_POS.API.Models;
using EBISX_POS.API.Services.Interfaces;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace EBISX_POS.ViewModels.Manager
{
    /// <summary>
    /// ViewModel for managing drink and add-on types
    /// </summary>
    public partial class DrinkAndAddOnTypeViewModel : ObservableObject
    {
        private readonly IMenu _menuService;
        private readonly Window _window;

        [ObservableProperty]
        private ObservableCollection<AddOnType> _addOnTypes = new();

        [ObservableProperty]
        private ObservableCollection<DrinkType> _drinkTypes = new();

        [ObservableProperty]
        private AddOnType? _selectedAddOnType;

        [ObservableProperty]
        private DrinkType? _selectedDrinkType;

        [ObservableProperty]
        private bool _isLoading;

        public DrinkAndAddOnTypeViewModel(IMenu menu, Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _menuService = menu ?? throw new ArgumentNullException(nameof(menu));

            _ = LoadAddOnsAndDrinks();
        }

        /// <summary>
        /// Loads both add-on types and drink types from the service
        /// </summary>
        private async Task LoadAddOnsAndDrinks()
        {
            try
            {
                IsLoading = true;
                await Task.WhenAll(
                    LoadAddOnTypes(),
                    LoadDrinkTypes()
                );
            }
            catch (Exception ex)
            {
                await ShowError($"Error loading data: {ex.Message}");
                Debug.WriteLine($"Error loading Add ons and DrinkTypes: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadAddOnTypes()
        {
            var addOnTypes = await _menuService.GetAddOnTypes();
            if (addOnTypes != null)
            {
                AddOnTypes.Clear();
                foreach (var addOn in addOnTypes)
                {
                    AddOnTypes.Add(addOn);
                }
            }
        }

        private async Task LoadDrinkTypes()
        {
            var drinkTypes = await _menuService.GetDrinkTypes();
            if (drinkTypes != null)
            {
                DrinkTypes.Clear();
                foreach (var drinkType in drinkTypes)
                {
                    DrinkTypes.Add(drinkType);
                }
            }
        }

        /// <summary>
        /// Generic method to handle type updates (both AddOn and Drink types)
        /// </summary>
        private async Task<bool> UpdateType<T>(T type, Func<T, string, Task<(bool, string)>> updateAction)
        {
            if (type == null) return false;

            try
            {
                var (isSuccess, message) = await updateAction(type, CashierState.ManagerEmail!);

                if (isSuccess)
                {
                    await LoadAddOnsAndDrinks();
                    return true;
                }
                await ShowError(message);
                return false;
            }
            catch (Exception ex)
            {
                await ShowError($"Error updating type: {ex.Message}");
                Debug.WriteLine($"Error saving changes: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Generic method to handle type deletion (both AddOn and Drink types)
        /// </summary>
        private async Task<bool> DeleteType<T>(T type, Func<int, string, Task<(bool, string)>> deleteAction)
        {
            if (type == null) return false;

            try
            {
                var id = type switch
                {
                    AddOnType addOn => addOn.Id,
                    DrinkType drink => drink.Id,
                    _ => throw new ArgumentException("Unsupported type")
                };

                var (isSuccess, message) = await deleteAction(id, CashierState.ManagerEmail!);

                if (isSuccess)
                {
                    await LoadAddOnsAndDrinks();
                    await ShowSuccess(message);
                    return true;
                }
                await ShowError(message);
                return false;
            }
            catch (Exception ex)
            {
                await ShowError($"Error deleting type: {ex.Message}");
                Debug.WriteLine($"Error deleting type: {ex}");
                return false;
            }
        }

        [RelayCommand]
        private async Task NewAddOnType()
        {
            try
            {
                var window = new AddDrinkAndAddOnTypeWindow();
                window.DataContext = new AddDrinkAndAddOnTypeViewModel(_menuService, window, false);
                await window.ShowDialog(_window);
                await LoadAddOnsAndDrinks();
            }
            catch (Exception ex)
            {
                await ShowError($"Error opening add-on type window: {ex.Message}");
                Debug.WriteLine($"Error opening add-on type window: {ex}");
            }
        }

        [RelayCommand]
        private async Task NewDrinkType()
        {
            try
            {
                var window = new AddDrinkAndAddOnTypeWindow();
                window.DataContext = new AddDrinkAndAddOnTypeViewModel(_menuService, window, true);
                await window.ShowDialog(_window);
                await LoadAddOnsAndDrinks();
            }
            catch (Exception ex)
            {
                await ShowError($"Error opening drink type window: {ex.Message}");
                Debug.WriteLine($"Error opening drink type window: {ex}");
            }
        }
        public async Task RemoveAddOnType(AddOnType addOn)
        {
            if (addOn != null)
            {
                await DeleteType(addOn, _menuService.DeleteAddOnType);
            }
        }

        public async Task RemoveDrinkType(DrinkType drinkType)
        {
            if (drinkType != null)
            {
                await DeleteType(drinkType, _menuService.DeleteDrinkType);
            }
        }

        [RelayCommand]
        private void CloseWindow()
        {
            _window.Close();
        }

        partial void OnSelectedDrinkTypeChanged(DrinkType? value)
        {
            if (string.IsNullOrWhiteSpace(value?.DrinkTypeName))
            {
                ShowError("Type name required.");
                return;
            }
            if (value != null)
            {
                _ = UpdateType(value, _menuService.UpdateDrinkType);
            }
        }

        partial void OnSelectedAddOnTypeChanged(AddOnType? value)
        {
            if (string.IsNullOrWhiteSpace(value?.AddOnTypeName))
            {
                ShowError("Type name required.");
                return;
            }

            if (value != null)
            {
                _ = UpdateType(value, _menuService.UpdateAddOnType);
            }
        }

        private async Task ShowError(string message)
        {
            var msgBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ButtonDefinitions = ButtonEnum.Ok,
                ContentTitle = "Error",
                ContentMessage = message,
                Icon = Icon.Error
            });

            await msgBox.ShowAsPopupAsync(_window);
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