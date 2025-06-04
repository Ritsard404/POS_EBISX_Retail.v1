using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using MsBox.Avalonia.Enums;
using EBISX_POS.API.Models;
using EBISX_POS.Services;
using EBISX_POS.State;
using EBISX_POS.ViewModels;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia;
using System.ComponentModel;
using System.Linq;
using EBISX_POS.Models;
using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using EBISX_POS.API.Services.Interfaces;
using System.Text;
using System.Threading.Tasks;

namespace EBISX_POS.Views
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly IMenu _menu;
        private readonly MenuService _menuService;
        private readonly AuthService _authService;
        private ToggleButton? _selectedMenuButton; // Stores selected menu item
        private Category? _selectedMenuItem;       // Stores selected menu item object

        private bool isLoading = true;
        public bool IsLoading
        {
            get => isLoading;
            set
            {
                if (isLoading != value)
                {
                    isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private StringBuilder _barcodeBuffer = new StringBuilder();
        private DateTime _lastKeyPress = DateTime.MinValue;
        private const int BARCODE_TIMEOUT_MS = 100; // Timeout between keypresses for barcode scanner
        private bool _isBarcodeMode = false; // Flag to track if we're in barcode scanning mode

        public MainWindow()
        {
            InitializeComponent();

            // Get services from DI container
            _menuService = App.Current.Services.GetRequiredService<MenuService>();
            _authService = App.Current.Services.GetRequiredService<AuthService>();
            _menu = App.Current.Services.GetRequiredService<IMenu>();

            // Create and set the ItemListView
            var itemListView = CreateItemListView();
            ItemListViewContainer.Content = itemListView;

            // Set up global keyboard handling
            this.AddHandler(InputElement.KeyDownEvent, OnGlobalKeyDown, RoutingStrategies.Tunnel);

            // Initialize barcode mode toggle button state
            if (BarcodeModeToggle != null)
            {
                BarcodeModeToggle.IsChecked = _isBarcodeMode;
            }

            // When the window is opened, load the first category's menus.
            this.Opened += async (s, e) =>
            {
                // Set loading flag to true
                IsLoadCtgry.IsVisible = true;
                IsLoadMenu.IsVisible = true;
                IsCtgryAvail.IsVisible = false;
                IsMenuAvail.IsVisible = false;

                bool isCashedDrawer = await _authService.IsCashedDrawer();
                if (!isCashedDrawer)
                {
                    var setCashDrawer = new SetCashDrawerWindow("Cash-In");
                    await setCashDrawer.ShowDialog(this);
                }

                var categories = await _menuService.GetCategoriesAsync();
                if (categories.Any())
                {
                    var firstCategory = categories.First();

                    await itemListView.LoadMenusAsync(firstCategory.Id);

                    // Once loaded, set loading flag to false.
                    IsLoadCtgry.IsVisible = false;
                    IsLoadMenu.IsVisible = false;
                    IsCtgryAvail.IsVisible = true;
                    IsMenuAvail.IsVisible = true;
                }

            };

            OnPropertyChanged(nameof(OrderState.CurrentOrderItem));
        }


        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            // Get all ToggleButtons that are part of the ItemsControl's visual tree.
            var toggleButtons = MenuGroup.GetLogicalDescendants().OfType<ToggleButton>().ToList();

            if (toggleButtons.Any())
            {
                // Set the first ToggleButton as checked.
                _selectedMenuButton = toggleButtons.First();
                _selectedMenuButton.IsChecked = true;
            }
        }


        private ItemListView CreateItemListView()
        {
            return new ItemListView(_menuService);
        }

        private async void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton clickedButton)
            {
                // Get the Category object from the clicked button's DataContext.
                var menuItem = clickedButton.DataContext as Category;

                // Deselect previous button if it's different.
                if (_selectedMenuButton != null && _selectedMenuButton != clickedButton)
                {
                    _selectedMenuButton.IsChecked = false;
                }

                // Toggle the current button's state.
                bool isSelected = clickedButton.IsChecked ?? false;
                _selectedMenuButton = isSelected ? clickedButton : null;
                _selectedMenuItem = isSelected ? menuItem : null;

                if (_selectedMenuItem != null)
                {
                    if (DataContext is MainWindowViewModel viewModel)
                    {
                        // Set loading flag to true before loading
                        IsLoading = true;
                        await viewModel.LoadMenusAsync(_selectedMenuItem.Id);

                        // Update ItemListView's DataContext.
                        if (ItemListViewContainer.Content is ItemListView itemListView)
                        {
                            await itemListView.LoadMenusAsync(_selectedMenuItem.Id);
                        }
                        // Set loading flag to false after loading
                        IsLoading = false;
                    }
                }
            }
        }
        private void NumberButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Content?.ToString(), out int digit))
            {
                if (!OrderState.CurrentOrderItem.SubOrders.Any())
                {
                    OrderState.CurrentOrderItem.Quantity = digit;
                    OrderState.UpdateItemOrder(itemType: "Menu", itemId: 0, name: "Select Menu", price: 0, size: null, false, false, false);
                    OrderState.CurrentOrderItem.RefreshDisplaySubOrders();
                    return;
                }

                // Build new quantity string and parse it back to int
                string newString = $"{(OrderState.CurrentOrderItem.Quantity == 0 ? "" : OrderState.CurrentOrderItem.Quantity)}{digit}";

                if (int.TryParse(newString, out int newValue))
                {
                    OrderState.CurrentOrderItem.Quantity = newValue;
                    OnPropertyChanged(nameof(OrderState.CurrentOrderItem));
                }

            }
        }

        private void ClearNumber_Click(object sender, RoutedEventArgs e)
        {
            OrderState.CurrentOrderItem.Quantity = 0;
        }

        private async void CancelOrder_Click(object sender, RoutedEventArgs e)
        {
            var orderService = App.Current.Services.GetRequiredService<OrderService>();

            if (!OrderState.CurrentOrder.Any())
                return;

            //var box = MessageBoxManager.GetMessageBoxStandard(
            //    new MessageBoxStandardParams
            //    {
            //        ContentHeader = "Cancel Order",
            //        ContentMessage = "Swipe the manager ID.",
            //        ButtonDefinitions = ButtonEnum.OkCancel, // Defines the available buttons
            //        WindowStartupLocation = WindowStartupLocation.CenterOwner,
            //        CanResize = false,
            //        SizeToContent = SizeToContent.WidthAndHeight,
            //        Width = 400,
            //        ShowInCenter = true,
            //        Icon = MsBox.Avalonia.Enums.Icon.Warning
            //    });

            //var result = await box.ShowAsPopupAsync(this);

            //switch (result)
            //{
            //    case ButtonResult.Ok:
            //        var (isSuccess, message) = await orderService.CancelCurrentOrder(managerEmail);
            //        Debug.WriteLine($" Cancel Message: {message}");


            //        OrderState.CurrentOrderItem = new OrderItemState();
            //        OrderState.CurrentOrder.Clear();
            //        OrderState.CurrentOrderItem.RefreshDisplaySubOrders();
            //        return;
            //    case ButtonResult.Cancel:
            //        return;
            //    default:
            //        return;
            //}


            var swipeManager = new ManagerSwipeWindow(header: "Manager", message: "Please ask the manager to swipe.", ButtonName: "Swipe");
            var (success, email) = await swipeManager.ShowDialogAsync(this);

            if (success)
            {
                var (isSuccess, message) = await orderService.CancelCurrentOrder(email);
                Debug.WriteLine($" Cancel Message: {message}");


                OrderState.CurrentOrderItem = new OrderItemState();
                OrderState.CurrentOrder.Clear();
                OrderState.CurrentOrderItem.RefreshDisplaySubOrders();
                TenderState.tenderOrder.CalculateTotalAmount();

                return;
            }

        }

        private async void OrderType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string orderType = string.Empty;

                if (btn.Content is TextBlock textBlock)
                {
                    orderType = textBlock.Text;
                }
                else
                {
                    orderType = btn.Content?.ToString() ?? string.Empty;
                }

                // Perform actions based on the orderType
                switch (orderType)
                {
                    case "DINE IN":
                        // Handle Dine In logic
                        TenderState.tenderOrder.OrderType = "Dine In";
                        break;
                    case "TAKE OUT":
                        // Handle Take Out logic
                        TenderState.tenderOrder.OrderType = "Take Out";
                        break;
                    case "TENDER":
                        // Handle Take Out logic
                        TenderState.tenderOrder.OrderType = "";
                        break;
                    default:
                        // Handle other cases if necessary
                        break;
                }

                TenderState.tenderOrder.Reset();
                TenderState.tenderOrder.HasScDiscount = OrderState.CurrentOrder.Any(d => d.IsSeniorDiscounted);
                TenderState.tenderOrder.HasPwdDiscount = OrderState.CurrentOrder.Any(d => d.IsPwdDiscounted);

                // Select the PromoDiscountAmount from the first order that has a non-null value
                TenderState.tenderOrder.PromoDiscountAmount = OrderState.CurrentOrder
                    .Where(d => d.PromoDiscountAmount != null)
                    .Select(d => d.PromoDiscountAmount)
                    .FirstOrDefault() ?? 0m;
                TenderState.tenderOrder.CalculateTotalAmount();

                // Open the TenderOrderWindow
                var tenderOrderWindow = new TenderOrderWindow();
                await tenderOrderWindow.ShowDialog((Window)this.VisualRoot);
            }
        }

        private async void DiscountPwdSc_Click(object sender, RoutedEventArgs e)
        {
            if (OrderState.CurrentOrder.Any(d => d.HasDiscount) || OrderState.CurrentOrder.Any(d => d.CouponCode != null))
            {

                var dbox = MessageBoxManager.GetMessageBoxStandard(
                    new MessageBoxStandardParams
                    {
                        ContentHeader = $"Discounted already!",
                        ContentMessage = "The order has discounted already!",
                        ButtonDefinitions = ButtonEnum.Ok, // Defines the available buttons
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        CanResize = false,

                        SizeToContent = SizeToContent.WidthAndHeight,
                        Width = 400,
                        ShowInCenter = true,
                        Icon = MsBox.Avalonia.Enums.Icon.Error
                    });

                var dresult = await dbox.ShowAsPopupAsync(this);
                switch (dresult)
                {
                    case ButtonResult.Ok:
                        return;
                    default:
                        return;
                }
            }


            var swipeManager = new ManagerSwipeWindow(header: "Pwd/SC Discount", message: "Please ask the manager to swipe.", ButtonName: "Swipe");
            var (success, email) = await swipeManager.ShowDialogAsync(this);
            if (success)
            {
                CashierState.ManagerEmail = email;
                //var discountPwdSw = new SelectDiscountPwdScWindow();
                //await discountPwdSw.ShowDialog(this);

                var discScPwd = new AddSeniorPwdDiscountWindow(email);
                await discScPwd.ShowDialog(this);

                if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
                {
                    desktopLifetime.MainWindow?.Close();
                }
                return;
            }

        }
        private async void OtherDiscount_Click(object sender, RoutedEventArgs e)
        {
            if (OrderState.CurrentOrder.Any(d => d.HasDiscount) || OrderState.CurrentOrder.Any(d => d.CouponCode != null))
            {

                var dbox = MessageBoxManager.GetMessageBoxStandard(
                    new MessageBoxStandardParams
                    {
                        ContentHeader = $"Discounted already!",
                        ContentMessage = "The order has discounted already!",
                        ButtonDefinitions = ButtonEnum.Ok, // Defines the available buttons
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        CanResize = false,
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Width = 400,
                        ShowInCenter = true,
                        Icon = MsBox.Avalonia.Enums.Icon.Error
                    });

                var dresult = await dbox.ShowAsPopupAsync(this);
                switch (dresult)
                {
                    case ButtonResult.Ok:
                        return;
                    default:
                        return;
                }
            }

            var swipeManager = new ManagerSwipeWindow(header: "Other Discount", message: "Please ask the manager to swipe.", ButtonName: "Swipe");
            var (success, email) = await swipeManager.ShowDialogAsync(this);

            if (success)
            {
                var otherDiscount = new OtherDiscountWindow(email);
                await otherDiscount.ShowDialog((Window)this.VisualRoot);

                if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
                {
                    desktopLifetime.MainWindow?.Close();
                }
                return;
            }

        }
        private async void Manager_Click(object sender, RoutedEventArgs e)
        {
            if (OrderState.CurrentOrder.Count() > 1)
            {

                var dbox = MessageBoxManager.GetMessageBoxStandard(
                    new MessageBoxStandardParams
                    {
                        ContentHeader = "Action Blocked",
                        ContentMessage = "There is a pending order that must be completed before proceeding.",
                        ButtonDefinitions = ButtonEnum.Ok, // Defines the available buttons
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        CanResize = false,

                        SizeToContent = SizeToContent.WidthAndHeight,
                        Width = 400,
                        ShowInCenter = true,
                        Icon = MsBox.Avalonia.Enums.Icon.Error
                    });

                var dresult = await dbox.ShowAsPopupAsync(this);
                return;
            }

            var swipeManager = new ManagerSwipeWindow(header: "Manager Authorization", message: "Please enter your email.", ButtonName: "Submit");
            var (success, email) = await swipeManager.ShowDialogAsync(this);

            if (success)
            {
                var managerView = new ManagerWindow();

                if (Application.Current.ApplicationLifetime
                    is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow = managerView;
                }
                managerView.Show();
                Close();
            }
        }

        private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
        {
            // If we're not in barcode mode, let the event propagate normally
            if (!_isBarcodeMode)
            {
                return;
            }

            // Ignore modifier keys and function keys
            if (e.Key == Key.LWin || e.Key == Key.RWin || e.Key == Key.Tab ||
                e.Key == Key.F1 || e.Key == Key.F2 || e.Key == Key.F3 || e.Key == Key.F4 ||
                e.Key == Key.F5 || e.Key == Key.F6 || e.Key == Key.F7 || e.Key == Key.F8 ||
                e.Key == Key.F9 || e.Key == Key.F10 || e.Key == Key.F11 || e.Key == Key.F12)
            {
                return;
            }

            var currentTime = DateTime.Now;

            // If it's been too long since the last keypress, clear the buffer
            if ((currentTime - _lastKeyPress).TotalMilliseconds > BARCODE_TIMEOUT_MS)
            {
                _barcodeBuffer.Clear();
            }
            _lastKeyPress = currentTime;

            // Handle the key press
            if (e.Key == Key.Enter)
            {
                var barcode = _barcodeBuffer.ToString().Trim();
                _barcodeBuffer.Clear();

                if (!string.IsNullOrWhiteSpace(barcode))
                {
                    Debug.WriteLine($"Processing barcode: {barcode}");
                    _ = ProcessBarcode(barcode);
                }
                e.Handled = true;
            }
            else
            {
                // Add the character to the buffer
                var key = e.Key.ToString();
                if (key.Length == 1) // Single character key
                {
                    _barcodeBuffer.Append(key);
                }
                else if (e.Key >= Key.D0 && e.Key <= Key.D9) // Number keys
                {
                    _barcodeBuffer.Append((char)('0' + (e.Key - Key.D0)));
                }
                else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) // Numpad keys
                {
                    _barcodeBuffer.Append((char)('0' + (e.Key - Key.NumPad0)));
                }

                Debug.WriteLine($"Current buffer: {_barcodeBuffer}");
                e.Handled = true;
            }
        }

        private void BarcodeModeToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton)
            {
                bool isChecked = toggleButton.IsChecked ?? false;
                ToggleBarcodeMode(isChecked);

            }
        }

        // Update the ToggleBarcodeMode method to include visual feedback
        public void ToggleBarcodeMode(bool enable)
        {
            _isBarcodeMode = enable;
            Debug.WriteLine(_isBarcodeMode ? "True" : "false");
            if (BarcodeModeToggle != null)
            {
                BarcodeModeToggle.IsChecked = enable;
            }
            if (enable)
            {
                _barcodeBuffer.Clear();
            }
        }

        private async Task ProcessBarcode(string barcode)
        {
            IsLoadCtgry.IsVisible = true;
            IsLoadMenu.IsVisible = true;

            try
            {
                Debug.WriteLine($"Processing barcode: {barcode}");

                if (long.TryParse(barcode, out long prodId))
                {
                    var product = await _menu.GetProduct(prodId);

                    if (product != null)
                    {
                        Debug.WriteLine($"======>Product found: {product.MenuName}<======");


                        if (OrderState.CurrentOrderItem.Quantity < 1)
                        {
                            OrderState.CurrentOrderItem.Quantity = 1;
                        }

                        // Clear the current sub-orders.
                        OrderState.CurrentOrderItem.SubOrders.Clear();

                        // Update the order item with the appropriate type and details.
                        OrderState.UpdateItemOrder(
                            itemType: "Menu",
                            itemId: product.Id,
                            name: product.MenuName,
                            price: product.MenuPrice,
                            size: product.Size,
                            hasAddOn: product.HasAddOn,
                            hasDrink: product.HasDrink,
                            isVatZero: product.IsVatExempt
                        );

                        IsLoadMenu.IsVisible = true;
                        IsMenuAvail.IsVisible = false;
                        // Finalize the current order and exit.
                        await OrderState.FinalizeCurrentOrder(isSolo: true, this);


                        IsLoadMenu.IsVisible = false;
                        IsMenuAvail.IsVisible = true;

                        TenderState.tenderOrder.CalculateTotalAmount();
                        return;
                    }
                    else
                    {
                        var box = MessageBoxManager.GetMessageBoxStandard(
                            new MessageBoxStandardParams
                            {
                                ContentHeader = "Product Not Found",
                                ContentMessage = "The scanned product was not found or is unavailable.",
                                ButtonDefinitions = ButtonEnum.Ok,
                                Icon = MsBox.Avalonia.Enums.Icon.Error
                            });
                        await box.ShowAsPopupAsync(this);
                    }
                }
                else
                {
                    var box = MessageBoxManager.GetMessageBoxStandard(
                        new MessageBoxStandardParams
                        {
                            ContentHeader = "Invalid Input",
                            ContentMessage = "Please scan a valid product barcode.",
                            ButtonDefinitions = ButtonEnum.Ok,
                            Icon = MsBox.Avalonia.Enums.Icon.Error
                        });
                    await box.ShowAsPopupAsync(this);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing barcode: {ex}");
                var box = MessageBoxManager.GetMessageBoxStandard(
                    new MessageBoxStandardParams
                    {
                        ContentHeader = "Error",
                        ContentMessage = "An error occurred while processing the barcode.",
                        ButtonDefinitions = ButtonEnum.Ok,
                        Icon = MsBox.Avalonia.Enums.Icon.Error
                    });
                await box.ShowAsPopupAsync(this);
            }
            finally
            {
                IsLoadCtgry.IsVisible = false;
                IsLoadMenu.IsVisible = false;
            }
        }

        // Renamed event handlers to match XAML
        private void OnSearchBarGotFocus(object sender, GotFocusEventArgs e)
        {
            // When barcode scanner gets focus, make it hit-test visible
            if (sender is TextBox textBox)
            {
                textBox.IsHitTestVisible = true;
            }
        }

        private void OnSearchBarLostFocus(object sender, RoutedEventArgs e)
        {
            // When barcode scanner loses focus, make it hit-test invisible
            if (sender is TextBox textBox)
            {
                textBox.IsHitTestVisible = false;
            }
        }

    }
}
