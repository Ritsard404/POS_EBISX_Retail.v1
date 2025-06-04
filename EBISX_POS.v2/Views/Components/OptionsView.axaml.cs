using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using EBISX_POS.API.Models;
using EBISX_POS.Models;
using EBISX_POS.Services.DTO.Menu;
using EBISX_POS.State;
using EBISX_POS.ViewModels;
using System.Diagnostics;
using System.Linq;

namespace EBISX_POS.Views
{
    public partial class OptionsView : UserControl
    {


        private ToggleButton? _selectedItemButton;   // Stores selected menu item
        private ToggleButton? _selectedSizeButton;   // Stores selected size (Regular / Medium / Large)
        private ToggleButton? _selectedDrinkTypeButton;
        private ToggleButton? _selectedAddOnTypeButton;
        private ToggleButton? _selectedAddOnButton;
        private ToggleButton? _selectedDrinksButton;

        private string? _selectedItem;    // Store selected menu item text
        private string? _selectedSize;    // Store selected size text
        private string? _selectedDrinkType;
        private string? _selectedAddOnType;
        private string? _selectedDrink;
        private string? _selectedAddOn;

        public OptionsView()
        {
            InitializeComponent();
            DataContext = new OptionsViewModel();

        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton clickedButton)
            {
                var parentStackPanel = clickedButton.Parent as StackPanel;

                // Determine which group this button belongs to
                if (clickedButton.DataContext is DrinkDetailDTO Drink)
                {
                    HandleSelection(ref _selectedDrinksButton, clickedButton, ref _selectedDrink);

                    OrderState.UpdateItemOrder(itemType: "Drink", itemId: Drink.MenuId, name: Drink.MenuName, price: Drink.MenuPrice, size: SelectedOptionsState.SelectedSize, hasAddOn: false, hasDrink: false, isVatZero: false);

                    //Debug.WriteLine($"Selected Drink: {Drink.MenuId}");
                    //OrderState.DisplayOrders();
                }
                else if (clickedButton.DataContext is AddOnDetailDTO AddOn)
                {
                    HandleSelection(ref _selectedAddOnButton, clickedButton, ref _selectedAddOn);

                    OrderState.UpdateItemOrder(itemType: "Add-On", itemId: AddOn.MenuId, name: AddOn.MenuName, price: AddOn.Price, size: AddOn.Size, hasAddOn: false, hasDrink: false, isVatZero: false);

                    //Debug.WriteLine($"Selected AddOn: {AddOn.MenuName} Size: {AddOn.Size}");
                    //OrderState.DisplayOrders();

                }
                else if (clickedButton.DataContext is AddOnTypeDTO selectedAddOnType)
                {
                    HandleSelection(ref _selectedAddOnTypeButton, clickedButton, ref _selectedAddOnType);
                    OptionsState.UpdateAddOns(selectedAddOnType.AddOnTypeId);
                    //Debug.WriteLine($"Selected AddOns Type: {selectedAddOnType.AddOnTypeName} Id: {selectedAddOnType.AddOnTypeId}");
                }
                else if (clickedButton.DataContext is DrinkTypeDTO selectedDrinkType)
                {
                    HandleSelection(ref _selectedDrinkTypeButton, clickedButton, ref _selectedDrinkType);
                    SelectedOptionsState.SelectedDrinkType = selectedDrinkType.DrinkTypeId;
                    OptionsState.UpdateDrinks(selectedDrinkType.DrinkTypeId, SelectedOptionsState.SelectedSize);


                    //Debug.WriteLine($"Selected Drink Type: {selectedDrinkType.DrinkTypeName} Id: {selectedDrinkType.DrinkTypeId}");
                }
                // If the button is inside an ItemsControl (for sizes)
                else if (clickedButton.DataContext is string size)
                {
                    HandleSelection(ref _selectedSizeButton, clickedButton, ref _selectedSize);
                    SelectedOptionsState.SelectedSize = size;
                    OptionsState.UpdateDrinks(SelectedOptionsState.SelectedDrinkType, size);
                    //Debug.WriteLine($"Selected Size: {size}");
                }
                else if (clickedButton.DataContext is ItemMenu item)
                {
                    HandleSelection(ref _selectedItemButton, clickedButton, ref _selectedItem);
                    //Debug.WriteLine($"Selected Item: {item.ItemName}");
                }
            }
        }

        private void HandleSelection(ref ToggleButton? selectedButton, ToggleButton clickedButton, ref string? selectedValue)
        {
            if (selectedButton == clickedButton)
            {
                clickedButton.IsChecked = false;
                selectedButton = null;
                selectedValue = null;
            }
            else
            {
                if (selectedButton != null)
                {
                    selectedButton.IsChecked = false;
                }

                clickedButton.IsChecked = true;
                selectedButton = clickedButton;
                selectedValue = clickedButton.Content?.ToString();
            }
        }
    }
}
