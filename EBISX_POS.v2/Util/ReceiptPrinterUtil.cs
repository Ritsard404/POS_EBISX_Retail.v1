using EBISX_POS.API.Services.DTO.Order;
using EBISX_POS.API.Services.DTO.Report;
using EBISX_POS.Helper;
using EBISX_POS.Services;
using EBISX_POS.Services.DTO.Report;
using EBISX_POS.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace EBISX_POS.Util
{
    public static class ReceiptPrinterUtil
    {
        private const int ReceiptWidth = 32;
        private const int QtyWidth = 3;
        private const int DescWidth = 20;
        private const int AmountWidth = 9;
        private static readonly CultureInfo PesoCulture = new CultureInfo("en-PH");
        private const string PrinterName = "POS"; // Your thermal printer name

        private static string CenterText(string text) =>
            text.PadLeft((ReceiptWidth + text.Length) / 2).PadRight(ReceiptWidth);

        private static string AlignText(string left, string right) =>
            left.PadRight(ReceiptWidth - (right ?? "0").Length) + (right ?? "0");

        private static string FormatItemLine(string qty, string desc, string amount)
        {
            return $"{qty.PadRight(QtyWidth)}{desc.PadRight(DescWidth)}{amount.PadLeft(AmountWidth)}";
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path), "Report directory path cannot be null or empty");
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void PrintToPrinter(StringBuilder content)
        {
            // Add line feeds for paper cutting
            content.AppendLine("\n\n\n");
            RawPrinterHelper.PrintText(PrinterName, content.ToString());
        }

        public static async void PrintXReading(IServiceProvider serviceProvider)
        {
            var reportOptions = serviceProvider.GetRequiredService<IOptions<SalesReport>>();
            var reportService = serviceProvider.GetRequiredService<ReportService>();

            var rpt = await reportService.XInvoiceReport();
            var reportPath = reportOptions.Value.XInvoiceReport;

            EnsureDirectoryExists(reportPath);

            string fileName = $"XInvoice-{DateTime.UtcNow.ToString("MMMM-dd-yyyy-HH-mm-ss")}.txt";
            var filePath = Path.Combine(reportPath, fileName);

            var content = new StringBuilder();

            // Header
            content.AppendLine(CenterText(rpt.BusinessName));
            content.AppendLine(CenterText($"Operated by: {rpt.OperatorName}"));
            content.AppendLine();
            content.AppendLine(CenterText(rpt.AddressLine));
            content.AppendLine();
            content.AppendLine(CenterText($"VAT REG TIN: {rpt.VatRegTin}"));
            content.AppendLine(CenterText($"MIN: {rpt.Min}"));
            content.AppendLine(CenterText($"S/N: {rpt.SerialNumber}"));
            content.AppendLine();

            // Title
            content.AppendLine(CenterText("X-READING REPORT"));
            content.AppendLine();

            // Report date/time
            content.AppendLine(AlignText("Report Date:", rpt.ReportDate));
            content.AppendLine(AlignText("Report Time:", rpt.ReportTime));
            content.AppendLine();

            // Period
            content.AppendLine(AlignText("Start Date/Time:", rpt.StartDateTime));
            content.AppendLine(AlignText("End Date/Time:", rpt.EndDateTime));
            content.AppendLine();

            // Cashier & OR
            content.AppendLine(AlignText($"Cashier: {rpt.Cashier}", ""));
            content.AppendLine();
            content.AppendLine(AlignText("Beg. OR #:", rpt.BeginningOrNumber));
            content.AppendLine(AlignText("End. OR #:", rpt.EndingOrNumber));
            content.AppendLine(AlignText("Txn Count #:", rpt.TransactCount));
            content.AppendLine();

            // Opening fund
            content.AppendLine(AlignText("Opening Fund:", rpt.OpeningFund));
            content.AppendLine(new string('=', ReceiptWidth));

            // Payments section
            content.AppendLine(CenterText("PAYMENTS RECEIVED"));
            content.AppendLine();
            content.AppendLine(AlignText("CASH", rpt.Payments.CashString));
            if (rpt.Payments.OtherPayments != null)
            {
                foreach (var p in rpt.Payments.OtherPayments)
                {
                    content.AppendLine(AlignText(p.Name.ToUpper(), p.AmountString));
                }
            }
            content.AppendLine(AlignText("Total Payments:", rpt.Payments.Total));
            content.AppendLine(new string('=', ReceiptWidth));

            // Void / Refund / Withdrawal
            content.AppendLine(AlignText($"VOID ({rpt.VoidCount})", rpt.VoidAmount));
            content.AppendLine(new string('=', ReceiptWidth));
            content.AppendLine(AlignText($"REFUND ({rpt.RefundCount})", rpt.Refund));
            content.AppendLine(new string('=', ReceiptWidth));
            content.AppendLine(AlignText("WITHDRAWAL", rpt.Withdrawal));
            content.AppendLine(new string('=', ReceiptWidth));

            // Transaction summary
            content.AppendLine(CenterText("TRANSACTION SUMMARY"));
            content.AppendLine();
            content.AppendLine(AlignText("Cash In Drawer:", rpt.TransactionSummary.CashInDrawer));
            foreach (var p in rpt.TransactionSummary.OtherPayments)
            {
                content.AppendLine(AlignText(p.Name.ToUpper(), p.AmountString));
            }
            content.AppendLine(new string('=', ReceiptWidth));

            // Short/Over
            content.AppendLine(AlignText("SHORT/OVER:", rpt.ShortOver));
            content.AppendLine();

            // Save to file
            File.WriteAllText(filePath, content.ToString());

            // Print to thermal printer
            //PrintToPrinter(content);

            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }

        public static async void PrintZReading(IServiceProvider serviceProvider)
        {
            var reportOptions = serviceProvider.GetRequiredService<IOptions<SalesReport>>();
            var reportService = serviceProvider.GetRequiredService<ReportService>();

            var rpt = await reportService.ZInvoiceReport();
            var reportPath = reportOptions.Value.ZInvoiceReport;

            EnsureDirectoryExists(reportPath);

            string fileName = $"ZInvoice-{DateTimeOffset.UtcNow:MMMM-dd-yyyy-HH-mm-ss}.txt";
            var filePath = Path.Combine(reportPath, fileName);

            var content = new StringBuilder();

            // Header
            content.AppendLine(CenterText(rpt.BusinessName));
            content.AppendLine(CenterText($"Operated by: {rpt.OperatorName}"));
            content.AppendLine(CenterText(rpt.AddressLine));
            content.AppendLine(CenterText($"VAT REG TIN: {rpt.VatRegTin}"));
            content.AppendLine(CenterText($"MIN: {rpt.Min}"));
            content.AppendLine(CenterText($"S/N: {rpt.SerialNumber}"));
            content.AppendLine();

            // Title
            content.AppendLine(CenterText("Z-READING REPORT"));
            content.AppendLine();

            // Report date/time
            content.AppendLine(AlignText("Report Date:", rpt.ReportDate));
            content.AppendLine(AlignText("Report Time:", rpt.ReportTime));
            content.AppendLine();

            // Period
            content.AppendLine(AlignText("Start Date/Time:", rpt.StartDateTime));
            content.AppendLine(AlignText("End Date/Time:", rpt.EndDateTime));
            content.AppendLine();

            // SI/VOID/RETURN numbers
            content.AppendLine(AlignText("Beg. SI #:", rpt.BeginningSI));
            content.AppendLine(AlignText("End. SI #:", rpt.EndingSI));
            content.AppendLine(AlignText("Beg. VOID #:", rpt.BeginningVoid));
            content.AppendLine(AlignText("End. VOID #:", rpt.EndingVoid));
            content.AppendLine(AlignText("Beg. RETURN #:", rpt.BeginningReturn));
            content.AppendLine(AlignText("End. RETURN #:", rpt.EndingReturn));
            content.AppendLine();

            content.AppendLine(AlignText("Txn Count #:", rpt.TransactCount));
            content.AppendLine(AlignText("Reset Counter No.:", rpt.ResetCounter));
            content.AppendLine(AlignText("Z Counter No.:", rpt.ZCounter));
            content.AppendLine(new string('-', ReceiptWidth));

            // Sales section
            content.AppendLine(AlignText("Accum. Sales:", rpt.PresentAccumulatedSales));
            content.AppendLine(AlignText("Prev. Accum. Sales:", rpt.PreviousAccumulatedSales));
            content.AppendLine(AlignText("Sales for the Day:", rpt.SalesForTheDay));
            content.AppendLine(new string('-', ReceiptWidth));

            // Breakdown of sales
            content.AppendLine(CenterText("BREAKDOWN OF SALES"));
            content.AppendLine();
            content.AppendLine(AlignText("VATABLE SALES:", rpt.SalesBreakdown.VatableSales));
            content.AppendLine(AlignText("VAT AMOUNT:", rpt.SalesBreakdown.VatAmount));
            content.AppendLine(AlignText("VAT EXEMPT SALES:", rpt.SalesBreakdown.VatExemptSales));
            content.AppendLine(AlignText("ZERO RATED SALES:", rpt.SalesBreakdown.ZeroRatedSales));
            content.AppendLine(new string('-', ReceiptWidth));
            content.AppendLine(AlignText("Gross Amount:", rpt.SalesBreakdown.GrossAmount));
            content.AppendLine(AlignText("Less Discount:", rpt.SalesBreakdown.LessDiscount));
            content.AppendLine(AlignText("Less Return:", rpt.SalesBreakdown.LessReturn));
            content.AppendLine(AlignText("Less Void:", rpt.SalesBreakdown.LessVoid));
            content.AppendLine(AlignText("Less VAT Adjustment:", rpt.SalesBreakdown.LessVatAdjustment));
            content.AppendLine(AlignText("Net Amount:", rpt.SalesBreakdown.NetAmount));
            content.AppendLine(new string('-', ReceiptWidth));

            // Discounts
            content.AppendLine(CenterText("DISCOUNT SUMMARY"));
            content.AppendLine(AlignText($"SC Disc. ({rpt.DiscountSummary.SeniorCitizenCount}):", rpt.DiscountSummary.SeniorCitizen));
            content.AppendLine(AlignText($"PWD Disc. ({rpt.DiscountSummary.PwdCount}):", rpt.DiscountSummary.Pwd));
            content.AppendLine(AlignText($"Other Disc. ({rpt.DiscountSummary.OtherCount}):", rpt.DiscountSummary.Other));
            content.AppendLine(new string('-', ReceiptWidth));

            // Adjustments
            content.AppendLine(CenterText("SALES ADJUSTMENT"));
            content.AppendLine(AlignText($"VOID ({rpt.SalesAdjustment.VoidCount}):", rpt.SalesAdjustment.Void));
            content.AppendLine(AlignText($"RETURN ({rpt.SalesAdjustment.ReturnCount}):", rpt.SalesAdjustment.Return));
            content.AppendLine(new string('-', ReceiptWidth));

            content.AppendLine(CenterText("VAT ADJUSTMENT"));
            content.AppendLine(AlignText("SC TRANS. :", rpt.VatAdjustment.ScTrans));
            content.AppendLine(AlignText("PWD TRANS :", rpt.VatAdjustment.PwdTrans));
            content.AppendLine(AlignText("REG.Disc. TRANS :", rpt.VatAdjustment.RegDiscTrans));
            content.AppendLine(AlignText("ZERO-RATED TRANS.:", rpt.VatAdjustment.ZeroRatedTrans));
            content.AppendLine(AlignText("VAT on Return:", rpt.VatAdjustment.VatOnReturn));
            content.AppendLine(AlignText("Other VAT Adjustments:", rpt.VatAdjustment.OtherAdjustments));
            content.AppendLine(new string('-', ReceiptWidth));

            // Transaction Summary
            content.AppendLine(CenterText("TRANSACTION SUMMARY"));
            content.AppendLine();
            content.AppendLine(AlignText("Cash In Drawer:", rpt.TransactionSummary.CashInDrawer));
            foreach (var p in rpt.TransactionSummary.OtherPayments)
            {
                content.AppendLine(AlignText(p.Name.ToUpper(), p.AmountString));
            }
            content.AppendLine(AlignText("Opening Fund:", rpt.OpeningFund));
            content.AppendLine(AlignText("Less Withdrawal:", rpt.Withdrawal));
            content.AppendLine(AlignText("Payments Received:", rpt.PaymentsReceived));
            content.AppendLine(new string('-', ReceiptWidth));

            // Short/Over
            content.AppendLine(AlignText("SHORT/OVER:", rpt.ShortOver));
            content.AppendLine();

            // Save to file
            File.WriteAllText(filePath, content.ToString());

            // Print to thermal printer
            //PrintToPrinter(content);

            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }

        public static void PrintInvoice(string folderPath, string filePath, FinalizeOrderResponseDTO finalizeOrder)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                throw new ArgumentNullException(nameof(folderPath), "Receipt folder path cannot be null or empty");
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath), "Receipt file path cannot be null or empty");
            }

            EnsureDirectoryExists(folderPath);

            var content = new StringBuilder();

            // Header
            content.AppendLine(new string('=', ReceiptWidth));
            //content.AppendLine(CenterText("INVOICE"));
            content.AppendLine(CenterText("Acknowledgment Reciept"));
            content.AppendLine(new string('=', ReceiptWidth));
            //content.AppendLine(CenterText(finalizeOrder.RegisteredName));
            //content.AppendLine(CenterText(finalizeOrder.Address));
            //content.AppendLine(CenterText($"TIN: {finalizeOrder.VatTinNumber}"));
            //content.AppendLine(CenterText($"MIN: {finalizeOrder.MinNumber}"));
            //content.AppendLine(new string('-', ReceiptWidth));
            content.AppendLine(CenterText("Acknowledgment Reciept"));
            content.AppendLine();

            // Invoice details
            content.AppendLine($"INV: {finalizeOrder.InvoiceNumber}".PadRight(ReceiptWidth));
            content.AppendLine(CenterText(TenderState.tenderOrder.OrderType));
            content.AppendLine($"Date: {finalizeOrder.InvoiceDate:d}".PadRight(ReceiptWidth));
            content.AppendLine($"Cashier: {CashierState.CashierName}".PadRight(ReceiptWidth));
            content.AppendLine(new string('-', ReceiptWidth));

            // Items header
            content.AppendLine(FormatItemLine("Qty", "Description", "Amount"));
            content.AppendLine(new string('-', ReceiptWidth));
            content.AppendLine();

            // Invoice items
            foreach (var order in OrderState.CurrentOrder)
            {
                foreach (var item in order.DisplaySubOrders)
                {
                    string quantityColumn = item.Opacity < 1.0
                        ? new string(' ', QtyWidth)
                        : $"{item.Quantity}";

                    string displayNameColumn = item.DisplayName.Length > DescWidth
                        ? item.DisplayName.Substring(0, DescWidth - 3) + "..."
                        : item.DisplayName;

                    string priceColumn = item.IsUpgradeMeal ? item.ItemPriceString : string.Empty;

                    content.AppendLine(FormatItemLine(quantityColumn, displayNameColumn, priceColumn));
                }
                content.AppendLine();
            }
            content.AppendLine(new string('-', ReceiptWidth));

            // Totals
            content.AppendLine(CenterText($"{"Total:",-15}{TenderState.tenderOrder.TotalAmount.ToString("C", PesoCulture),17}"));
            if (TenderState.tenderOrder.HasOrderDiscount)
            {
                string discountLabel = TenderState.tenderOrder.PromoDiscountAmount > 0m
                    ? $"Disc ({TenderState.tenderOrder.PromoDiscountName}):"
                    : "Discount:";

                content.AppendLine(CenterText($"{discountLabel,-15}{"-" + TenderState.tenderOrder.DiscountAmount.ToString("C", PesoCulture),17}"));
            }

            content.AppendLine(CenterText($"{"Due:",-15}{TenderState.tenderOrder.AmountDue.ToString("C", PesoCulture),17}"));

            if (TenderState.tenderOrder.HasOtherPayments && TenderState.tenderOrder.OtherPayments != null)
            {
                foreach (var payment in TenderState.tenderOrder.OtherPayments)
                {
                    content.AppendLine(CenterText($"{payment.SaleTypeName + ":",-15}{payment.Amount.ToString("C", PesoCulture),17}"));
                }
            }
            content.AppendLine(CenterText($"{"Cash:",-15}{TenderState.tenderOrder.CashTenderAmount.ToString("C", PesoCulture),17}"));
            content.AppendLine(CenterText($"{"Total:",-15}{TenderState.tenderOrder.TenderAmount.ToString("C", PesoCulture),17}"));
            content.AppendLine(CenterText($"{"Change:",-15}{TenderState.tenderOrder.ChangeAmount.ToString("C", PesoCulture),17}"));
            content.AppendLine();

            content.AppendLine(CenterText($"{"VAT Zero:",-15}{TenderState.tenderOrder.VatZero.ToString("C", PesoCulture),17}"));
            content.AppendLine(CenterText($"{"VAT Exempt:",-15}{(TenderState.tenderOrder.VatExemptSales).ToString("C", PesoCulture),17}"));
            content.AppendLine(CenterText($"{"VAT Sales:",-15}{(TenderState.tenderOrder.VatSales).ToString("C", PesoCulture),17}"));
            content.AppendLine(CenterText($"{"VAT Amt:",-15}{(TenderState.tenderOrder.VatAmount).ToString("C", PesoCulture),17}"));
            content.AppendLine();

            if (TenderState.ElligiblePWDSCDiscount == null || !TenderState.ElligiblePWDSCDiscount.Any())
            {
                content.AppendLine(CenterText("Name:_________________"));
                content.AppendLine(CenterText("Address:______________"));
                content.AppendLine(CenterText("TIN: _________________"));
                content.AppendLine(CenterText("Signature: ___________"));
                content.AppendLine();
            }
            else
            {
                var names = (TenderState.ElligiblePWDSCDiscount?.Any() == true)
                    ? TenderState.ElligiblePWDSCDiscount
                    : new List<string> { string.Empty };

                foreach (var pwdSc in names)
                {
                    string nameText = $"Name: {pwdSc.ToUpper()}";
                    content.AppendLine(nameText);
                    content.AppendLine("Address: _____________");
                    content.AppendLine("TIN: ________________");
                    content.AppendLine("Signature: __________");
                    content.AppendLine();
                }
            }

            // Footer
            //content.AppendLine(CenterText("Sales Invoice"));
            //content.AppendLine(CenterText("Arsene Software"));
            //content.AppendLine(CenterText("Labangon, Cebu"));
            //content.AppendLine(CenterText($"TIN: {finalizeOrder.VatTinNumber}"));
            //content.AppendLine(CenterText($"Issued: {finalizeOrder.DateIssued:d}"));
            //content.AppendLine(CenterText($"Valid: {finalizeOrder.ValidUntil:d}"));
            //content.AppendLine();
            //content.AppendLine(new string('=', ReceiptWidth));
            //content.AppendLine(CenterText("Thank you!"));
            //content.AppendLine(new string('=', ReceiptWidth));
            content.AppendLine();

            if (TenderState.tenderOrder.HasOrderDiscount || TenderState.ElligiblePWDSCDiscount?.Any() == true || TenderState.tenderOrder.HasOtherPayments)
            {
                // Store original content once
                string baseContent = content.ToString();

                foreach (var label in new[] { "", "COPY" })
                {
                    // Create a fresh builder for each output
                    var contentWithLabel = new StringBuilder();

                    // Add label if not empty
                    if (!string.IsNullOrWhiteSpace(label))
                    {
                        contentWithLabel.AppendLine(CenterText($"*** {label} ***"));
                    }

                    contentWithLabel.Append(baseContent);

                    var baseName = Path.GetFileNameWithoutExtension(filePath);
                    var ext = Path.GetExtension(filePath);

                    var outName = string.IsNullOrWhiteSpace(label)
                        ? $"{baseName}{ext}"
                        : $"{baseName}_{label}{ext}";

                    var outPath = Path.Combine(folderPath, outName);

                    File.WriteAllText(outPath, contentWithLabel.ToString());
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });

                    // Print to thermal printer
                    //PrintToPrinter(contentWithLabel);
                }
            }
            else
            {

                // Save to file
                File.WriteAllText(filePath, content.ToString());
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });

                // Print to thermal printer
                //PrintToPrinter(content);
            }

        }

        public static void PrintSearchedInvoice(string folderPath, string filePath, InvoiceDetailsDTO invoice, string status)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                throw new ArgumentNullException(nameof(folderPath), "Searched invoice folder path cannot be null or empty");
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath), "Searched invoice file path cannot be null or empty");
            }

            EnsureDirectoryExists(folderPath);

            var content = new StringBuilder();

            // Header
            content.AppendLine(new string('=', ReceiptWidth));
            content.AppendLine(CenterText("Acknowledgment Reciept"));
            content.AppendLine();
            var invoiceTitle = "INVOICE" + (status != "Paid" ? $" {status.ToUpper()}" : "");
            content.AppendLine(CenterText(invoiceTitle));
            content.AppendLine(new string('=', ReceiptWidth));
            //content.AppendLine(CenterText(invoice.RegisteredName));
            //content.AppendLine(CenterText(invoice.Address));
            //content.AppendLine(CenterText($"TIN: {invoice.VatTinNumber}"));
            //content.AppendLine(CenterText($"MIN: {invoice.MinNumber}"));
            content.AppendLine(CenterText("Acknowledgment Reciept"));
            content.AppendLine(new string('-', ReceiptWidth));
            content.AppendLine();

            // Invoice details
            content.AppendLine($"INV: {invoice.InvoiceNum}".PadRight(ReceiptWidth));
            content.AppendLine(CenterText(invoice.OrderType));
            content.AppendLine($"Date: {invoice.InvoiceDate:d}".PadRight(ReceiptWidth));
            content.AppendLine($"Cashier: {invoice.CashierName}".PadRight(ReceiptWidth));
            content.AppendLine(new string('-', ReceiptWidth));

            // Items header
            content.AppendLine(FormatItemLine("Qty", "Description", "Amount"));
            content.AppendLine(new string('-', ReceiptWidth));
            content.AppendLine();

            // Invoice items
            foreach (var item in invoice.Items)
            {
                foreach (var itemInfo in item.itemInfos)
                {
                    string quantityColumn = itemInfo.IsFirstItem ? item.Qty.ToString() : "";
                    string displayNameColumn = itemInfo.Description.Length > DescWidth
                        ? itemInfo.Description.Substring(0, DescWidth - 3) + "..."
                        : itemInfo.Description;
                    string amountColumn = decimal.TryParse(itemInfo.Amount, out decimal amount)
                        ? amount.ToString("C", PesoCulture)
                        : itemInfo.Amount;

                    content.AppendLine(FormatItemLine(quantityColumn, displayNameColumn, amountColumn));
                }
                content.AppendLine();
            }
            content.AppendLine(new string('-', ReceiptWidth));

            // Totals
            content.AppendLine(CenterText($"{"Total:",-15}{FormatCurrency(invoice.TotalAmount),17}"));
            if (!string.IsNullOrEmpty(invoice.DiscountAmount))
            {
                content.AppendLine(CenterText($"{"Discount:",-15}{invoice.DiscountAmount,17}"));
            }
            content.AppendLine(CenterText($"{"Due:",-15}{FormatCurrency(invoice.DueAmount),17}"));

            if (invoice.OtherPayments != null && invoice.OtherPayments.Any())
            {
                foreach (var payment in invoice.OtherPayments)
                {
                    content.AppendLine(CenterText($"{payment.SaleTypeName + ":",-15}{FormatCurrency(payment.Amount),17}"));
                }
            }
            content.AppendLine(CenterText($"{"Cash:",-15}{FormatCurrency(invoice.CashTenderAmount),17}"));
            content.AppendLine(CenterText($"{"Total:",-15}{FormatCurrency(invoice.TotalTenderAmount),17}"));
            content.AppendLine(CenterText($"{"Change:",-15}{FormatCurrency(invoice.ChangeAmount),17}"));
            content.AppendLine();

            content.AppendLine(CenterText($"{"VAT Zero:",-15}{FormatCurrency(invoice.VatZero),17}"));
            content.AppendLine(CenterText($"{"VAT Exempt:",-15}{FormatCurrency(invoice.VatExemptSales),17}"));
            content.AppendLine(CenterText($"{"VAT Sales:",-15}{FormatCurrency(invoice.VatSales),17}"));
            content.AppendLine(CenterText($"{"VAT Amt:",-15}{FormatCurrency(invoice.VatAmount),17}"));
            content.AppendLine();

            if (invoice.ElligiblePeopleDiscounts == null || !invoice.ElligiblePeopleDiscounts.Any())
            {
                content.AppendLine(CenterText("Name:_________________"));
                content.AppendLine(CenterText("Address:______________"));
                content.AppendLine(CenterText("TIN: _________________"));
                content.AppendLine(CenterText("Signature: ___________"));
                content.AppendLine();
            }
            else
            {
                foreach (var pwdSc in invoice.ElligiblePeopleDiscounts)
                {
                    string nameText = $"Name: {pwdSc.ToUpper()}";
                    content.AppendLine(nameText);
                    content.AppendLine("Address: _____________");
                    content.AppendLine("TIN: ________________");
                    content.AppendLine("Signature: __________");
                    content.AppendLine();
                }
            }

            // Footer
            //content.AppendLine(CenterText("Sales Invoice"));
            //content.AppendLine(CenterText("Arsene Software"));
            //content.AppendLine(CenterText("Labangon, Cebu"));
            //content.AppendLine(CenterText($"TIN: {invoice.VatTinNumber}"));
            //content.AppendLine(CenterText($"Issued: {invoice.DateIssued:d}"));
            //content.AppendLine(CenterText($"Valid: {invoice.ValidUntil:d}"));
            //content.AppendLine();
            content.AppendLine(new string('=', ReceiptWidth));
            content.AppendLine(CenterText("Thank you!"));
            content.AppendLine(new string('=', ReceiptWidth));
            content.AppendLine(CenterText($"DUPLICATE COPY #{invoice.PrintCount}"));

            // Print to thermal printer
            //PrintToPrinter(content);

            File.WriteAllText(filePath, content.ToString());

            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }

        private static string FormatCurrency(string value)
        {
            if (decimal.TryParse(value, out decimal amount))
            {
                return amount.ToString("C", PesoCulture);
            }
            return value;
        }
    }
}
