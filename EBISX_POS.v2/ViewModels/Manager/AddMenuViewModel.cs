using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EBISX_POS.API.Services.Interfaces;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System.Threading.Tasks;
using EBISX_POS.API.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Media.Imaging;
using System.IO;

namespace EBISX_POS.ViewModels.Manager
{
    /// <summary>
    /// ViewModel for adding or editing menu items in the system
    /// </summary>
    public partial class AddMenuViewModel : ObservableObject
    {
        // Dependencies
        private readonly IMenu _menuService;
        private readonly Window _window;

        // Observable properties for UI binding
        [ObservableProperty]
        private API.Models.Menu? _menuDetails;

        [ObservableProperty]
        private List<Category>? _categories;

        [ObservableProperty]
        private List<AddOnType>? _addOns;

        [ObservableProperty]
        private List<DrinkType>? _drinks;

        [ObservableProperty]
        private List<string> _drinkSizes = new List<string>
        {
            "",
            "R",
            "M",
            "L",
            "XL"
        };

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private Bitmap? _selectedImage;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string _availabilityName;

        [ObservableProperty]
        private string _hasAddOnName = "Has Add-On";

        [ObservableProperty]
        private string _hasDrinkName = "Has Drink";

        [ObservableProperty]
        private string _isAddOnName = "Is Add-On";

        [ObservableProperty]
        private string _windowTitle = "Add New Product";

        /// <summary>
        /// Initializes a new instance of the AddMenuViewModel
        /// </summary>
        /// <param name="menuService">Service for menu operations</param>
        /// <param name="window">Parent window for dialogs</param>
        /// <param name="menuToEdit">Optional menu item to edit</param>
        /// <exception cref="ArgumentNullException">Thrown when menuService or window is null</exception>
        public AddMenuViewModel(IMenu menuService, Window window, API.Models.Menu? menuToEdit = null)
        {
            _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
            _window = window ?? throw new ArgumentNullException(nameof(window));

            // Initialize MenuDetails with default values or edit values
            MenuDetails = menuToEdit ?? new API.Models.Menu
            {
                MenuName = string.Empty,
                MenuPrice = 0,
                MenuIsAvailable = true,
                HasDrink = false,
                HasAddOn = false,
                IsAddOn = false,
                Category = new Category { Id = 0, CtgryName = "" },
                DrinkType = new DrinkType { Id = 0, DrinkTypeName = "" },
                AddOnType = new AddOnType { Id = 0, AddOnTypeName = "" }
            };

            // Set edit mode if menuToEdit is provided
            IsEditMode = menuToEdit != null;
            WindowTitle = IsEditMode ? "Edit Product" : "Add New Product";
            AvailabilityName = MenuDetails.MenuIsAvailable ? "Available" : "Not Available";
            HasAddOnName = MenuDetails.HasAddOn ? "Has Add-On ✓" : "Has Add-On";
            HasDrinkName = MenuDetails.HasDrink ? "Has Drink ✓" : "Has Drink";
            IsAddOnName = MenuDetails.IsAddOn ? "Is Add-On ✓" : "Is Add-On";

            // Load initial data
            _ = LoadCombos();

            // Load image if in edit mode and image path exists
            if (IsEditMode && !string.IsNullOrEmpty(MenuDetails.MenuImagePath))
            {
                _ = LoadExistingImage();
            }
        }

        partial void OnMenuDetailsChanged(API.Models.Menu? value)
        {
            if (value != null)
            {
                AvailabilityName = value.MenuIsAvailable ? "Available" : "Not Available";
                HasAddOnName = value.HasAddOn ? "Has Add-On ✓" : "Has Add-On";
                HasDrinkName = value.HasDrink ? "Has Drink ✓" : "Has Drink";
                IsAddOnName = value.IsAddOn ? "Is Add-On ✓" : "Is Add-On";
            }
        }

        private async Task LoadExistingImage()
        {
            try
            {
                if (string.IsNullOrEmpty(MenuDetails?.MenuImagePath)) return;

                using var stream = File.OpenRead(MenuDetails.MenuImagePath);
                var bitmap = await Task.Run(() => Bitmap.DecodeToWidth(stream, 300));
                if (bitmap != null)
                {
                    SelectedImage = bitmap;
                    OnPropertyChanged(nameof(SelectedImage));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading existing image: {ex}");
            }
        }

        /// <summary>
        /// Loads all required combo box data (categories, drinks, add-ons)
        /// </summary>
        private async Task LoadCombos()
        {
            try
            {
                IsLoading = true;

                // 1) Load from service
                var cats = await _menuService.Categories();
                var drks = await _menuService.GetDrinkTypes();
                var adds = await _menuService.GetAddOnTypes();

                // 2) Insert a "blank" entry at index 0
                drks?.Insert(0, new DrinkType { Id = 0, DrinkTypeName = "" });
                adds?.Insert(0, new AddOnType { Id = 0, AddOnTypeName = "" });

                // 3) Assign back to your ObservableProperties
                Categories = cats;
                Drinks = drks;
                AddOns = adds;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading combo data: {ex}");
                await ShowError("Failed to load menu data. Please try again.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Validates the menu details before submission
        /// </summary>
        /// <returns>True if validation passes, false otherwise</returns>
        private bool ValidateMenuDetails()
        {
            if (MenuDetails is null)
            {
                ShowError("Please enter menu details before saving.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(MenuDetails.MenuName))
            {
                ShowError("Menu name is required.");
                return false;
            }

            if (MenuDetails.MenuPrice <= 0)
            {
                ShowError("Menu price must be more than ₱0.00.");
                return false;
            }

            if (MenuDetails.Category is null || MenuDetails.Category.Id <= 0)
            {
                ShowError("You must choose a valid category.");
                return false;
            }

            bool hasAddonType = MenuDetails.AddOnType?.Id > 0;
            bool hasDrinkType = MenuDetails.DrinkType?.Id > 0;
            bool flagAddon = MenuDetails.HasAddOn;
            bool flagDrink = MenuDetails.HasDrink;
            bool isAddOn = MenuDetails.IsAddOn;

            // 1) If this item itself is marked as an Add-On...
            if (isAddOn)
            {
                if (!hasAddonType)
                {
                    ShowError("You marked this item as an Add-On, so you must select an Add-On type.");
                    return false;
                }
                if (flagAddon || flagDrink)
                {
                    ShowError("An Add-On item cannot be flagged as Has Add‑On or Has Drink.");
                    return false;
                }
                // All good for pure Add-On items
                return true;
            }

            // 2) If AddOnType is chosen for a non–Add-On item, it must not have flags
            if (hasAddonType)
            {
                if (flagAddon || flagDrink)
                {
                    ShowError("An item with an Add‑On type cannot also be flagged as Has Add‑On or Has Drink.");
                    return false;
                }

                if (!isAddOn)
                {
                    ShowError("An item not marked as an Add-On it must also have an Add-On type selected.");
                    return false;
                }
                // valid base item with configured add-on type
                return true;
            }

            // 3) If DrinkType is chosen for a non–Add-On item, it must not have flags
            if (hasDrinkType)
            {
                if (flagAddon || flagDrink)
                {
                    ShowError("An item with a Drink type cannot also be flagged as Has Add‑On or Has Drink.");
                    return false;
                }
                return true;
            }

            // 5) If flagged HasAddOn, it must NOT have an AddOnType
            if (flagAddon && hasAddonType)
            {
                ShowError("An item marked \"Has Add‑On\" cannot also have an Add‑On type selected.");
                return false;
            }

            // 6) If flagged HasDrink, it must NOT have a DrinkType
            if (flagDrink && hasDrinkType)
            {
                ShowError("An item marked \"Has Drink\" cannot also have a Drink type selected.");
                return false;
            }

            return true;
        }


        /// <summary>
        /// Command to handle image upload
        /// </summary>
        [RelayCommand]
        public async Task UploadImage()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Choose an image",
                AllowMultiple = false,
                Filters =
                {
                    new FileDialogFilter
                    {
                        Name = "Image Files",
                        Extensions = { "png", "jpg", "jpeg", "bmp", "gif" }
                    }
                }
            };

            var result = await dlg.ShowAsync(_window);
            if (result is null || result.Length == 0)
                return;

            var path = result[0];
            try
            {
                using var stream = File.OpenRead(path);
                var bitmap = await Task.Run(() => Bitmap.DecodeToWidth(stream, 300));
                if (bitmap != null)
                {
                    SelectedImage = bitmap;
                    MenuDetails!.MenuImagePath = path;
                    OnPropertyChanged(nameof(SelectedImage));
                }
                else
                {
                    await ShowError("Failed to decode image file.");
                }
            }
            catch (IOException)
            {
                await ShowError("Failed to load image file.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading image: {ex}");
                await ShowError("An unexpected error occurred while loading the image.");
            }
        }

        /// <summary>
        /// Command to save menu changes (add or edit)
        /// </summary>
        [RelayCommand]
        private async Task SaveMenu()
        {
            if (IsLoading) return;

            if (!ValidateMenuDetails())
            {
                return;
            }

            try
            {
                IsLoading = true;

                var menuToSave = new API.Models.Menu
                {
                    Id = MenuDetails!.Id,
                    SearchId = MenuDetails.SearchId,
                    MenuName = MenuDetails.MenuName,
                    MenuPrice = MenuDetails.MenuPrice,
                    Category = MenuDetails.Category,
                    Size = MenuDetails.Size,
                    DrinkType = MenuDetails.DrinkType == null || MenuDetails.DrinkType.Id == 0 ? null : MenuDetails.DrinkType,
                    AddOnType = MenuDetails.AddOnType == null || MenuDetails.AddOnType.Id == 0 ? null : MenuDetails.AddOnType,
                    HasAddOn = MenuDetails.HasAddOn,
                    HasDrink = MenuDetails.HasDrink,
                    IsAddOn = MenuDetails.IsAddOn,
                    MenuImagePath = MenuDetails.MenuImagePath,
                    MenuIsAvailable = MenuDetails.MenuIsAvailable,
                    Qty = MenuDetails.Qty
                };

                var (isSuccess, message) = IsEditMode
                    ? await _menuService.UpdateMenu(menuToSave, CashierState.ManagerEmail!)
                    : await _menuService.AddMenu(menuToSave, CashierState.ManagerEmail!);

                if (isSuccess)
                {
                    await ShowSuccess(message);
                    _window.Close(true);
                    return;
                }

                await ShowError(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving menu: {ex}");
                await ShowError("Failed to save menu. Please try again.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Command to cancel the add menu operation
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            _window.Close(false);
        }

        [RelayCommand]
        private void ClearImage()
        {
            SelectedImage = null;
            MenuDetails.MenuImagePath = null;
        }

        /// <summary>
        /// Shows a success message dialog
        /// </summary>
        /// <param name="message">Message to display</param>
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

        /// <summary>
        /// Shows an error message dialog
        /// </summary>
        /// <param name="message">Error message to display</param>
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
    }
}
