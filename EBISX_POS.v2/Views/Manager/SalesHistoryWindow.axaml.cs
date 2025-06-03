using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EBISX_POS.ViewModels.Manager;
using Microsoft.Extensions.Configuration;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using EBISX_POS.Services;
using EBISX_POS.Util;
using EBISX_POS.API.Models;
using EBISX_POS.Services.DTO.Report;
using EBISX_POS.Helper;
using System.Threading.Tasks;
using EBISX_POS.API.Services.Interfaces;

namespace EBISX_POS.Views.Manager
{

    public partial class SalesHistoryWindow : Window
    {
        private readonly IConfiguration _configuration;
        //public SalesHistoryWindow()
        public SalesHistoryWindow(IConfiguration configuration)
        {

            InitializeComponent();
            DataContext = new SalesHistoryViewModel(this);
            var dataService = App.Current.Services.GetRequiredService<IData>();

            _configuration = configuration;

        }

        private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }

        private async void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Get the ViewModel from DataContext
            var viewModel = DataContext as SalesHistoryViewModel;
            if (sender is Button button && button.Tag is InvoiceDTO printInvoice)
            {

                if (viewModel != null)
                {
                    // Access the selectedInvoice
                    var selectedInvoice = printInvoice;
                    //var selectedInvoice = viewModel.SelectedInvoice;

                    // Perform actions based on the selected invoice, for example:
                    if (selectedInvoice != null)
                    {
                        ShowLoader(true);

                        // Do something with the selectedInvoice, like generating a report
                        var reportOptions = App.Current.Services.GetRequiredService<IOptions<SalesReport>>();
                        var reportService = App.Current.Services.GetRequiredService<ReportService>();

                        var invoice = await reportService.GetInvoiceById(selectedInvoice.InvoiceNum);

                        string folderPath = reportOptions.Value.SearchedInvoice;
                        string fileName = $"Receipt-{DateTimeOffset.UtcNow.ToString("MMMM-dd-yyyy-HH-mm-ss")}.txt";
                        string filePath = Path.Combine(folderPath, fileName);

                        ReceiptPrinterUtil.PrintSearchedInvoice(folderPath, filePath, invoice, selectedInvoice.InvoiceStatus);
                        ShowLoader(false);

                    }
                    else
                    {
                        // Handle the case when no invoice is selected
                        await MessageBoxManager.GetMessageBoxStandard("No Invoice Selected", "Please select an invoice.", ButtonEnum.Ok)
                            .ShowAsPopupAsync(this);
                    }
                }
            }
        }

        private void ShowLoader(bool show)
        {
            LoadingOverlay.IsVisible = show;
        }

    }
};