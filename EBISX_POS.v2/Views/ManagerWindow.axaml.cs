using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using Microsoft.Extensions.DependencyInjection;
using EBISX_POS.Services;
using EBISX_POS.ViewModels;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using EBISX_POS.State;
using EBISX_POS.Models;
using System.Linq;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Options;
using EBISX_POS.Util;
using EBISX_POS.Views.Manager;
using EBISX_POS.ViewModels.Manager;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;
using Avalonia.Interactivity;
using EBISX_POS.v2.Views;
using EBISX_POS.Helper;
using EBISX_POS.API.Services.Interfaces;
using Avalonia.Threading;

namespace EBISX_POS.Views
{
    public partial class ManagerWindow : Window
    {
        private readonly string _cashTrackReportPath;

        private readonly IServiceProvider? _serviceProvider;
        private readonly MenuService _menuService;
        private readonly AuthService _authService;
        private readonly IJournal _journalService;

        // Constructor with IServiceProvider parameter
        public ManagerWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            DataContext = this;

            _serviceProvider = serviceProvider;

            // fetch only what we actually need
            _journalService = serviceProvider.GetRequiredService<IJournal>();
            _menuService = serviceProvider.GetRequiredService<MenuService>();
            _authService = serviceProvider.GetRequiredService<AuthService>();
            _cashTrackReportPath = serviceProvider
                                        .GetRequiredService<IOptions<SalesReport>>()
                                        .Value.CashTrackReport;

            // cache these checks so you only call them once
            bool hasManager = !string.IsNullOrWhiteSpace(CashierState.ManagerEmail);
            bool hasCashier = !string.IsNullOrWhiteSpace(CashierState.CashierEmail);

            // show overlay & hide BackButton only if nobody is signed in
            var overlayOn = !(hasManager || hasCashier);
            ButtonOverlay.IsVisible = overlayOn;

            BackButton.IsVisible = (hasManager || hasCashier);

            // enable SalesReport only for managers
            SalesReport.IsEnabled = hasManager;
            CashPullOut.IsEnabled = !hasManager;

            Mode.IsEnabled = hasManager;
            Mode.IsChecked = CashierState.IsTrainMode;

            DataLayout.IsVisible = hasManager || !(hasManager || hasCashier);

            // disable LogOut for managers (since they'd go back to login instead)
            LogOut.IsEnabled = !hasManager;

            PosInfo.IsEnabled = CashierState.ManagerEmail == "EBISX@POS.com";
            //LoadDataButton.IsEnabled = CashierState.ManagerEmail == "EBISX@POS.com";
            //PushDataButton.IsEnabled = CashierState.ManagerEmail == "EBISX@POS.com";
        }
        public ManagerWindow() : this(App.Current.Services.GetRequiredService<IServiceProvider>())
        { }
        // This constructor is required for Avalonia to instantiate the view in XAML.

        private void ShowLoader(bool show)
        {
            LoadingOverlay.IsVisible = show;
            if (show)
            {
                // Reset progress bar to indeterminate state
                LoadingProgressBar.IsIndeterminate = true;
                LoadingProgressBar.Value = 0;
                LoadingStatusText.Text = "Loading...";
            }
        }

        private void SalesReport_Button(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            //var reportWindow = _serviceProvider?.GetRequiredService<DailySalesReportView>();
            //reportWindow?.Show();
            //if (!string.IsNullOrWhiteSpace(CashierState.CashierEmail))
            //    return;

            //var swipeManager = new ManagerSwipeWindow(header: "Z Reading", message: "Please ask the manager to swipe.", ButtonName: "Swipe");
            //bool isSwiped = await swipeManager.ShowDialogAsync(this);

            //if (isSwiped)
            //{
            ShowLoader(true);
            ReceiptPrinterUtil.PrintZReading(_serviceProvider!);
            ShowLoader(false);
            //}

        }

        private void TransactionLog(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var reportWindow = _serviceProvider?.GetRequiredService<SalesHistoryWindow>();
            reportWindow?.ShowDialog(this);
        }

        private async void Cash_Track_Button(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {

            ShowLoader(true);
            var reportService = App.Current.Services.GetRequiredService<ReportService>();

            var (CashInDrawer, CurrentCashDrawer) = await reportService.CashTrack();

            // Ensure the target directory exists
            if (!Directory.Exists(_cashTrackReportPath))
            {
                Directory.CreateDirectory(_cashTrackReportPath);
            }

            // Define the file path
            string fileName = $"Cash-Track-{CashierState.CashierEmail}-{DateTimeOffset.UtcNow.ToString("MMMM-dd-yyyy-HH-mm-ss")}.txt";
            string filePath = Path.Combine(_cashTrackReportPath, fileName);

            string reportContent = $@"
                ==================================
                        Cash Track Report
                ==================================
                Cash In Drawer: {CashInDrawer:C}
                Total Cash Drawer: {CurrentCashDrawer:C}
                ";

            reportContent = string.Join("\n", reportContent.Split("\n").Select(line => line.Trim()));
            File.WriteAllText(filePath, reportContent);
            s
            //Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });

            string cashierEmail = CashierState.CashierEmail;
            string thermalPrinter = "POS";   // the exact name of your 58 mm printer
            string archiveFolder = _cashTrackReportPath; // or null/"" if you don't need archiving

            CashTrackPrinter.PrintCashTrackReport(
                cashierEmail,
                CashInDrawer,
                CurrentCashDrawer,
                thermalPrinter,
                archiveFolder
            );

            ShowLoader(false);
        }

        private void Back_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var isManager = !string.IsNullOrWhiteSpace(CashierState.ManagerEmail);

            // Only reset if they were a manager
            if (isManager)
                CashierState.CashierStateReset();

            // Pick which window to open
            Window nextWindow = isManager
                ? new LogInWindow()
                : new MainWindow()
                {
                    DataContext = new MainWindowViewModel(_menuService)
                };


            if (Application.Current.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = nextWindow;
            }

            nextWindow.Show();
            CashierState.ManagerEmail = null;
            Close();
        }


        private async void CashPullOut_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var setCashDrawer = new SetCashDrawerWindow("Withdraw");
            await setCashDrawer.ShowDialog(this);
        }
        private async void ManagerLog_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var managerLog = new UserLogsWindow();
            managerLog.DataContext = new UserLogsViewModel(true, managerLog);
            await managerLog.ShowDialog(this);
        }
        private async void CashierLog_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var managerLog = new UserLogsWindow();
            managerLog.DataContext = new UserLogsViewModel(false, managerLog);
            await managerLog.ShowDialog(this);
        }
        private async void LoadData_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ShowLoader(true);
            try
            {
                if (!await NetworkHelper.IsOnlineAsync())
                {
                    await ShowMessageAsync("No Internet Connection",
                                           "Please check your network and try again.");
                    return;  // loader will be hidden, window stays open
                }

                var (isSuccess, message) = await _authService.LoadDataAsync();
                if (!isSuccess)
                {
                    await ShowMessageAsync("Load Failed",
                                           string.IsNullOrWhiteSpace(message)
                                               ? "An unknown error occurred."
                                               : message);
                    return;
                }

                await PostLoadFlowAsync();
            }
            catch (Exception ex)
            {
                // log or report ex
                await ShowMessageAsync("Error", "Sorry, something went wrong.");
            }
            finally
            {
                ShowLoader(false);
            }
        }
        private async void PushData_Click(object? sender, RoutedEventArgs e)
        {
            ShowLoader(true);
            try
            {
                if (!await NetworkHelper.IsOnlineAsync())
                {
                    await ShowMessageAsync("No Internet Connection",
                                           "Please check your network and try again.");
                    return;  // loader will be hidden, window stays open
                }

                // Get today's date as default
                var today = DateTime.Today;
                var selectDateWindow = new SelectDateWindow();
                var selectedDate = await selectDateWindow.ShowDialogAsync(this, today);

                // Check if user cancelled the date selection
                if (!selectedDate.HasValue)
                {
                    ShowLoader(false);
                    return; // User cancelled
                }

                // Create progress reporter
                var progress = new Progress<(int current, int total, string status)>(update =>
                {
                    // Update UI on the UI thread
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (update.total > 0)
                        {
                            // Calculate percentage
                            var percentage = (double)update.current / update.total * 100;

                            // Update progress bar
                            LoadingProgressBar.IsIndeterminate = false;
                            LoadingProgressBar.Value = percentage;
                            LoadingProgressBar.Maximum = 100;

                            // Update status text
                            LoadingStatusText.Text = $"{update.status} ({percentage:F1}%)";
                        }
                        else
                        {
                            // Fallback to indeterminate if no total
                            LoadingProgressBar.IsIndeterminate = true;
                            LoadingStatusText.Text = update.status;
                        }
                    });
                });

                var (isSuccess, message) = await _journalService.PushAccountJournals(selectedDate.Value, progress);
                if (!isSuccess)
                {
                    await ShowMessageAsync("Push Failed",
                                           string.IsNullOrWhiteSpace(message)
                                               ? "An unknown error occurred."
                                               : message);
                    return;
                }
                Debug.WriteLine("Data pushed successfully to the server: " + message);
                await ShowMessageAsync("Success", $"Data for {selectedDate.Value:yyyy-MM-dd} pushed to the server successfully!");
            }
            catch (Exception ex)
            {
                // log or report ex
                await ShowMessageAsync("Error", "Sorry, something went wrong.");
            }
            finally
            {
                ShowLoader(false);
            }
        }

        private async Task PostLoadFlowAsync()
        {
            // if manager email is set, ask if they want to log in as cashier
            if (!string.IsNullOrWhiteSpace(CashierState.ManagerEmail))
            {
                var result = await MessageBoxManager
                   .GetMessageBoxStandard(new MessageBoxStandardParams
                   {
                       ContentHeader = "Data Updated!",
                       ContentMessage = "Data loaded successfully. Log in as cashier now?",
                       ButtonDefinitions = ButtonEnum.OkCancel,
                       WindowStartupLocation = WindowStartupLocation.CenterOwner,
                       CanResize = false,
                       SizeToContent = SizeToContent.WidthAndHeight,
                       Width = 400,
                       ShowInCenter = true,

                   })
                   .ShowAsPopupAsync(this);

                if (result != ButtonResult.Ok)
                    return;
                CashierState.CashierStateReset();
            }

            ShowLoginAndClose();
        }

        private void ShowLoginAndClose()
        {
            var loginWin = new LogInWindow();
            if (Application.Current.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = loginWin;
            }
            loginWin.Show();
            this.Close();
        }

        private Task<ButtonResult> ShowMessageAsync(string header, string message)
            => MessageBoxManager
                .GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentHeader = header,
                    ContentMessage = message,
                    ButtonDefinitions = ButtonEnum.Ok,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Width = 400,
                    ShowInCenter = true,
                    SystemDecorations = SystemDecorations.None
                })
                .ShowWindowDialogAsync(this);

        private async void Refund_Click(object? sender, RoutedEventArgs e)
        {
            //var refundOrder = new SetCashDrawerWindow("Returned");
            //await refundOrder.ShowDialog(this);

            var refundItems = new RefundItemWindow();
            await refundItems.ShowDialog(this);
        }

        private async void LogOut_Button(object? sender, RoutedEventArgs e)
        {
            // 1. Early‑exit if there are pending items
            if (OrderState.CurrentOrder.Any())
            {
                await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentHeader = "Error",
                    ContentMessage = "Unable to log out – there is a pending order.",
                    ButtonDefinitions = ButtonEnum.Ok,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Width = 400,
                    ShowInCenter = true,
                    Icon = MsBox.Avalonia.Enums.Icon.Error
                }).ShowAsPopupAsync(this);

                return;
            }

            ShowLoader(true);
            try
            {
                // 2. Manager authorization step
                var swipeManager = new ManagerSwipeWindow(
                    header: "Manager Authorization",
                    message: "Please enter your manager email.",
                    ButtonName: "Submit"
                );

                var (authorized, managerEmail) = await swipeManager.ShowDialogAsync(this);
                if (!authorized)
                    return;

                // 3. Cash‑out step
                var cashOutWindow = new SetCashDrawerWindow("Cash-Out");
                bool drawerOk = await cashOutWindow.ShowDialog<bool>(this);

                if (!drawerOk)
                {
                    // user canceled or operation failed
                    ShowLoader(false);
                    return;
                }

                // 4. Print X‑reading
                ReceiptPrinterUtil.PrintXReading(_serviceProvider!);

                // 5. Call the API to log out
                var (logoutSuccess, logoutMessage) = await _authService.LogOut(managerEmail);
                if (!logoutSuccess)
                {
                    await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                    {
                        ContentHeader = "Logout Failed",
                        ContentMessage = logoutMessage,
                        ButtonDefinitions = ButtonEnum.Ok,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Icon = MsBox.Avalonia.Enums.Icon.Error
                    }).ShowAsPopupAsync(this);
                    return;
                }

                // 6. Clear application state
                CashierState.CashierName = null;
                CashierState.CashierEmail = null;
                OrderState.CurrentOrder.Clear();
                OrderState.CurrentOrderItem = new OrderItemState();
                TenderState.tenderOrder.Reset();

                // 7. Swap to login window
                var loginWindow = new LogInWindow();
                if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow = loginWindow;
                }
                loginWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Logout error: {ex}");
                await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentHeader = "Unexpected Error",
                    ContentMessage = "An error occurred while logging out. Please try again.",
                    ButtonDefinitions = ButtonEnum.Ok,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Icon = MsBox.Avalonia.Enums.Icon.Error
                }).ShowAsPopupAsync(this);
            }
            finally
            {
                ShowLoader(false);
            }
        }
        private async void ChangeMode_Button(object? sender, RoutedEventArgs e)
        {
            ShowLoader(true);
            if (string.IsNullOrWhiteSpace(CashierState.ManagerEmail))
            {
                await MessageBoxManager.GetMessageBoxStandard(
                    new MessageBoxStandardParams
                    {
                        ContentHeader = $"Error",
                        ContentMessage = "Unable to change mode – invalid credential.",
                        ButtonDefinitions = ButtonEnum.Ok, // Defines the available buttons
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        CanResize = false,
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Width = 400,
                        ShowInCenter = true,
                        Icon = MsBox.Avalonia.Enums.Icon.Error,
                        SystemDecorations = SystemDecorations.None
                    }).ShowAsPopupAsync(this);
                return;
            }

            var isTrainMode = await _authService.ChangeModeAsync(CashierState.ManagerEmail);
            CashierState.IsTrainMode = isTrainMode;
            Mode.IsChecked = CashierState.IsTrainMode;
            ShowLoader(false);
        }

        private void OnBtnClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            switch (btn.Tag as string)
            {
                case "User": OpenUser(); return;
                case "MenuTypes": OpenMenuTypes(); return;
                case "Category": OpenCategory(); return;
                case "Menu": OpenMenu(); return;
                case "CouponAndPromo": OpenCouponAndPromo(); return;
                case "PosTerminalInfo": OpenPosTerminalInfo(); return;
            }
        }

        void OpenUser()
        {
            var userWindow = new AppUsersWindow();
            userWindow.ShowDialog(this);
        }
        void OpenMenuTypes()
        {
            var menuTypesWindow = new DrinkAndAddOnTypeWindow();
            menuTypesWindow.ShowDialog(this);
        }
        void OpenCategory()
        {
            var categoryWindow = new CategoryWindow();
            categoryWindow.ShowDialog(this);
        }
        void OpenMenu()
        {
            var menuWindow = new MenuWindow();
            menuWindow.ShowDialog(this);
        }
        void OpenCouponAndPromo()
        {
            var couponPromoWindow = new CouponPromoWindow();
            couponPromoWindow.ShowDialog(this);
        }
        void OpenPosTerminalInfo()
        {
            var posTerminalInfoWindow = new PosTerminalInfoView();
            posTerminalInfoWindow.ShowDialog(this);
        }
    }
}