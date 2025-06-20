﻿using EBISX_POS.API.Data;
using EBISX_POS.API.Models;
using EBISX_POS.API.Services.DTO.Auth;
using EBISX_POS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using EBISX_POS.API.Services.DTO.Menu;
using System.Net.Http.Json;
using System.Security.Cryptography;

namespace EBISX_POS.API.Services.Repositories
{
    public class AuthRepository(DataContext _dataContext) : IAuth
    {
        private static readonly HttpClient _httpClient = new()
        {
            BaseAddress = new Uri("https://ebisx.com/")
        };

        private static long GenerateSearchIdFromText(string text)
        {
            // Use SHA256 to generate a hash
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text));

            // Take first 8 bytes and convert to long
            // We use absolute value to ensure positive number
            var numericHash = Math.Abs(BitConverter.ToInt64(hashBytes, 0));

            // Ensure it's not too large for our database
            return numericHash % 1000000000000; // 12 digits max
        }

        public async Task<List<CashierDTO>> Cashiers()
        {
            var cashiers = await _dataContext.User
                .Where(u => u.UserRole == "Cashier" && u.IsActive)
                .Select(u => new CashierDTO
                {
                    Email = u.UserEmail,
                    Name = u.UserFName + " " + u.UserLName
                })
                .ToListAsync();

            return cashiers;
        }

        public async Task<(bool, string)> CashWithdrawDrawer(string cashierEmail, string managerEmail, decimal cash)
        {
            var timestamp = await _dataContext.Timestamp
                .Include(t => t.Cashier)
                .Include(t => t.ManagerLog)
                .FirstOrDefaultAsync(t => t.Cashier.UserEmail == cashierEmail && t.TsOut == null);

            var manager = await _dataContext.User
                .FirstOrDefaultAsync(m => m.UserEmail == managerEmail && m.UserRole != "Cashier" && m.IsActive);

            if (manager is null)
                return (false, "Invalid manager credentials.");

            if (timestamp?.CashInDrawerAmount is not { } available)
                return (false, "No active session or drawer amount not set.");

            var tsIn = timestamp.TsIn;

            // First get all valid orders for the cashier
            var orders = await _dataContext.Order
                .Include(o => o.Cashier)
                .ToListAsync();

            orders = orders
                .Where(o =>
                    o.Cashier.UserEmail == cashierEmail &&
                    !o.IsCancelled &&
                    !o.IsPending &&
                    !o.IsReturned &&
                    o.CreatedAt >= tsIn &&
                    o.CashTendered != null &&
                    o.TotalAmount != 0)
                .ToList();

            // Then calculate the total in memory
            decimal totalCashInDrawer = orders.Sum(o =>
                o.CashTendered!.Value - o.ChangeAmount!.Value);

            if (cash > available + totalCashInDrawer)
                return (false, "Withdrawal exceeds drawer balance.");

            timestamp.ManagerLog.Add(new UserLog
            {
                Manager = manager,
                WithdrawAmount = cash,
                Timestamp = timestamp,
                Action = "Cash Withdrawal"
            });

            await _dataContext.SaveChangesAsync();
            return (true, "Cash withdrawal recorded.");
        }

        public async Task<bool> ChangeMode(string managerEmail)
        {
            var manager = await _dataContext.User
                .FirstOrDefaultAsync(u => u.UserEmail == managerEmail && u.UserRole != "Cashier" && u.IsActive);

            if (manager == null)
                return false;

            var posTerminalInfo = await _dataContext.PosTerminalInfo.FirstOrDefaultAsync();
            if (posTerminalInfo == null)
            {
                throw new InvalidOperationException("POS Terminal Info not found");
            }

            // Toggle the training mode
            posTerminalInfo.IsTrainMode = !posTerminalInfo.IsTrainMode;

            // Log the mode change
            _dataContext.UserLog.Add(new UserLog
            {
                Manager = manager,
                Action = $"Changed to {(posTerminalInfo.IsTrainMode ? "Training" : "Live")} Mode"
            });

            await _dataContext.SaveChangesAsync();
            return posTerminalInfo.IsTrainMode;
        }

        public async Task<(bool, string)> CheckData()
        {
            var hasUsers = await _dataContext.User
                .AnyAsync(a => a.IsActive);
            var hasMenu = await _dataContext.Menu
                .AnyAsync();

            if (hasUsers || hasMenu)
            {
                // Already has at least one user or one menu item → no need to seed
                return (false, "Data is already set up; skipping seed.");
            }

            // No users AND no menus → safe to seed
            return (true, "No existing data found; ready to seed initial data.");
        }

        public async Task<(bool, string, string)> HasPendingOrder()
        {
            // Check if there's a pending order with a related cashier.
            var pendingOrder = await _dataContext.Order
                .Include(o => o.Cashier)
                .Where(o => o.IsPending)
                .FirstOrDefaultAsync();

            // Check if there's an active timestamp (meaning the cashier hasn't timed out yet).
            var activeTimestamp = await _dataContext.Timestamp
                .Include(t => t.Cashier)
                .Where(t => t.TsOut == null)
                .FirstOrDefaultAsync();

            // If a pending order exists, use its cashier information.
            if (pendingOrder != null)
            {
                return (true,
                        pendingOrder.Cashier.UserEmail,
                        $"{pendingOrder.Cashier.UserFName} {pendingOrder.Cashier.UserLName}");
            }
            // If no pending order but the cashier is still clocked in, return its information.
            else if (activeTimestamp != null)
            {
                return (true,
                        activeTimestamp.Cashier.UserEmail,
                        $"{activeTimestamp.Cashier.UserFName} {activeTimestamp.Cashier.UserLName}");
            }

            // If neither condition is met, return false with empty values.
            return (false, "", "");
        }

        public async Task<bool> IsCashedDrawer(string cashierEmail)
        {

            var timestamp = await _dataContext.Timestamp
                .Include(t => t.Cashier)
                .Where(t => t.Cashier.UserEmail == cashierEmail && t.TsOut == null && t.CashInDrawerAmount != null && t.CashInDrawerAmount >= 1000)
                .FirstOrDefaultAsync();

            return timestamp != null;
        }

        public async Task<(bool, string)> IsManagerValid(string managerEmail)
        {
            var email = await _dataContext.User
                .Where(u => u.UserRole != "Cashier" && u.IsActive && u.UserEmail == managerEmail)
                .Select(u => u.UserEmail)
                .FirstOrDefaultAsync()
                ?? string.Empty;

            // returns (true, "manager@…") if found, or (false, "") if not
            return (!string.IsNullOrEmpty(email), email);
        }

        public async Task<bool> IsTrainMode()
        {
            var posTerminalInfo = await _dataContext.PosTerminalInfo.FirstOrDefaultAsync();
            if (posTerminalInfo == null)
            {
                throw new InvalidOperationException("POS Terminal Info not found");
            }
            return posTerminalInfo.IsTrainMode;
        }

        public async Task<(bool, string)> LoadData()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<MenuResponseDTO>("asspos/mobileloaditems.php?db_name=arseneso_barandog&usecenter=MAIN");

                if (response?.AllUsers == null || !response.AllUsers.Any())
                {
                    return (false, "No menu items found in the API response.");
                }

                // Get unique categories from the API data with case-insensitive deduplication
                var uniqueCategories = response.AllUsers
                    .Select(x => x.ItemGroup?.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .GroupBy(x => x.ToLower())
                    .Select(g => g.First()) // Take the first occurrence of each case-insensitive group
                    .ToList();

                // Add new categories to database with case-insensitive checking
                foreach (var categoryName in uniqueCategories)
                {
                    var normalizedCategoryName = categoryName.Trim();
                    
                    // Use client-side evaluation for case-insensitive comparison
                    var existingCategories = await _dataContext.Category.ToListAsync();
                    var exists = existingCategories.Any(c => c.CtgryName.ToUpper() == normalizedCategoryName.ToUpper());
                    
                    if (!exists)
                    {
                        var category = new Category { CtgryName = normalizedCategoryName.ToUpper() };
                        await _dataContext.Category.AddAsync(category);
                    }
                }
                await _dataContext.SaveChangesAsync();

                // Get all categories from database for reference
                var dbCategories = await _dataContext.Category.ToListAsync();

                // Process menu items
                foreach (var item in response.AllUsers)
                {
                    if (string.IsNullOrWhiteSpace(item.ItemGroup))
                    {
                        continue; // Skip items without category
                    }

                    // Find the category with case-insensitive comparison using client-side evaluation
                    var category = dbCategories.FirstOrDefault(c => 
                        c.CtgryName.ToLower() == item.ItemGroup.Trim().ToLower());
                    
                    if (category == null)
                    {
                        // This shouldn't happen, but if it does, create the category
                        category = new Category { CtgryName = item.ItemGroup.Trim() };
                        await _dataContext.Category.AddAsync(category);
                        await _dataContext.SaveChangesAsync();
                        dbCategories.Add(category);
                    }

                    // Check if menu item already exists
                    var existingMenu = await _dataContext.Menu
                        .FirstOrDefaultAsync(m => m.MenuName == item.ItemId);

                    // Generate SearchId from barcode
                    long searchId;
                    if (string.IsNullOrWhiteSpace(item.Barcode))
                    {
                        // If barcode is empty, use the item description
                        searchId = GenerateSearchIdFromText(item.Description);
                    }
                    else if (long.TryParse(item.Barcode, out var numericBarcode))
                    {
                        // If barcode is numeric, use it directly
                        searchId = numericBarcode;
                    }
                    else
                    {
                        // If barcode is text, generate a numeric hash
                        searchId = GenerateSearchIdFromText(item.Barcode);
                    }

                    if (existingMenu == null)
                    {
                        // Create new menu item
                        var menu = new Menu
                        {
                            MenuName = item.ItemId,
                            MenuCost = decimal.TryParse(item.Cost, out var cost) ? cost : 0,
                            MenuPrice = decimal.TryParse(item.Price, out var price) ? price : 0,
                            BaseUnit = item.BaseUnit,
                            MenuImagePath = item.ImageLocation,
                            MenuType = item.ItemType,
                            MenuIsAvailable = true,
                            IsVatExempt = item.VatType.Contains("EXEMPT", StringComparison.OrdinalIgnoreCase),
                            Category = category,
                            SearchId = searchId,
                            PrivateId = item.ItemId
                        };

                        await _dataContext.Menu.AddAsync(menu);
                    }
                    else
                    {
                        // Update existing menu item
                        existingMenu.MenuCost = decimal.TryParse(item.Cost, out var cost) ? cost : existingMenu.MenuCost;
                        existingMenu.MenuPrice = decimal.TryParse(item.Price, out var price) ? price : existingMenu.MenuPrice;
                        existingMenu.BaseUnit = item.BaseUnit;
                        existingMenu.MenuImagePath = item.ImageLocation;
                        existingMenu.MenuType = item.ItemType;
                        existingMenu.IsVatExempt = item.VatType.Contains("EXEMPT", StringComparison.OrdinalIgnoreCase);
                        existingMenu.Category = category;
                        existingMenu.SearchId = searchId;
                        existingMenu.PrivateId = item.ItemId;
                    }
                }

                await _dataContext.SaveChangesAsync();
                return (true, "Menu data loaded successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading menu data: {ex.Message}");
                return (false, $"Failed to load menu data: {ex.Message}");
            }
        }

        public async Task<(bool success, bool isManager, string email, string name)> LogIn(LogInDTO dto)
        {
            // Try to look up a manager if one was supplied
            User? manager = null;
            if (!string.IsNullOrWhiteSpace(dto.ManagerEmail))
            {
                manager = await _dataContext.User
                    .FirstOrDefaultAsync(u =>
                        u.UserEmail == dto.ManagerEmail &&
                        u.UserRole != "Cashier" &&
                        u.IsActive);
            }

            // Try to look up a cashier if one was supplied
            User? cashier = null;
            if (!string.IsNullOrWhiteSpace(dto.CashierEmail))
            {
                cashier = await _dataContext.User
                    .FirstOrDefaultAsync(u =>
                        u.UserEmail == dto.CashierEmail &&
                        u.UserRole == "Cashier" &&
                        u.IsActive);
            }
            var hasMenu = await _dataContext.Menu.AnyAsync(i => i.MenuIsAvailable);


            // If neither found, fail
            if (manager == null && cashier == null)
                return (false, false, "", "Invalid credentials. Please try again.");

            // --- Manager-only login (no cashierEmail supplied) ---
            if (cashier == null)
            {
                // Log the manager login
                _dataContext.UserLog.Add(new UserLog
                {
                    Manager = manager!,          // we know manager != null here
                    Action = "Manager Login",
                });

                await _dataContext.SaveChangesAsync();

                return (
                    success: true,
                    isManager: true,
                    email: manager!.UserEmail,
                    name: $"{manager!.UserFName} {manager!.UserLName}"
                );
            }

            // If manager was supplied but not found, return error
            if (!string.IsNullOrWhiteSpace(dto.ManagerEmail) && manager == null)
            {
                return (false, false, "", "Invalid manager credentials");
            }
            if (!hasMenu)
                return (false, false, "", "No menu is available. Cannot proceed with the transaction.");

            // --- Cashier login (may have manager approval) ---
            var timestamp = new Timestamp
            {
                TsIn = DateTimeOffset.Now,
                Cashier = cashier,    // non-null
                ManagerIn = manager,      // may be null if self-approved
                IsTrainMode = await IsTrainMode()
            };
            _dataContext.Timestamp.Add(timestamp);

            // Optionally log the manager's approval
            if (manager != null)
            {
                _dataContext.UserLog.Add(new UserLog
                {
                    Manager = manager,
                    Timestamp = timestamp,
                    Action = "Approved Cashier Login"
                });
            }


            await _dataContext.SaveChangesAsync();

            return (
                success: true,
                isManager: false,
                email: cashier.UserEmail,
                name: $"{cashier.UserFName} {cashier.UserLName}"
            );
        }


        public async Task<(bool, string)> LogOut(LogInDTO logOutDTO)
        {
            var manager = await _dataContext.User
                .Where(m => m.UserEmail == logOutDTO.ManagerEmail && m.UserRole != "Cashier" && m.IsActive)
                .FirstOrDefaultAsync();

            var cashier = await _dataContext.User
                .Where(m => m.UserEmail == logOutDTO.CashierEmail && m.UserRole == "Cashier" && m.IsActive)
                .FirstOrDefaultAsync();

            if (manager == null)
                return (false, "Invalid Credential!");
            if (cashier == null)
                return (false, "Invalid Credential of Cashier!");


            var pendingOrder = await _dataContext.Order
                .Where(o => o.IsPending && o.Cashier == cashier)
                .AnyAsync();

            if (pendingOrder)
                return (false, "Cashier has pending order!");

            var timestamp = await _dataContext.Timestamp
                .Where(t => t.Cashier == cashier && t.TsOut == null)
                .FirstOrDefaultAsync();

            if (timestamp == null)
                return (false, "Cashier is not clocked in!");

            timestamp.TsOut = DateTimeOffset.UtcNow; // Set the time-out to now
            timestamp.ManagerOut = manager; // Manager who authorized the logout

            await _dataContext.SaveChangesAsync();

            return (true, "Cashier Logged Out!");
        }

        public async Task<(bool, string)> SetCashInDrawer(string cashierEmail, decimal cash)
        {
            try
            {
                // First check if cashier exists
                var cashier = await _dataContext.User
                    .FirstOrDefaultAsync(u => u.UserEmail == cashierEmail && u.UserRole == "Cashier" && u.IsActive);

                if (cashier == null)
                    return (false, "Cashier not found or is not active!");

                // Get the active timestamp (where cashier is clocked in)
                var timestamp = await _dataContext.Timestamp
                    .Include(t => t.Cashier)
                    .Where(t => t.Cashier.UserEmail == cashierEmail && t.TsOut == null)
                    .FirstOrDefaultAsync();

                if (timestamp == null)
                    return (false, "No active session found for this cashier. Please clock in first.");

                // Validate cash amount
                if (cash < 0)
                    return (false, "Cash amount cannot be negative!");

                // Set the cash amount
                timestamp.CashInDrawerAmount = cash;
                await _dataContext.SaveChangesAsync();

                return (true, "Cash in drawer set successfully!");
            }
            catch (Exception ex)
            {
                // Log the exception
                Debug.WriteLine($"Error setting cash in drawer: {ex.Message}");
                return (false, "An error occurred while setting cash in drawer.");
            }
        }

        public async Task<(bool, string)> SetCashOutDrawer(string cashierEmail, decimal cash)
        {
            var timestamp = await _dataContext.Timestamp
                .Include(t => t.Cashier)
                .Where(t => t.Cashier.UserEmail == cashierEmail && t.TsOut == null)
                .FirstAsync();

            timestamp.CashOutDrawerAmount = cash;

            if (timestamp.CashInDrawerAmount == null)
                return (false, "Cash in drawer amount is null!");

            await _dataContext.SaveChangesAsync();

            return (true, "Cash out drawer set!");
        }
    }
}
