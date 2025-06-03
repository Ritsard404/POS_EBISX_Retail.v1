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
    public partial class AddCategoryViewModel : ViewModelBase
    {
        private readonly IMenu _menuService;
        private readonly Window _window;

        [ObservableProperty]
        private string _categoryName = string.Empty;

        [ObservableProperty]
        private string? _errorMessage;

        public AddCategoryViewModel(IMenu menuService, Window window)
        {
            _menuService = menuService;
            _window = window;
        }

        [RelayCommand]
        private async Task AddCategory()
        {
            Debug.WriteLine("→ Add Category started");
            if (string.IsNullOrWhiteSpace(CategoryName))
            {
                ShowError("All fields are required");
                return;
            }

            try
            {
                var newCategory = new Category
                {
                    CtgryName = CategoryName,
                };

                var (isSuccess, message, _) = await _menuService.AddCategory(newCategory, CashierState.ManagerEmail!);
                
                if (isSuccess)
                {
                    await ShowSuccess(message);
                    _window.Close();
                }
                else
                {
                    ShowError(message);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                Debug.WriteLine($"❌ AddUser exception: {ex}");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _window.Close();
        }

        private async void ShowError(string message)
        {
            var msgBox = MessageBoxManager
                .GetMessageBoxStandard(new MessageBoxStandardParams
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