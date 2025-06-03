using CommunityToolkit.Mvvm.ComponentModel;
using EBISX_POS.Models;
using EBISX_POS.Services;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using EBISX_POS.Services.DTO.Menu;
using System.Collections.ObjectModel;
using EBISX_POS.State;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using EBISX_POS.API.Models;
using Avalonia.Controls;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System.ComponentModel;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;

namespace EBISX_POS.ViewModels
{

    public partial class SubItemWindowViewModel : ViewModelBase
    {
        private readonly MenuService _menuService;

        public ItemMenu Item { get; }


        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _hasOptions;




        public string ItemNameAndQuantity
        => Item.ItemName +
            $" ({(OrderState.CurrentOrderItem.Quantity == 0 ? 1 : OrderState.CurrentOrderItem.Quantity)})";
        public SubItemWindowViewModel(ItemMenu item, MenuService menuService, Window? window)
        {
            // Subscribe to changes of the static property.
            OrderState.StaticPropertyChanged += OnOrderStateStaticPropertyChanged;

            OrderState.CurrentOrderItem.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(ItemNameAndQuantity));
            };

            Item = item;
            _menuService = menuService;


            _ = LoadOptions();

        }

        private void OnOrderStateStaticPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OrderState.CurrentOrderItem))
            {
                // Notify the view that the property has updated.
                OnPropertyChanged(nameof(ItemNameAndQuantity));
            }
        }
        public async Task LoadOptions()
        {
            try
            {
                // Validate required item
                if (Item == null)
                {
                    Debug.WriteLine("Error: Cannot load options - menu item is null");
                    return;
                }

                Debug.WriteLine($"Loading options for Item ID: {Item.Id}");
                IsLoading = true;
                HasOptions = false;

                var desktop = Application.Current?.ApplicationLifetime
                                  as IClassicDesktopStyleApplicationLifetime;
                var owner = desktop?.MainWindow as Window;


                // 🔹 Reset state before fetching new data
                OptionsState.DrinkTypes.Clear();
                OptionsState.DrinkSizes.Clear();
                OptionsState.AddOnsType.Clear();
                SelectedOptionsState.SelectedDrinkType = null;
                SelectedOptionsState.SelectedSize = null;

                // Load drink options
                var drinksResult = await _menuService.GetDrinks(Item.Id);
                if (drinksResult != null
                    && drinksResult.DrinkTypesWithDrinks.Any())
                {

                    // Update drink types
                    //OptionsState.DrinkTypes.Clear();
                    foreach (var dt in drinksResult.DrinkTypesWithDrinks)
                        OptionsState.DrinkTypes.Add(dt);

                    // Update available sizes
                    //OptionsState.DrinkSizes.Clear();
                    foreach (var size in drinksResult.Sizes)
                        OptionsState.DrinkSizes.Add(size);

                    var firstType = drinksResult.DrinkTypesWithDrinks[0];
                    var firstSize = drinksResult.Sizes[0];

                    SelectedOptionsState.SelectedDrinkType = firstType.DrinkTypeId;
                    SelectedOptionsState.SelectedSize = firstSize;
                    OptionsState.UpdateDrinks(firstType.DrinkTypeId, firstSize);

                }
                else
                {
                    OptionsState.DrinkTypes.Clear();
                    OptionsState.DrinkSizes.Clear();
                    SelectedOptionsState.SelectedDrinkType = null;
                    SelectedOptionsState.SelectedSize = null;
                }

                // Load add-on options
                var addOnResult = await _menuService.GetAddOns(Item.Id);
                if (addOnResult != null && addOnResult.Any())
                {
                    foreach (var addOn in addOnResult)
                        OptionsState.AddOnsType.Add(addOn);

                    // Default Display

                    var firstAddOnType = addOnResult[0].AddOnTypeId;
                    OptionsState.UpdateAddOns(firstAddOnType);
                }
                else
                {
                    OptionsState.AddOnsType.Clear();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading options: {ex.Message}");
                //NotificationService.NetworkIssueMessage();
            }
            finally
            {
                // Always clear loading state
                IsLoading = false;
                HasOptions = true;
            }
        }
    }
}
