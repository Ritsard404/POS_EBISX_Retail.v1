using EBISX_POS.API.Data;
using EBISX_POS.API.Models;
using EBISX_POS.API.Models.Journal;
using EBISX_POS.API.Services.DTO.Journal;
using EBISX_POS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EBISX_POS.API.Services.Repositories
{
    public class JournalRepository(JournalContext _journal, DataContext _dataContext) : IJournal
    {
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
                    var accountName = item.Menu?.MenuName ?? item.Drink?.MenuName ?? item.AddOn?.MenuName ?? "Unknown";
                    var description = item.Menu != null ? "Menu"
                                     : item.Drink != null ? "Drink"
                                     : item.AddOn != null ? "Add-On"
                                     : "Unknown";

                    var journal = new AccountJournal
                    {
                        EntryNo = order.InvoiceNumber,
                        EntryLineNo = 3, // Adjust if needed
                        Status = item.IsVoid ? "Unposted" : order.IsReturned ? "Returned" : "Posted",
                        EntryName = item.EntryId ?? "",
                        AccountName = accountName,
                        EntryDate = item.createdAt.DateTime,
                        Description = description,
                        QtyOut = item.ItemQTY,
                        Price = Convert.ToDouble(item.ItemPrice)
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
                        EntryLineNo = 5,
                        Status = journalDTO.Status ?? "Posted",
                        AccountName = pwdOrSc.Name,
                        Reference = pwdOrSc.OscaNum,
                        EntryDate = journalDTO.EntryDate,
                        EntryName = journalDTO.IsPWD ? "PWD" : "Senior"
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
                    EntryLineNo = lineNo++,
                    Status = order.IsCancelled ? "Unposted" : "Posted",
                    AccountName = name,
                    Reference = osca,
                    EntryName = order.DiscountType ?? "",
                    EntryDate = order.CreatedAt.DateTime
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
                    EntryLineNo = 0,
                    Status = order.IsCancelled ? "Unposted" : order.IsReturned ? "Returned" : "Posted",
                    EntryName = "Cash Tendered",
                    AccountName = "Cash",
                    Description = "Cash Tendered",
                    Debit = order.IsReturned ? 0 : Convert.ToDouble(order.CashTendered),
                    Credit = order.IsReturned ? Convert.ToDouble(order.CashTendered) : 0,
                    EntryDate = order.CreatedAt.DateTime
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
                        EntryLineNo = 0,
                        Status = order.IsCancelled ? "Unposted" : order.IsReturned ? "Returned" : "Posted",
                        EntryName = tender.SaleType.Name,
                        AccountName = tender.SaleType.Account,
                        Description = tender.SaleType.Type,
                        Reference = tender.Reference,
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
            if (order.DiscountAmount > 0)
            {
                var discountAccount = !string.IsNullOrWhiteSpace(order.DiscountType)
                    ? order.DiscountType
                    : "Discount";

                journals.Add(new AccountJournal
                {
                    EntryNo = order.InvoiceNumber,
                    EntryLineNo = 10,
                    EntryName = "Discount Amount",
                    Status = order.IsCancelled ? "Unposted" : order.IsReturned ? "Returned" : "Posted",
                    AccountName = discountAccount,
                    Description = "Discount",
                    Debit = order.IsReturned ? 0 : Convert.ToDouble(order.DiscountAmount),
                    Credit = order.IsReturned ? Convert.ToDouble(order.DiscountAmount) : 0,
                    EntryDate = order.CreatedAt.DateTime
                });
            }

            // 2) Total line (EntryLineNo = 10)
            journals.Add(new AccountJournal
            {
                EntryNo = order.InvoiceNumber,
                EntryLineNo = 10,
                EntryName = "Total Amount",
                Status = order.IsCancelled ? "Unposted" : order.IsReturned ? "Returned" : "Posted",
                AccountName = "Sales",
                Description = "Order Total",
                Debit = order.IsReturned ? 0 : Convert.ToDouble(order.TotalAmount),
                Credit = order.IsReturned ? Convert.ToDouble(order.TotalAmount) : 0,
                EntryDate = order.CreatedAt.DateTime
            });

            journals.Add(new AccountJournal
            {
                EntryNo = order.InvoiceNumber,
                EntryLineNo = 10,
                EntryName = "VAT Amount",
                Status = order.IsCancelled ? "Unposted" : order.IsReturned ? "Returned" : "Posted",
                AccountName = "VAT",
                Description = "Order VAT",
                Vatable = Convert.ToDouble(order.VatAmount),
                EntryDate = order.CreatedAt.DateTime
            });

            journals.Add(new AccountJournal
            {
                EntryNo = order.InvoiceNumber,
                EntryLineNo = 10,
                EntryName = "VAT Exempt Amount",
                Status = order.IsCancelled ? "Unposted" : order.IsReturned ? "Returned" : "Posted",
                AccountName = "VAT Exempt",
                Description = "Order VAT Exempt",
                Vatable = Convert.ToDouble(order.VatExempt),
                EntryDate = order.CreatedAt.DateTime
            });

            journals.Add(new AccountJournal
            {
                EntryNo = order.InvoiceNumber,
                EntryLineNo = 10,
                EntryName = "Sub Total",
                Status = order.IsCancelled ? "Unposted" : order.IsReturned ? "Returned" : "Posted",
                AccountName = "SubTotal",
                Description = "Order SubTotal",
                SubTotal = Convert.ToDouble(order.DueAmount),
                EntryDate = order.CreatedAt.DateTime
            });

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
    }
}
