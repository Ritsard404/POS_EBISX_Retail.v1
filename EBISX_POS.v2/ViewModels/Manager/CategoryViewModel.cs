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
    public partial class CategoryViewModel : ObservableObject
    {
        private readonly IMenu _menuService;
        private readonly Window _window;

        [ObservableProperty]
        private ObservableCollection<Category> _categories = new();

        [ObservableProperty]
        private Category? _selectedCategory;

        [ObservableProperty]
        private bool _isLoading;

        public CategoryViewModel(IMenu menu, Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _menuService = menu ?? throw new ArgumentNullException(nameof(menu));

            _ = LoadCategories();
        }

        private async Task LoadCategories()
        {
            try
            {
                IsLoading = true;
                var categories = await _menuService.GetCategories();
                if (categories != null)
                {
                    Categories.Clear();
                    foreach (var category in categories)
                    {
                        Categories.Add(category);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading Categories: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void SaveCategoryChanges(Category category)
        {
            if (category == null) return;

            try
            {
                var (isSuccess, message) = await _menuService.UpdateCategory(category, CashierState.ManagerEmail!);

                if (isSuccess)
                {
                    await LoadCategories();
                    //await ShowSuccess(message);
                    return;
                }
                ShowError(message);
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving user changes: {ex}");
            }
        }

        [RelayCommand]
        private async Task AddCategory()
        {
            try
            {
                var addCategoryWindow = new AddCategoryWindow();
                addCategoryWindow.DataContext = new AddCategoryViewModel(_menuService, addCategoryWindow);

                await addCategoryWindow.ShowDialog(_window);
                await LoadCategories();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening add user window: {ex}");
            }
        }

        public async Task RemoveCategory(Category category)
        {
            if(category == null) return;

            try
            {
                var (isSuccess, message) = await _menuService.DeleteCategory(category.Id, CashierState.ManagerEmail!);

                if (isSuccess)
                {
                    await LoadCategories();
                    await ShowSuccess(message);
                    return;
                }
                ShowError(message);
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving user changes: {ex}");
            }
        }

        [RelayCommand]
        private void CloseWindow()
        {
            _window.Close();
        }

        partial void OnSelectedCategoryChanged(Category? value)
        {
            if (value != null)
            {
                SaveCategoryChanges(value);
            }
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