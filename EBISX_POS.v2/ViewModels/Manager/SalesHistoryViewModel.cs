using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using EBISX_POS.Services;
using EBISX_POS.Services.DTO.Report;
using EBISX_POS.API.Services.Interfaces;
using Microsoft.Extensions.Options;
using MsBox.Avalonia;
using Avalonia.Controls;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using Tmds.DBus.Protocol;

namespace EBISX_POS.ViewModels.Manager
{
    public partial class SalesHistoryViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<InvoiceDTO> salesHistoryList = new();

        [ObservableProperty]
        private DateTimeOffset fromDate = DateTime.Today;

        [ObservableProperty]
        private DateTimeOffset toDate = DateTime.Today;

        public IRelayCommand FilterCommand { get; }
        public IRelayCommand PrintCommand { get; }
        public IRelayCommand PrintSalesCommand { get; }

        private readonly List<InvoiceDTO> allSalesHistory; 
        
        [ObservableProperty]
        private InvoiceDTO? selectedInvoice;

        [ObservableProperty]
        private bool isLoading = false;

        private const int PageSize = 10;

        [ObservableProperty]
        private int totalPages = 1;

        [ObservableProperty]
        private int currentPage = 1;

        [ObservableProperty]
        private ObservableCollection<InvoiceDTO> paginatedSalesHistoryList = new();

        public IRelayCommand NextPageCommand { get; }
        public IRelayCommand PreviousPageCommand { get; }

        private readonly Window _window;

        public SalesHistoryViewModel(Window window)
        {
            var reportService = App.Current.Services.GetRequiredService<ReportService>();
            _window = window;

            allSalesHistory = new List<InvoiceDTO>(); // Temp until data is fetched

            FilterCommand = new RelayCommand(async () => await FilterByDateRange());
            PrintCommand = new RelayCommand(async () => await PrintTranxList());
            PrintSalesCommand = new RelayCommand(async () => await PrintSalesList());
            NextPageCommand = new RelayCommand(GoToNextPage, CanGoToNextPage);
            PreviousPageCommand = new RelayCommand(GoToPreviousPage, CanGoToPreviousPage);

            // Optionally fetch records for today as default
            _ = LoadInvoicesAsync(DateTime.Today, DateTime.Today);
        }

        private void GoToNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                UpdatePaginatedList();
            }
        }

        private void GoToPreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                UpdatePaginatedList();
            }
        }
        private void UpdatePaginatedList()
        {
            var paginated = allSalesHistory
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            PaginatedSalesHistoryList = new ObservableCollection<InvoiceDTO>(paginated);
            TotalPages = (int)Math.Ceiling((double)allSalesHistory.Count / PageSize);

            NextPageCommand.NotifyCanExecuteChanged();
            PreviousPageCommand.NotifyCanExecuteChanged();
        }

        private bool CanGoToNextPage() => CurrentPage < TotalPages;
        private bool CanGoToPreviousPage() => CurrentPage > 1;

        private async Task LoadInvoicesAsync(DateTime from, DateTime to)
        {
            IsLoading = true;
            var reportService = App.Current.Services.GetRequiredService<ReportService>();
            var invoices = await reportService.GetInvoicesByDateRange(from, to);

            allSalesHistory.Clear();
            allSalesHistory.AddRange(invoices ?? new List<InvoiceDTO>());

            CurrentPage = 1;
            UpdatePaginatedList();

            IsLoading = false;
        }

        private async Task FilterByDateRange()
        {
            await LoadInvoicesAsync(FromDate.Date, ToDate.Date);
        }

        private async Task PrintTranxList()
        {
            IsLoading = true;

            var reportService = App.Current.Services.GetRequiredService<IReport>();
            var reportOptions = App.Current.Services.GetRequiredService<IOptions<SalesReport>>();

            var tranxPath = reportOptions.Value.TransactionLogsFolder;

            await reportService.GetTransactList(fromDate: FromDate.Date, toDate: ToDate.Date, tranxPath);


            var msgBox = MessageBoxManager
                .GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = "Transaction List Saved",
                    ContentMessage = $"Your transaction list was saved to:\n{tranxPath}",
                    Icon = Icon.Success
                });

            await msgBox.ShowAsPopupAsync(_window);

            IsLoading = false;
        }

        private async Task PrintSalesList()
        {
            IsLoading = true;

            var reportService = App.Current.Services.GetRequiredService<IReport>();
            var reportOptions = App.Current.Services.GetRequiredService<IOptions<SalesReport>>();

            var salesPath = reportOptions.Value.SalesLogsFolder;

            await reportService.GetSalesReport(fromDate: FromDate.Date, toDate: ToDate.Date, salesPath);


            var msgBox = MessageBoxManager
                .GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = "Sales List Saved",
                    ContentMessage = $"Your sales list was saved to:\n{salesPath}",
                    Icon = Icon.Success
                });

            await msgBox.ShowAsPopupAsync(_window);

            IsLoading = false;
        }
    }
}
