using System;
using System.Threading.Tasks;
using EBISX_POS.API.Models;
using EBISX_POS.API.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using Avalonia.Controls;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using Tmds.DBus.Protocol;
using Avalonia.Controls.Shapes;

namespace EBISX_POS.ViewModels.Manager
{
    public partial class AddDrinkAndAddOnTypeViewModel : ViewModelBase
    {
        private readonly IMenu _menuService;
        private readonly Window _window;

        [ObservableProperty]
        private bool _isDrink;

        public string DialogTitle => IsDrink ? "Add New Drink Type" : "Add New Add-On Type";
        public string InputWatermark => IsDrink ? "Drink Type" : "Add-On Type";
        public string InputText
        {
            get => IsDrink
                ? DrinkTypeName
                : AddOnTypeName;
            set
            {
                if (IsDrink)
                    DrinkTypeName = value;
                else
                    AddOnTypeName = value;
            }
        }


        [ObservableProperty]
        private string _drinkTypeName = string.Empty;
        [ObservableProperty]
        private string _addOnTypeName = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        public AddDrinkAndAddOnTypeViewModel(IMenu menuService, Window window, bool isDrink)
        {
            _menuService = menuService;
            _window = window;
            _isDrink = isDrink;
        }

        [RelayCommand]
        private async Task AddMenuType()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;

                if (IsDrink)
                {
                    if (string.IsNullOrWhiteSpace(DrinkTypeName))
                    {
                        await ShowMessage("Error", "All fields are required.", Icon.Error);
                        return;
                    }

                    var (isSuccess, message, _) = await _menuService.AddDrinkType(
                        new DrinkType { DrinkTypeName = DrinkTypeName },
                        CashierState.ManagerEmail!);

                    await ShowMessage(isSuccess ? "Success" : "Error", message,
                        isSuccess ? Icon.Success : Icon.Error);

                    if (isSuccess)
                        _window.Close();
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(AddOnTypeName))
                    {
                        await ShowMessage("Error", "All fields are required.", Icon.Error);
                        return;
                    }

                    var (isSuccess, message, _) = await _menuService.AddAddOnType(
                        new AddOnType { AddOnTypeName = AddOnTypeName },
                        CashierState.ManagerEmail!);

                    await ShowMessage(isSuccess ? "Success" : "Error", message,
                        isSuccess ? Icon.Success : Icon.Error);

                    if (isSuccess)
                        _window.Close();
                }
            }
            catch (Exception ex)
            {
                DebugWrite(ex);
                await ShowMessage("Error", ex.Message, Icon.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void DebugWrite(Exception ex)
        {
#if DEBUG
            Console.WriteLine($"❌ AddMenuType exception: {ex}");
#endif
        }

        [RelayCommand]
        private void Cancel() => _window.Close();


        private async Task ShowMessage(string title, string message, Icon icon)
        {
            var msgBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ButtonDefinitions = ButtonEnum.Ok,
                ContentTitle = title,
                ContentMessage = message,
                Icon = icon
            });
            await msgBox.ShowAsPopupAsync(_window);
        }
    }
}