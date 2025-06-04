using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity; // Add this line
using EBISX_POS.Models;
using EBISX_POS.ViewModels;
using EBISX_POS.Services; // Ensure this is added
using System.Diagnostics;
using System.Threading.Tasks;
using EBISX_POS.State;
using EBISX_POS.API.Models;
using System.Linq;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;

namespace EBISX_POS.Views
{
    public partial class ItemListView : UserControl
    {
        private readonly MenuService _menuService;


        private Button? _selectedItemButton;
        private string? _selectedItem;

        public ItemListView(MenuService menuService) // Ensure this constructor is public
        {
            InitializeComponent();
            _menuService = menuService;
            DataContext = new ItemListViewModel(menuService); // Set initial DataContext
            this.Loaded += OnLoaded; // Add this line

        }

        private void OnLoaded(object? sender, RoutedEventArgs e) // Update the event handler signature
        {
            if (ItemsList.ItemContainerGenerator.ContainerFromIndex(0) is ToggleButton firstButton)
            {
                firstButton.IsChecked = true;
                _selectedItemButton = firstButton;
                _selectedItem = firstButton.Content?.ToString();
            }
        }

        public async Task LoadMenusAsync(int categoryId)
        {
            IsLoadMenu.IsVisible = true;
            if (DataContext is ItemListViewModel viewModel)
            {
                await viewModel.LoadMenusAsync(categoryId);
            }
            IsLoadMenu.IsVisible = false;
        }

        public void UpdateDataContext(ItemListViewModel viewModel)
        {
            DataContext = viewModel;
        }

        private async void OnItemClicked(object? sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton && clickedButton.DataContext is ItemMenu item)
            {
                IsLoadMenu.IsVisible = true;
                HandleSelection(ref _selectedItemButton, clickedButton, ref _selectedItem);


                var getDrinksTask = _menuService.GetDrinks(item.Id);
                var getAddOnsTask = _menuService.GetAddOns(item.Id);

                await Task.WhenAll(getDrinksTask, getAddOnsTask);
                var drinksResult = getDrinksTask.Result;
                var addOnResult = getAddOnsTask.Result;

                if (await ShowAvailabilityWarningAsync(item.HasDrink, drinksResult.DrinkTypesWithDrinks.Any(),
                    "No Drinks Available",
                    "Sorry, this item is flagged to include a drink, but none are available at the moment."))
                {
                    return;
                }

                if (await ShowAvailabilityWarningAsync(item.HasAddOn, addOnResult.Any(),
                    "No Add-On Available",
                    "Sorry, this item is flagged to include an add-on, but none are available at the moment."))
                {
                    return;
                }


                if (item.IsSolo || item.IsAddOn ||(!item.HasDrink && !item.HasAddOn))
                {
                    var owner = this.VisualRoot as Window;

                    if (OrderState.CurrentOrderItem.Quantity < 1)
                    {
                        OrderState.CurrentOrderItem.Quantity = 1;
                    }

                    // Determine the item type based on the item's properties.
                    string itemType = item.IsDrink ? "Drink" : item.IsAddOn ? "Add-On" : "Menu";

                    // Clear the current sub-orders.
                    OrderState.CurrentOrderItem.SubOrders.Clear();

                    // Update the order item with the appropriate type and details.
                    OrderState.UpdateItemOrder(
                        itemType: itemType,
                        itemId: item.Id,
                        name: item.ItemName,
                        price: item.Price,
                        size: item.Size,
                        hasAddOn: item.HasAddOn,
                        hasDrink: item.HasDrink,
                        isVatZero: item.IsVatZero
                    );


                    var mainWindow = this.VisualRoot as MainWindow;

                    if (mainWindow != null)
                    {
                        mainWindow.IsLoadMenu.IsVisible = true;
                        mainWindow.IsMenuAvail.IsVisible = false;
                    }
                    // Finalize the current order and exit.
                    await OrderState.FinalizeCurrentOrder(isSolo: true, owner);

                    if (mainWindow != null)
                    {
                        mainWindow.IsLoadMenu.IsVisible = false;
                        mainWindow.IsMenuAvail.IsVisible = true;
                    }
                    TenderState.tenderOrder.CalculateTotalAmount();
                    return;
                }


                OrderState.CurrentOrderItem.SubOrders.Clear();
                OrderState.CurrentOrderItem.Quantity = (OrderState.CurrentOrderItem.Quantity < 1)
                    ? 1
                    : OrderState.CurrentOrderItem.Quantity;
                OrderState.UpdateItemOrder(itemType: "Menu", itemId: item.Id, name: item.ItemName, price: item.Price, size: null, hasAddOn: false, hasDrink: false, isVatZero: item.IsVatZero);

                var detailsWindow = new SubItemWindow(item, _menuService);
                detailsWindow.DataContext = new SubItemWindowViewModel(item, _menuService, detailsWindow);


                await detailsWindow.ShowDialog((Window)this.VisualRoot);
                IsLoadMenu.IsVisible = false;
            }
        }

        private void HandleSelection(ref Button? selectedButton, Button clickedButton, ref string? selectedValue)
        {
            if (selectedButton == clickedButton)
            {
                selectedButton = null;
                selectedValue = null;
            }
            else
            {
                selectedButton = clickedButton;
                selectedValue = clickedButton.Content?.ToString();
            }
        }
        private async Task<bool> ShowAvailabilityWarningAsync(bool featureEnabled, bool hasData, string title, string message)
        {
            if (!featureEnabled || hasData) return false;

            var msg = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentHeader = title,
                ContentMessage = message,
                ButtonDefinitions = ButtonEnum.Ok,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                SizeToContent = SizeToContent.WidthAndHeight,
                Width = 400,
                SystemDecorations = SystemDecorations.None,
                Icon= Icon.Info,
                ShowInCenter = true,
            });

            await msg.ShowAsPopupAsync((Window)this.VisualRoot);
            return true;
        }
    }
}
