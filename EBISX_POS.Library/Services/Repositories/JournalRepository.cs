using EBISX_POS.API.Data;
using EBISX_POS.API.Models.Journal;
using EBISX_POS.API.Services.DTO.Journal;
using EBISX_POS.API.Services.Interfaces;
using EBISX_POS.Library.Services.DTO.Journal;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace EBISX_POS.API.Services.Repositories
{
    public class JournalRepository(JournalContext _journal, DataContext _dataContext) : IJournal
    {
        private static readonly HttpClient _httpClient = new()
        {
            BaseAddress = new Uri("https://ebisx.com/")
        };

        public async Task<List<AccountJournal>> AccountJournals()
        {
            return await _journal.AccountJournal.ToListAsync();
        }

        //public async Task<(bool isSuccess, string message)> AddItemsJournal(long orderId)
        //{

        //    var items = await _dataContext.Order
        //        .Include(o => o.Items)
        //        .Include(c => c.Coupon)
        //        .Where(o => o.Id == orderId)
        //        .SelectMany(o => o.Items)
        //        .Include(i => i.Menu)
        //        .Include(i => i.Drink)
        //        .Include(i => i.AddOn)
        //        .Include(i => i.Order)
        //        .Include(i => i.Meal)
        //        .ToListAsync();

        //    if (items.IsNullOrEmpty())
        //        return (false, "Order not found");

        //    var journals = new List<AccountJournal>();

        //    foreach (var item in items)
        //    {
        //        var journal = new AccountJournal
        //        {
        //            EntryNo = item.Order.Id,
        //            EntryLineNo = 3, // Adjust as 
        //            Status = item.IsVoid ? "Unposted" : "Posted",
        //            EntryName = item.EntryId,
        //            AccountName = item.Menu?.MenuName ?? item.Drink?.MenuName ?? item.AddOn?.MenuName ?? "Unknown",
        //            EntryDate = item.createdAt.DateTime,
        //            Description = item.Menu?.MenuName != null ? "Menu"
        //            : item.Drink?.MenuName != null ? "Drink"
        //            : "Add-On",
        //            QtyOut = item.ItemQTY,
        //            Price = Convert.ToDouble(item.ItemPrice),

        //            // Optionally, set other properties as needed.
        //        };
        //    }


        //    return (true, "Success");
        //}
        public async Task<(bool isSuccess, string message)> AddItemsJournal(long orderId)
        {
            try
            {

                var order = await _dataContext.Order
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Menu)
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Drink)
                    .Include(o => o.Items)
                        .ThenInclude(i => i.AddOn)
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Meal)
                    .Include(o => o.Coupon)
                    .Include(o => o.Cashier)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    return (false, "Order not found");
                }

                // Skip journal entries for training mode
                if (order.IsTrainMode)
                {
                    return (true, "Training mode order - no journal entries created");
                }

                if (order.Items == null || !order.Items.Any())
                {
                    return (false, "No items found in the order");
                }

                var journals = new List<AccountJournal>();

                foreach (var item in order.Items)
                {

                    var accountName = item.Menu?.MenuName
                                   ?? item.Drink?.MenuName
                                   ?? item.AddOn?.MenuName
                                   ?? item.Meal?.Menu?.MenuName // if Meal points to another Item
                                   ?? "Unknown";

                    var description = item.Menu != null ? "Menu"
                                     : item.Drink != null ? "Drink"
                                     : item.AddOn != null ? "Add-On"
                                     : "Unknown";

                    var unit = item.Menu?.BaseUnit
                            ?? item.Drink?.BaseUnit
                            ?? item.AddOn?.BaseUnit
                            ?? item.Meal?.Menu?.BaseUnit // only if Meal wraps a Menu
                            ?? "";

                    var journal = new AccountJournal
                    {
                        EntryNo = order.InvoiceNumber,
                        Reference = order.InvoiceNumber.ToString("D12") ?? "",
                        EntryLineNo = 3, // Adjust if needed
                        Status = item.IsVoid ? "Unposted" : order.IsReturned ? "Returned" : "Posted",
                        EntryName = "1",
                        AccountName = accountName,
                        EntryDate = item.createdAt.DateTime,
                        Description = item.Menu?.PrivateId ?? "",
                        QtyOut = item.ItemQTY,
                        Price = Convert.ToDouble(item.ItemPrice),
                        Cashier = order.Cashier?.UserEmail ?? "Unknown Cashier",
                        ItemID = item.Menu?.PrivateId ?? "",
                        QtyPerBaseUnit = 1,
                        Unit = unit,
                    };

                    journals.Add(journal);

                }

                if (!journals.Any())
                {
                    return (false, "No valid journal entries to add.");
                }

                _journal.AccountJournal.AddRange(journals);
                await _journal.SaveChangesAsync();

                return (true, "Journal entries successfully added.");
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred: {ex.Message}");
            }
        }

        public async Task<(bool isSuccess, string message)> AddPwdScAccountJournal(AddPwdScAccountJournalDTO journalDTO)
        {
            if (journalDTO is null)
            {
                return (false, "Input cannot be null.");
            }

            if (journalDTO.PwdScInfo == null || !journalDTO.PwdScInfo.Any())
            {
                return (false, "PwdScInfo list cannot be empty.");
            }

            try
            {
                // Get the order to check training mode and invoice number
                var order = await _dataContext.Order
                    .FirstOrDefaultAsync(o => o.Id == journalDTO.OrderId);

                if (order == null)
                {
                    return (false, "Order not found.");
                }

                // Skip journal entries for training mode
                if (order.IsTrainMode)
                {
                    return (true, "Training mode order - no journal entries created");
                }

                // Prepare a list of valid journal entries
                var journals = new List<AccountJournal>();

                foreach (var pwdOrSc in journalDTO.PwdScInfo)
                {
                    if (string.IsNullOrWhiteSpace(pwdOrSc.Name))
                    {
                        continue;
                    }

                    var journal = new AccountJournal
                    {
                        EntryNo = order.InvoiceNumber,
                        Reference = order.InvoiceNumber.ToString("D12") ?? "",
                        EntryLineNo = 5,
                        Status = journalDTO.Status ?? "Posted",
                        AccountName = pwdOrSc.Name,
                        EntryDate = journalDTO.EntryDate,
                        EntryName = "1",
                        Description = journalDTO.IsPWD ? "PWD" : "Senior",
                        Cashier = order.Cashier?.UserEmail ?? "Unknown Cashier"
                    };

                    journals.Add(journal);

                }

                if (!journals.Any())
                {
                    return (false, "No valid journal entries to add. Please check your input.");
                }

                await _journal.AccountJournal.AddRangeAsync(journals);
                await _journal.SaveChangesAsync();

                return (true, "Success");
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred: {ex.Message}");
            }
        }

        public async Task<(bool isSuccess, string message)> AddPwdScJournal(long orderId)
        {

            if (orderId <= 0)
            {
                return (false, "Invalid order ID.");
            }

            // 1) Load the order so we can read the PWD/SC fields
            var order = await _dataContext.Order
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return (false, "Order not found.");
            }

            // Skip journal entries for training mode
            if (order.IsTrainMode)
            {
                return (true, "Training mode order - no journal entries created");
            }

            // 2) Guard: need both names and OSCAs
            if (string.IsNullOrWhiteSpace(order.EligibleDiscNames) ||
                string.IsNullOrWhiteSpace(order.OSCAIdsNum))
            {
                return (false, "No PWD/SC information to journal.");
            }

            // 3) Split into lists
            var names = order.EligibleDiscNames
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.Trim())
                .ToList();

            var oscas = order.OSCAIdsNum
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(o => o.Trim())
                .ToList();

            // 4) Pair them up to the smaller count
            var count = Math.Min(names.Count, oscas.Count);
            if (count == 0)
            {
                return (false, "No valid PWD/SC pairs found.");
            }

            // 5) Build journal entries
            var journals = new List<AccountJournal>();
            int lineNo = 5;  // starting line number for PWD/SC entries

            for (int i = 0; i < count; i++)
            {
                var name = names[i];
                var osca = oscas[i];

                var journal = new AccountJournal
                {
                    EntryNo = order.InvoiceNumber,
                    Reference = order.InvoiceNumber.ToString("D12") ?? "",
                    EntryLineNo = lineNo++,
                    Status = order.IsCancelled ? "Unposted" : "Posted",
                    AccountName = name,
                    EntryName = "1",
                    EntryDate = order.CreatedAt.DateTime,
                    Cashier = order.Cashier?.UserEmail ?? "Unknown Cashier"
                };

                journals.Add(journal);

            }

            // 6) Persist
            try
            {
                _journal.AccountJournal.AddRange(journals);
                await _journal.SaveChangesAsync();

                return (true, $"{journals.Count} PWD/SC entries added.");
            }
            catch (Exception ex)
            {

                return (false, $"An error occurred: {ex.Message}");
            }
        }

        public async Task<(bool isSuccess, string message)> AddTendersJournal(long orderId)
        {

            if (orderId <= 0)
            {
                return (false, "Invalid order ID.");
            }

            // Load the order plus any AlternativePayments
            var order = await _dataContext.Order
                .Include(o => o.AlternativePayments)
                    .ThenInclude(t => t.SaleType)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return (false, "Order not found");
            }

            // Skip journal entries for training mode
            if (order.IsTrainMode)
            {
                return (true, "Training mode order - no journal entries created");
            }

            var journals = new List<AccountJournal>();

            // 1) Record the cash tendered on the order itself
            if (order.CashTendered > 0)
            {
                journals.Add(new AccountJournal
                {
                    EntryNo = order.InvoiceNumber,
                    Reference = order.InvoiceNumber.ToString("D12") ?? "",
                    EntryLineNo = 0,
                    Status = order.IsCancelled ? "Unposted" : order.IsReturned ? "Returned" : "Posted",
                    EntryName = "1",
                    AccountName = "Cash",
                    Description = "Cash Tendered",
                    Debit = order.IsReturned ? 0 : Convert.ToDouble(order.CashTendered),
                    Credit = order.IsReturned ? Convert.ToDouble(order.CashTendered) : 0,
                    EntryDate = order.CreatedAt.DateTime,
                    Cashier = order.Cashier?.UserEmail ?? "Unknown Cashier"
                });
            }

            // 2) Record any alternative payments (card, gift-card, etc.)
            if (order.AlternativePayments != null)
            {
                foreach (var tender in order.AlternativePayments)
                {
                    var journal = new AccountJournal
                    {
                        EntryNo = order.InvoiceNumber,
                        Reference = order.InvoiceNumber.ToString("D12") ?? "",
                        EntryLineNo = 0,
                        Status = order.IsCancelled ? "Unposted" : order.IsReturned ? "Returned" : "Posted",
                        EntryName = "1",
                        AccountName = tender.SaleType.Account,
                        Description = tender.SaleType.Type,
                        Debit = order.IsReturned ? 0 : Convert.ToDouble(tender.Amount),
                        Credit = order.IsReturned ? Convert.ToDouble(tender.Amount) : 0,
                        EntryDate = order.CreatedAt.DateTime
                    };

                    journals.Add(journal);
                }
            }

            if (!journals.Any())
            {
                return (false, "No payment entries found.");
            }

            try
            {
                _journal.AccountJournal.AddRange(journals);
                await _journal.SaveChangesAsync();

                return (true, $"{journals.Count} payment entries added.");
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred: {ex.Message}");
            }
        }

        public async Task<(bool isSuccess, string message)> AddTotalsJournal(long orderId)
        {

            if (orderId <= 0)
            {
                return (false, "Invalid order ID.");
            }

            // Load just the Order so we can grab TotalAmount, DiscountType, DiscountAmount
            var order = await _dataContext.Order
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return (false, "Order not found.");
            }

            // Skip journal entries for training mode
            if (order.IsTrainMode)
            {
                return (true, "Training mode order - no journal entries created");
            }

            var journals = new List<AccountJournal>();

            // 1) Discount line (EntryLineNo = 9)
            //if (order.DiscountAmount > 0)
            //{
            //    var discountAccount = !string.IsNullOrWhiteSpace(order.DiscountType)
            //        ? order.DiscountType
            //        : "Discount";

            //    journals.Add(new AccountJournal
            //    {
            //        EntryNo = order.InvoiceNumber,
            //        EntryLineNo = 10,
            //        EntryName = "1",
            //        Reference = order.InvoiceNumber.ToString("D12") ?? "",
            //        Status = order.IsCancelled ? "Unposted" : order.IsReturned ? "Returned" : "Posted",
            //        AccountName = discountAccount,
            //        Description = "Discount",
            //        Debit = order.IsReturned ? 0 : Convert.ToDouble(order.DiscountAmount),
            //        Credit = order.IsReturned ? Convert.ToDouble(order.DiscountAmount) : 0,
            //        EntryDate = order.CreatedAt.DateTime,
            //        Cashier = order.Cashier?.UserEmail ?? "Unknown Cashier"
            //    });
            //}

            // 2) Total line (EntryLineNo = 10)
            journals.Add(new AccountJournal
            {
                EntryNo = order.InvoiceNumber,
                Reference = order.InvoiceNumber.ToString("D12") ?? "",
                EntryLineNo = 10,
                EntryName = "1",
                Status = order.IsCancelled ? "Unposted" : order.IsReturned ? "Returned" : "Posted",
                AccountName = "Sales",
                Description = "Total Amount",
                Debit = order.IsReturned ? 0 : Convert.ToDouble(order.TotalAmount),
                Credit = order.IsReturned ? Convert.ToDouble(order.TotalAmount) : 0,
                EntryDate = order.CreatedAt.DateTime,
                Cashier = order.Cashier?.UserEmail ?? "Unknown Cashier",
                AccountBalance = (order.IsReturned ? 0 : Convert.ToDouble(order.TotalAmount)) - (order.IsReturned ? Convert.ToDouble(order.TotalAmount) : 0),
                SubTotal = Convert.ToDouble(order.DueAmount),
                TaxTotal = Convert.ToDouble(order.VatAmount),
                GrossTotal = Convert.ToDouble(order.TotalAmount),
                DiscAmt = Convert.ToDouble(order.DiscountAmount),
                
                 

            });

            //journals.Add(new AccountJournal
            //{
            //    EntryNo = order.InvoiceNumber,
            //    Reference = order.InvoiceNumber.ToString("D12") ?? "",
            //    EntryLineNo = 10,
            //    EntryName = "1",
            //    Status = order.IsCancelled ? "Unposted" : order.IsReturned ? "Returned" : "Posted",
            //    AccountName = "VAT",
            //    Description = "VAT Amount",
            //    Vatable = Convert.ToDouble(order.VatAmount),
            //    EntryDate = order.CreatedAt.DateTime,
            //    Cashier = order.Cashier?.UserEmail ?? "Unknown Cashier"
            //});

            //journals.Add(new AccountJournal
            //{
            //    EntryNo = order.InvoiceNumber,
            //    Reference = order.InvoiceNumber.ToString("D12") ?? "",
            //    EntryLineNo = 10,
            //    EntryName = "1",
            //    Status = order.IsCancelled ? "Unposted" : order.IsReturned ? "Returned" : "Posted",
            //    AccountName = "VAT Exempt",
            //    Description = "VAT Exempt Amount",
            //    Vatable = Convert.ToDouble(order.VatExempt),
            //    EntryDate = order.CreatedAt.DateTime,
            //    Cashier = order.Cashier?.UserEmail ?? "Unknown Cashier"
            //});

            //journals.Add(new AccountJournal
            //{
            //    EntryNo = order.InvoiceNumber,
            //    Reference = order.InvoiceNumber.ToString("D12") ?? "",
            //    EntryLineNo = 10,
            //    EntryName = "1",
            //    Status = order.IsCancelled ? "Unposted" : order.IsReturned ? "Returned" : "Posted",
            //    AccountName = "SubTotal",
            //    Description = "Order SubTotal",
            //    SubTotal = Convert.ToDouble(order.DueAmount),
            //    EntryDate = order.CreatedAt.DateTime,
            //    Cashier = order.Cashier?.UserEmail ?? "Unknown Cashier"
            //});

            if (!journals.Any())
            {
                return (false, "No totals to journal.");
            }

            try
            {
                _journal.AccountJournal.AddRange(journals);
                await _journal.SaveChangesAsync();

                return (true, $"{journals.Count} totals entries added.");
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred: {ex.Message}");
            }
        }

        public async Task<(bool isSuccess, string message)> PushAccountJournals(DateTime selectedDate, IProgress<(int current, int total, string status)>? progress = null)
        {
            try
            {
                // Check if data for this date has already been pushed
                var alreadyPushedQuery = _journal.AccountJournal.AsQueryable();
                var startOfDay = selectedDate.Date;
                var endOfDay = startOfDay.AddDays(1);
                alreadyPushedQuery = alreadyPushedQuery.Where(j => j.EntryDate >= startOfDay && j.EntryDate < endOfDay);
                alreadyPushedQuery = alreadyPushedQuery.Where(j => j.Cleared == "Y");

                //var alreadyPushedCount = await alreadyPushedQuery.CountAsync();
                //if (alreadyPushedCount > 0)
                //{
                //    return (false, $"Data for {selectedDate:yyyy-MM-dd} has already been pushed. You can only push data once per day.");
                //}

                // Get all posted journal entries that haven't been pushed yet (Cleared != "Y")
                var query = _journal.AccountJournal.AsQueryable();

                // Filter by selected date (now required)
                query = query.Where(j => j.EntryDate >= startOfDay && j.EntryDate < endOfDay);

                // Only get entries that haven't been pushed yet (Cleared != "Y")
                query = query.Where(j => j.Cleared != "Y");

                var journals = await query.ToListAsync();

                if (!journals.Any())
                {
                    return (true, $"No journal entries to push for {selectedDate:yyyy-MM-dd}. All entries may have already been pushed.");
                }

                var successCount = 0;
                var errorCount = 0;
                var errors = new List<string>();
                var totalCount = journals.Count;

                // Report initial progress
                progress?.Report((0, totalCount, $"Found {totalCount} entries to push for {selectedDate:yyyy-MM-dd}. Starting push process..."));

                for (int i = 0; i < journals.Count; i++)
                {
                    var journal = journals[i];

                    try
                    {
                        // Report progress before each request
                        progress?.Report((i, totalCount, $"Pushing journal {journal.UniqueId} ({i + 1}/{totalCount})..."));

                        // Map AccountJournal to PushAccountJournalDTO
                        var pushDto = new PushAccountJournalDTO
                        {
                            Entry_Type = "INVOICE",
                            Entry_No = journal.EntryNo?.ToString() ?? "",
                            Entry_Line_No = journal.EntryLineNo?.ToString() ?? "0",
                            Entry_Date = journal.EntryDate.ToString("yyyy-MM-dd"),
                            CostCenter = journal.CostCenter ?? "Store 1",
                            ItemId = journal.ItemID ?? "",
                            Unit = journal.Unit ?? "",
                            Qty = journal.QtyOut?.ToString() ?? "0",
                            Cost = journal.Cost?.ToString() ?? "0.00",
                            Price = journal.Price?.ToString() ?? "0.00",
                            TotalPrice = journal.TotalPrice?.ToString() ?? "0.00",
                            Debit = journal.Debit?.ToString() ?? "0.00",
                            Credit = journal.Credit?.ToString() ?? "0.00",
                            AccountBalance = journal.AccountBalance?.ToString() ?? "0.00",
                            Prev_Reading = journal.PrevReading.ToString(),
                            Curr_Reading = journal.CurrReading.ToString(),
                            Memo = journal.Memo ?? "",
                            AccountName = journal.AccountName ?? "",
                            Reference = journal.Reference ?? "",
                            Entry_Name = journal.EntryName ?? "",
                            Cashier = journal.Cashier ?? "",
                            Count_Type = "", // Default value
                            Deposited = "0", // Default value
                            Deposit_Date = "",
                            Deposit_Reference = "",
                            Deposit_By = "",
                            Deposit_Time = "00:00:00",
                            CustomerName = journal.NameDesc ?? "",
                            SubTotal = journal.SubTotal?.ToString() ?? "0.00",
                            TotalTax = journal.TaxTotal?.ToString() ?? "0.00",
                            GrossTotal = journal.GrossTotal?.ToString() ?? "0.00",
                            Discount_Type = journal.EntryName ?? "", //
                            Discount_Amount = journal.DiscAmt?.ToString() ?? "0.00",
                            NetPayable = (journal.GrossTotal - journal.DiscAmt)?.ToString() ?? "0.00",
                            Status = journal.Status,
                            User_Email = journal.Cashier ?? "",
                            QtyPerBaseUnit = journal.QtyPerBaseUnit?.ToString() ?? "1",
                            QtyBalanceInBaseUnit = journal.QtyBalanceInBaseUnit?.ToString() ?? "0"
                        };

                        // Build the query string
                        var queryParams = new List<string>
                        {
                            $"entry_type={Uri.EscapeDataString(pushDto.Entry_Type)}",
                            $"entry_no={Uri.EscapeDataString(pushDto.Entry_No)}",
                            $"entry_line_no={Uri.EscapeDataString(pushDto.Entry_Line_No)}",
                            $"entry_date={Uri.EscapeDataString(pushDto.Entry_Date)}",
                            $"costcenter={Uri.EscapeDataString(pushDto.CostCenter)}",
                            $"itemid={Uri.EscapeDataString(pushDto.ItemId)}",
                            $"unit={Uri.EscapeDataString(pushDto.Unit)}",
                            $"qty={Uri.EscapeDataString(pushDto.Qty)}",
                            $"cost={Uri.EscapeDataString(pushDto.Cost)}",
                            $"price={Uri.EscapeDataString(pushDto.Price)}",
                            $"totalprice={Uri.EscapeDataString(pushDto.TotalPrice)}",
                            $"debit={Uri.EscapeDataString(pushDto.Debit)}",
                            $"credit={Uri.EscapeDataString(pushDto.Credit)}",
                            $"accountbalance={Uri.EscapeDataString(pushDto.AccountBalance)}",
                            $"prev_reading={Uri.EscapeDataString(pushDto.Prev_Reading)}",
                            $"curr_reading={Uri.EscapeDataString(pushDto.Curr_Reading)}",
                            $"memo={Uri.EscapeDataString(pushDto.Memo)}",
                            $"accountname={Uri.EscapeDataString(pushDto.AccountName)}",
                            $"reference={Uri.EscapeDataString(pushDto.Reference)}",
                            $"entry_name={Uri.EscapeDataString(pushDto.Entry_Name)}",
                            $"cashier={Uri.EscapeDataString(pushDto.Cashier)}",
                            $"count_type={Uri.EscapeDataString(pushDto.Count_Type)}",
                            $"deposited={Uri.EscapeDataString(pushDto.Deposited)}",
                            $"deposit_date={Uri.EscapeDataString(pushDto.Deposit_Date)}",
                            $"deposit_reference={Uri.EscapeDataString(pushDto.Deposit_Reference)}",
                            $"deposit_by={Uri.EscapeDataString(pushDto.Deposit_By)}",
                            $"deposit_time={Uri.EscapeDataString(pushDto.Deposit_Time)}",
                            $"customername={Uri.EscapeDataString(pushDto.CustomerName)}",
                            $"subtotal={Uri.EscapeDataString(pushDto.SubTotal)}",
                            $"totaltax={Uri.EscapeDataString(pushDto.TotalTax)}",
                            $"grosstotal={Uri.EscapeDataString(pushDto.GrossTotal)}",
                            $"discount_type={Uri.EscapeDataString(pushDto.Discount_Type)}",
                            $"discount_amount={Uri.EscapeDataString(pushDto.Discount_Amount)}",
                            $"netpayable={Uri.EscapeDataString(pushDto.NetPayable)}",
                            $"status={Uri.EscapeDataString(pushDto.Status)}",
                            $"user_email={Uri.EscapeDataString(pushDto.User_Email)}",
                            $"qtyperbaseunit={Uri.EscapeDataString(pushDto.QtyPerBaseUnit)}",
                            $"qtybalanceinbaseunit={Uri.EscapeDataString(pushDto.QtyBalanceInBaseUnit)}"
                        };

                        var queryString = string.Join("&", queryParams);
                        var url = $"asspos/mobilepostransactions.php?{queryString}";

                        // Make the GET request with timeout
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 30 second timeout per request
                        var response = await _httpClient.GetAsync(url, cts.Token);

                        if (response.IsSuccessStatusCode)
                        {
                            successCount++;
                            // Mark as pushed by setting Cleared to "Y"
                            journal.Cleared = "Y";

                            // Report success progress
                            progress?.Report((i + 1, totalCount, $"Successfully pushed journal {journal.UniqueId}"));
                        }
                        else
                        {
                            errorCount++;
                            errors.Add($"Failed to push journal {journal.UniqueId}: {response.StatusCode} - {response.ReasonPhrase}");

                            // Report error progress
                            progress?.Report((i + 1, totalCount, $"Failed to push journal {journal.UniqueId}: {response.StatusCode}"));
                        }

                        // Wait 3 seconds between requests (except for the last one)
                        if (i < journals.Count - 1)
                        {
                            progress?.Report((i + 1, totalCount, "Waiting 3 seconds before next request..."));
                            await Task.Delay(3000); // 3 seconds
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        errorCount++;
                        errors.Add($"Timeout pushing journal {journal.UniqueId}: Request timed out after 30 seconds");
                        progress?.Report((i + 1, totalCount, $"Timeout pushing journal {journal.UniqueId}"));
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errors.Add($"Error pushing journal {journal.UniqueId}: {ex.Message}");
                        progress?.Report((i + 1, totalCount, $"Error pushing journal {journal.UniqueId}: {ex.Message}"));
                    }
                }

                // Save changes to mark successful pushes
                if (successCount > 0)
                {
                    await _journal.SaveChangesAsync();
                }

                // Create push data file after pushing is finished
                progress?.Report((totalCount, totalCount, "Creating data file..."));
                var dataFileResult = await CreatePushDataFile(selectedDate);
                var dataFileMessage = dataFileResult.isSuccess ? $" Data file created: {dataFileResult.message}" : $" Failed to create data file: {dataFileResult.message}";

                var message = $"Pushed {successCount} journal entries successfully for {selectedDate:yyyy-MM-dd}.{dataFileMessage}";
                if (errorCount > 0)
                {
                    message += $" Failed to push {errorCount} entries. Errors: {string.Join("; ", errors.Take(5))}";
                    if (errors.Count > 5)
                    {
                        message += $" and {errors.Count - 5} more...";
                    }
                }

                // Report final progress
                progress?.Report((totalCount, totalCount, $"Push completed for {selectedDate:yyyy-MM-dd}. {successCount} successful, {errorCount} failed."));

                return (successCount > 0, message);
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred while pushing journal entries for {selectedDate:yyyy-MM-dd}: {ex.Message}");
            }
        }

        public async Task<(bool isSuccess, string message)> TruncateOrders()
        {
            try
            {
                // 1) Fetch PosTerminalInfo
                var posInfo = await _dataContext.PosTerminalInfo.FirstOrDefaultAsync();
                if (posInfo == null)
                    return (false, "POS Terminal Info not found");

                var isTrain = posInfo.IsTrainMode;
                var resetId = isTrain ? posInfo.ResetCounterTrainNo : posInfo.ResetCounterNo;
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                // 2) Build backup paths & copy files
                var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                Directory.CreateDirectory(backupDir);

                var orderDbPath = _dataContext.Database.GetDbConnection().DataSource;
                var journalDbPath = _journal.Database.GetDbConnection().DataSource;

                var suffix = isTrain ? "_Train" : "";
                var orderBackupPath = Path.Combine(backupDir, $"Order_{resetId}_{timestamp}{suffix}.db");
                var journalBackupPath = Path.Combine(backupDir, $"Journal_{resetId}_{timestamp}{suffix}.db");

                if (File.Exists(orderDbPath))
                    File.Copy(orderDbPath, orderBackupPath, overwrite: true);
                if (File.Exists(journalDbPath))
                    File.Copy(journalDbPath, journalBackupPath, overwrite: true);

                // 3) Delete data from both databases in the correct order
                // First, handle the Journal database
                using (var journalTrans = await _journal.Database.BeginTransactionAsync())
                {
                    try
                    {
                        await _journal.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
                        await _journal.Database.ExecuteSqlRawAsync("DELETE FROM AccountJournal;");
                        await _journal.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence WHERE name = 'AccountJournal';");
                        await _journal.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");

                        await _journal.SaveChangesAsync();
                        await journalTrans.CommitAsync();

                    }
                    catch (Exception ex)
                    {
                        await journalTrans.RollbackAsync();
                        throw new Exception($"Failed to truncate Journal database: {ex.Message}", ex);
                    }
                }

                // Then, handle the Order database
                using (var orderTrans = await _dataContext.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Log counts before deletion
                        var beforeCounts = new
                        {
                            Items = await _dataContext.Item.CountAsync(),
                            UserLogs = await _dataContext.UserLog.CountAsync(),
                            AltPayments = await _dataContext.AlternativePayments.CountAsync(),
                            Timestamps = await _dataContext.Timestamp.CountAsync(),
                            Orders = await _dataContext.Order.CountAsync()
                        };

                        // Disable foreign keys and triggers
                        await _dataContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
                        await _dataContext.Database.ExecuteSqlRawAsync("PRAGMA triggers = OFF;");

                        // Delete in correct order based on dependencies
                        var deleteOrder = new[]
                        {
                            // First, delete all dependent records
                            "AlternativePayments",    // Depends on Order and SaleType
                            "Item",                   // Depends on Order, Menu, Drink, AddOn
                            "UserLog",               // Depends on Timestamp, User
                            "Timestamp",             // Depends on User
                            //"CouponPromo",           // Depends on Order
                            
                            //// Then, delete the main tables
                            //"Menu",                  // Depends on Category, AddOnType, DrinkType
                            //"SaleType",              // Independent table
                            //"DrinkType",             // Independent table
                            //"AddOnType",             // Independent table
                            //"Category",              // Independent table
                            
                            // Finally, delete the parent table
                            "Order"                // Parent table
                        };

                        foreach (var table in deleteOrder)
                        {
                            await _dataContext.Database.ExecuteSqlRawAsync($"DELETE FROM [{table}];");
                        }

                        // Reset all auto-increment counters
                        await _dataContext.Database.ExecuteSqlRawAsync(@"
                            DELETE FROM sqlite_sequence 
                            WHERE name IN (
                                'AlternativePayments', 'Item', 'UserLog', 'Timestamp', 
                                'CouponPromo', 'Menu', 'SaleType', 'DrinkType', 
                                'AddOnType', 'Category', 'Order'
                            );
                        ");

                        // Re-enable foreign keys and triggers
                        await _dataContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
                        await _dataContext.Database.ExecuteSqlRawAsync("PRAGMA triggers = ON;");

                        await _dataContext.SaveChangesAsync();
                        await orderTrans.CommitAsync();

                        // Log counts after deletion
                        var afterCounts = new
                        {
                            Items = await _dataContext.Item.CountAsync(),
                            UserLogs = await _dataContext.UserLog.CountAsync(),
                            AltPayments = await _dataContext.AlternativePayments.CountAsync(),
                            Timestamps = await _dataContext.Timestamp.CountAsync(),
                            Orders = await _dataContext.Order.CountAsync()
                        };
                    }
                    catch (Exception ex)
                    {
                        await orderTrans.RollbackAsync();
                        throw new Exception($"Failed to truncate Order database: {ex.Message}", ex);
                    }
                }

                // 4) Update reset counter
                if (isTrain)
                    posInfo.ResetCounterTrainNo++;
                else
                    posInfo.ResetCounterNo++;

                await _dataContext.SaveChangesAsync();

                return (true, "Successfully truncated and backed up all databases.");
            }
            catch (Exception ex)
            {
                return (false, $"Truncation failed: {ex.Message}");
            }
        }
        public async Task<(bool isSuccess, string message)> UnpostPwdScAccountJournal(long orderId, string oscaNum)
        {
            var pwdOrSc = await _journal.AccountJournal
                .FirstOrDefaultAsync(x => x.Reference == oscaNum && x.EntryNo == orderId);

            if (pwdOrSc == null)
                return (false, "Not Found Pwd/Sc");

            pwdOrSc.Status = "Unposted";
            await _journal.SaveChangesAsync();

            return (true, "Success");
        }

        public async Task<(bool isSuccess, string message)> CreatePushDataFile(DateTime? selectedDate = null)
        {
            try
            {
                // Get all posted journal entries that haven't been pushed yet (Cleared != "Y")
                var query = _journal.AccountJournal.AsQueryable();

                // Filter by selected date if provided
                if (selectedDate.HasValue)
                {
                    var startOfDay = selectedDate.Value.Date;
                    var endOfDay = startOfDay.AddDays(1);
                    query = query.Where(j => j.EntryDate >= startOfDay && j.EntryDate < endOfDay);
                }

                // Only get entries that haven't been pushed yet (Cleared != "Y")
                query = query.Where(j => j.Cleared != "Y");

                var journals = await query.ToListAsync();

                if (!journals.Any())
                {
                    var dateMsg1 = selectedDate.HasValue ? $" for {selectedDate.Value:yyyy-MM-dd}" : "";
                    return (false, $"No journal entries found{dateMsg1}.");
                }

                // Map to PushAccountJournalDTO list
                var pushList = journals.OrderBy(j => j.EntryNo).ThenBy(j => j.EntryLineNo).Select(journal => new PushAccountJournalDTO
                {
                    Entry_Type = journal.EntryType ?? "INVOICE",
                    Entry_No = journal.EntryNo?.ToString() ?? "",
                    Entry_Line_No = journal.EntryLineNo?.ToString() ?? "0",
                    Entry_Date = journal.EntryDate.ToString("yyyy-MM-dd"),
                    CostCenter = journal.CostCenter ?? "Store 1",
                    ItemId = journal.ItemID ?? "",
                    Unit = journal.Unit ?? "",
                    Qty = journal.QtyOut?.ToString() ?? "0",
                    Cost = journal.Cost?.ToString() ?? "0.00",
                    Price = journal.Price?.ToString() ?? "0.00",
                    TotalPrice = journal.TotalPrice?.ToString() ?? "0.00",
                    Debit = journal.Debit?.ToString() ?? "0.00",
                    Credit = journal.Credit?.ToString() ?? "0.00",
                    AccountBalance = journal.AccountBalance?.ToString() ?? "0.00",
                    Prev_Reading = journal.PrevReading.ToString(),
                    Curr_Reading = journal.CurrReading.ToString(),
                    Memo = journal.Memo ?? "",
                    AccountName = journal.AccountName ?? "",
                    Reference = journal.Reference ?? "",
                    Entry_Name = journal.EntryName ?? "",
                    Cashier = journal.Cashier ?? "",
                    Count_Type = "",
                    Deposited = "0",
                    Deposit_Date = "",
                    Deposit_Reference = "",
                    Deposit_By = "",
                    Deposit_Time = "00:00:00",
                    CustomerName = journal.NameDesc ?? "",
                    SubTotal = journal.SubTotal?.ToString() ?? "0.00",
                    TotalTax = journal.TaxTotal?.ToString() ?? "0.00",
                    GrossTotal = journal.TotalPrice?.ToString() ?? "0.00",
                    Discount_Type = journal.EntryName ?? "",
                    Discount_Amount = journal.DiscAmt?.ToString() ?? "0.00",
                    NetPayable = journal.TotalPrice?.ToString() ?? "0.00",
                    Status = journal.Status,
                    User_Email = journal.Cashier ?? "",
                    QtyPerBaseUnit = journal.QtyPerBaseUnit?.ToString() ?? "1",
                    QtyBalanceInBaseUnit = journal.QtyBalanceInBaseUnit?.ToString() ?? "0"
                }).ToList();

                // Create data directory if it doesn't exist
                var dataDirectory = Path.Combine("C:\\Data", "Journal");
                Directory.CreateDirectory(dataDirectory);

                // Create timestamped filename with date filter
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var dateSuffix = selectedDate.HasValue ? $"_{selectedDate.Value:yyyyMMdd}" : "";
                var dataFileName = $"PushData{dateSuffix}_{timestamp}.json";
                var dataFilePath = Path.Combine(dataDirectory, dataFileName);

                // Serialize to JSON with formatting
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var json = JsonSerializer.Serialize(pushList, options);
                await File.WriteAllTextAsync(dataFilePath, json);

                var dateMsg2 = selectedDate.HasValue ? $" for {selectedDate.Value:yyyy-MM-dd}" : "";
                return (true, $"Data file created successfully{dateMsg2}: {dataFileName}");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to create data file: {ex.Message}");
            }
        }
    }
}
