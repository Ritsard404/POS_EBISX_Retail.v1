﻿using EBISX_POS.API.Data;
using EBISX_POS.API.Models;
using EBISX_POS.API.Models.Utils;
using EBISX_POS.API.Services.DTO.Order;
using EBISX_POS.API.Services.DTO.Report;
using EBISX_POS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.IO;
using EBISX_POS.API.Services.PDF;

namespace EBISX_POS.API.Services.Repositories
{
    public class ReportRepository(DataContext _dataContext, IAuth _auth, AuditTrailPDFService _auditTrailPDFService, TransactionListPDFService _transactionListPDFService, SalesReportPDFService _salesReportPDF) : IReport
    {

        private async Task InitializePDFServices()
        {
            var posInfo = await _dataContext.PosTerminalInfo.FirstOrDefaultAsync();
            if (posInfo == null)
            {
                throw new InvalidOperationException("POS terminal information not configured");
            }

            // Update PDF services with business info
            _auditTrailPDFService.UpdateBusinessInfo(
                posInfo.RegisteredName ?? "N/A",
                posInfo.Address ?? "N/A",
                posInfo.VatTinNumber ?? "N/A",
                posInfo.MinNumber ?? "N/A",
                posInfo.PosSerialNumber ?? "N/A"
            );

            _transactionListPDFService.UpdateBusinessInfo(
                posInfo.RegisteredName ?? "N/A",
                posInfo.Address ?? "N/A",
                posInfo.VatTinNumber ?? "N/A"
            );

            _salesReportPDF.UpdateBusinessInfo(
                posInfo.RegisteredName ?? "N/A",
                posInfo.Address ?? "N/A",
                posInfo.VatTinNumber ?? "N/A"
            );
        }

        public async Task<(string CashInDrawer, string CurrentCashDrawer)> CashTrack(string cashierEmail)
        {
            // First get the timestamp
            var timestamp = await _dataContext.Timestamp
                .Include(t => t.Cashier)
                .Where(t => t.Cashier.UserEmail == cashierEmail && t.TsOut == null && t.CashInDrawerAmount != null && t.CashInDrawerAmount >= 1000)
                .FirstOrDefaultAsync();

            if (timestamp == null || timestamp.CashInDrawerAmount == null)
                return ("₱0.00", "₱0.00");

            var tsIn = timestamp.TsIn;

            // Fetch all orders with their cashier
            var orders = await _dataContext.Order
                .Include(o => o.Cashier)
                .ToListAsync();

            // Filter and calculate in memory
            decimal totalCashInDrawer = orders
                .Where(o =>
                    o.Cashier.UserEmail == cashierEmail &&
                    !o.IsCancelled &&
                    !o.IsPending &&
                    o.CreatedAt >= tsIn &&
                    o.CashTendered != null &&
                    o.TotalAmount != 0)
                .Sum(o => o.CashTendered ?? 0m - o.ChangeAmount ?? 0m - o.ReturnedAmount ?? 0m);

            // Get withdrawals
            var withdrawals = await _dataContext.UserLog
                .Where(u => u.Timestamp != null && u.Timestamp.Id == timestamp.Id && u.Action == "Cash Withdrawal")
                .ToListAsync();

            var totalWithdrawn = withdrawals.Sum(u => u.WithdrawAmount);

            var phCulture = new CultureInfo("en-PH");

            string cashInDrawerText = timestamp.CashInDrawerAmount.Value.ToString("C", phCulture);

            string currentCashDrawerText =
                (timestamp.CashInDrawerAmount.Value
                + totalCashInDrawer
                - totalWithdrawn
                ).ToString("C", phCulture);

            return (cashInDrawerText, currentCashDrawerText);
        }

        public async Task<List<GetInvoicesDTO>> GetInvoicesByDateRange(DateTime fromDate, DateTime toDate)
        {
            // normalize to midnight at the start of each day
            var start = fromDate.Date;
            var end = toDate.Date.AddDays(1);
            var isTrainMode = await _auth.IsTrainMode();

            // First fetch all orders with their related data
            var orders = await _dataContext.Order
                .Include(o => o.Cashier)
                .ToListAsync();

            var filteredOrders = orders
                .Where(o =>
                    o.CreatedAt >= start &&
                    o.CreatedAt < end &&
                    !o.IsPending &&
                    o.IsTrainMode == isTrainMode)
                .Select(s => new GetInvoicesDTO
                {
                    InvoiceNum = s.InvoiceNumber,
                    InvoiceNumString = s.InvoiceNumber.ToString("D12"),
                    Date = s.CreatedAt.ToString("MM/dd/yyyy"),
                    Time = s.CreatedAt.ToString("hh:mm tt"),
                    CashierName = s.Cashier.UserFName + " " + s.Cashier.UserLName,
                    CashierEmail = s.Cashier.UserEmail,
                    InvoiceStatus = s.IsCancelled ? "Cancelled" : s.IsReturned ? "Refund" : "Paid"
                })
                .OrderBy(i => i.InvoiceNum)
                .ToList();

            return filteredOrders;

        }


        public async Task<GetInvoiceDTO> GetInvoiceById(long invId)
        {
            var pesoCulture = new CultureInfo("en-PH");
            // 1) Load the order, its cashier, items and alternative payments
            var order = await _dataContext.Order
                .Include(o => o.Cashier)
                .Include(o => o.Items)
                .Include(o => o.AlternativePayments)
                    .ThenInclude(ap => ap.SaleType)
                .FirstOrDefaultAsync(o => o.InvoiceNumber == invId);

            if (order == null)
                return new GetInvoiceDTO();

            var orderItems = await GetOrderItems(order.Id);

            // 2) Load your POS terminal / business info (assumes a single row)
            var posInfo = await _dataContext.Set<PosTerminalInfo>()
                .FirstOrDefaultAsync();

            var discountValue = order.DiscountAmount ?? 0m;

            order.PrintCount += 1;

            await _dataContext.SaveChangesAsync();

            // 3) Map to your DTO
            var dto = new GetInvoiceDTO
            {
                // --- Business Details from POS info
                RegisteredName = posInfo?.RegisteredName ?? "",
                Address = posInfo?.Address ?? "",
                VatTinNumber = posInfo?.VatTinNumber ?? "",
                MinNumber = posInfo?.MinNumber ?? "",

                // --- Invoice Header
                InvoiceNum = order.InvoiceNumber.ToString("D12"),
                InvoiceDate = order.CreatedAt
                                          .ToString("MM/dd/yyyy HH:mm:ss"),
                OrderType = order.OrderType,
                CashierName = $"{order.Cashier.UserFName} {order.Cashier.UserLName}",

                // --- Line Items

                Items = orderItems
                .Select(group => new ItemDTO
                {
                    // take the quantity of the first (parent) sub‐order
                    Qty = group.TotalQuantity,

                    // map every sub‐order into your ItemInfoDTO
                    itemInfos = group.SubOrders?
                        .Select((s, index) => new ItemInfoDTO
                        {
                            IsFirstItem = index == 0,
                            Description = s.DisplayName,
                            Amount = s.ItemPriceString
                        })
                        .ToList()
                        // ensure non‐null list
                        ?? new List<ItemInfoDTO>()
                })
                .ToList(),



                // --- Totals
                TotalAmount = (order.TotalAmount).ToString("C", pesoCulture),
                DiscountAmount = discountValue != 0m
                      ? "-" + discountValue.ToString("C", pesoCulture)
                      : discountValue.ToString("C", pesoCulture),
                DueAmount = (order.DueAmount ?? 0m).ToString("C", pesoCulture),
                CashTenderAmount = (order.CashTendered ?? 0m).ToString("C", pesoCulture),
                TotalTenderAmount = (order.TotalTendered ?? 0m).ToString("C", pesoCulture),
                ChangeAmount = (order.ChangeAmount ?? 0m).ToString("C", pesoCulture),

                // VAT breakdown
                VatExemptSales = (order.VatExempt ?? 0m).ToString("C", pesoCulture),
                VatSales = (order.VatSales ?? 0m).ToString("C", pesoCulture),
                VatAmount = (order.VatAmount ?? 0m).ToString("C", pesoCulture),
                VatZero = (order.VatZero ?? 0m).ToString("C", pesoCulture),

                // Other tenders (e.g. gift cert, card, etc.)
                OtherPayments = order.AlternativePayments
                    .Select(ap => new OtherPaymentDTO
                    {
                        SaleTypeName = ap.SaleType.Name,
                        Amount = ap.Amount.ToString("C2")
                    })
                    .ToList(),

                // PWD/Senior/etc.
                ElligiblePeopleDiscounts = order.EligibleDiscNames?
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList()
                ?? new List<string>(),

                // --- POS Details
                PosSerialNumber = posInfo?.PosSerialNumber ?? "",
                DateIssued = posInfo?.DateIssued.ToString("MM/dd/yyyy") ?? "",
                ValidUntil = posInfo?.ValidUntil.ToString("MM/dd/yyyy") ?? "",
                PrintCount = order.PrintCount.ToString(),
            };

            return dto;
        }

        private async Task<List<GetCurrentOrderItemsDTO>> GetOrderItems(long orderId)
        {
            var items = await _dataContext.Order
                .Include(o => o.Items)
                .Include(c => c.Coupon)
                .Where(o => o.Id == orderId)
                .SelectMany(o => o.Items)
                .Where(i => !i.IsVoid)
                .Include(i => i.Menu)
                .Include(i => i.Drink)
                .Include(i => i.AddOn)
                .Include(i => i.Order)
                .Include(i => i.Meal)
                .ToListAsync();

            // Group items by EntryId.
            // For items with no EntryId (child meals), use the parent's EntryId from the Meal property.
            var groupedItems = items
                .GroupBy(i => i.EntryId ?? i.Meal?.EntryId)
                .OrderBy(g => g.Min(i => i.createdAt))
                .Select(g =>
                {
                    // Compute the promo discount amount from the parent order.
                    var promoDiscount = g.Select(i => (i.Order?.DiscountType == DiscountTypeEnum.Promo.ToString()
                                                        ? i.Order?.DiscountAmount ?? 0m
                                                        : 0m))
                                         .FirstOrDefault();
                    // Check for other discount types.
                    var otherDiscount = g.Any(i => i.IsPwdDiscounted || i.IsSeniorDiscounted);

                    // Set HasDiscount to true if there's any other discount or promo discount value is greater than zero.
                    var hasDiscount = otherDiscount || (promoDiscount > 0m);

                    // Build the DTO from the group
                    var dto = new GetCurrentOrderItemsDTO
                    {
                        // Use the group's key or 0 if still null.
                        EntryId = g.Key ?? "",
                        HasDiscount = hasDiscount,
                        PromoDiscountAmount = promoDiscount,
                        IsPwdDiscounted = g.Any(i => i.IsPwdDiscounted),
                        IsSeniorDiscounted = g.Any(i => i.IsSeniorDiscounted),
                        // Order each group so that the parent (Meal == null) comes first.
                        SubOrders = g.OrderBy(i => i.Meal == null ? 0 : 1)
                                     .Select(i => new CurrentOrderItemsSubOrder
                                     {
                                         MenuId = i.Menu?.Id,
                                         DrinkId = i.Drink?.Id,
                                         AddOnId = i.AddOn?.Id,
                                         // Fallback: use Menu name first, then Drink, then AddOn.
                                         Name = i.Menu?.MenuName ?? i.Drink?.MenuName ?? i.AddOn?.MenuName ?? "Unknown",
                                         Size = i.Menu?.Size ?? i.Drink?.Size ?? i.AddOn?.Size,
                                         ItemPrice = i.ItemPrice ?? 0m,
                                         Quantity = i.ItemQTY ?? 1,
                                         IsFirstItem = i.Meal == null
                                     })
                                     .ToList()
                    };

                    // If discount applies, add an extra suborder for discount details.
                    if (dto.HasDiscount && dto.PromoDiscountAmount <= 0)
                    {
                        // Calculate discount based on the current total of suborders.
                        // (Be aware that if you add the discount as a suborder, it might affect TotalPrice.)
                        var discountAmount = dto.SubOrders.Sum(s => s.ItemSubTotal) >= 1250
                        ? 250
                        : dto.SubOrders.Sum(s => s.ItemSubTotal) * 0.20m;

                        // Use the first item in the group to determine discount type.
                        var discountName = g.Any(i => i.IsPwdDiscounted) ? "PWD" : "Senior";

                        dto.SubOrders.Add(new CurrentOrderItemsSubOrder
                        {
                            Name = discountName,          // This can be adjusted to show a more descriptive name.
                            ItemPrice = discountAmount, // The discount amount.
                            Quantity = 1,
                            // You can set Size to null or leave it empty.
                            IsFirstItem = false         // Typically discount line is not the first item.
                        });
                    }

                    return dto;
                })
                .ToList();

            var ordersWithCoupons = await _dataContext.Order
                .Include(o => o.Coupon)
                .ThenInclude(c => c.CouponMenus)
                .Where(o => o.Id == orderId)
                .ToListAsync();

            var couponItems = ordersWithCoupons
                .SelectMany(o => o.Coupon)
                .Where(c => c != null)
                .DistinctBy(c => c.CouponCode) // using DistinctBy from System.Linq if available
                .Select(c => new GetCurrentOrderItemsDTO
                {
                    EntryId = $"Coupon-{c.CouponCode}",
                    HasDiscount = false,
                    PromoDiscountAmount = 0m,
                    IsPwdDiscounted = false,
                    IsSeniorDiscounted = false,
                    CouponCode = c.CouponCode,
                    SubOrders = new List<CurrentOrderItemsSubOrder>
                    {
                        new CurrentOrderItemsSubOrder
                        {
                            Name = $"Coupon: {c.CouponCode}",
                            ItemPrice = c.PromoAmount ?? 0m,
                            Quantity = c.CouponItemQuantity ?? 0,
                            IsFirstItem = true
                        }
                    }
                    .Concat(
                        c.CouponMenus?.Where(m => m.MenuIsAvailable)
                        .Select(m => new CurrentOrderItemsSubOrder
                        {
                            MenuId = m.Id,
                            Name = m.MenuName,
                            Size = m.Size,
                            ItemPrice = 0m,
                            Quantity = 1,
                            IsFirstItem = false
                        }) ?? Enumerable.Empty<CurrentOrderItemsSubOrder>()
                    ).ToList()
                })
                .ToList();



            // ✅ Merge regular orders with coupon orders.
            groupedItems.AddRange(couponItems);

            return groupedItems;
        }

        public async Task<XInvoiceReportDTO> XInvoiceReport()
        {
            var pesoCulture = new CultureInfo("en-PH");
            var defaultDecimal = 0m;
            var defaultDate = DateTime.UtcNow;

            bool isTrainMode = await _dataContext.PosTerminalInfo
                .Select(o => o.IsTrainMode)
                .FirstOrDefaultAsync();

            // Safely fetch data with null checks
            var orders = await _dataContext.Order
                .Include(o => o.Cashier)
                .Include(o => o.Items)
                .Include(o => o.AlternativePayments)
                    .ThenInclude(ap => ap.SaleType)
                .Where(o => !o.IsRead && o.IsTrainMode == isTrainMode)
                .ToListAsync() ?? new List<Order>();

            var posInfo = await _dataContext.PosTerminalInfo.FirstOrDefaultAsync();
            if (posInfo == null)
            {
                throw new InvalidOperationException("POS terminal information not configured");
            }

            var ts = await _dataContext.Timestamp
                .Include(t => t.Cashier)
                .Include(t => t.ManagerLog)
                .OrderBy(t => t.Id)
                .LastOrDefaultAsync(o => o.IsTrainMode == isTrainMode);

            // Handle empty orders scenario
            var firstOrder = orders.FirstOrDefault();
            var lastOrder = orders.LastOrDefault();
            var orderCount = orders.Count().ToString();

            // Calculate financials with null protection
            decimal openingFundDec = ts?.CashInDrawerAmount ?? defaultDecimal;

            // Move withdrawal calculation to memory
            var withdrawals = await _dataContext.UserLog
                .Where(mw => mw.Timestamp == ts)
                .ToListAsync();

            decimal withdrawnAmount = withdrawals.Sum(mw => mw.WithdrawAmount);

            // Calculate void and refund amounts in memory
            decimal voidDec = orders.Where(o => o.IsCancelled)
                                  .Sum(o => o?.TotalAmount ?? defaultDecimal);
            string voidCount = orders.Count(o => o.IsCancelled).ToString();

            decimal refundDec = orders.Where(o => o.IsReturned)
                                    .Sum(o => o?.ReturnedAmount ?? defaultDecimal);
            string refundCount = orders.Count(o => o.IsReturned).ToString();

            // Calculate valid orders total in memory - Now considering ReturnedAmount
            decimal validOrdersTotal = orders.Where(o => !o.IsCancelled)
                                           .Sum(o => (o?.CashTendered ?? defaultDecimal) -
                                                   (o?.ChangeAmount ?? defaultDecimal) -
                                                   (o?.ReturnedAmount ?? defaultDecimal));

            decimal actualCash = openingFundDec + validOrdersTotal;
            decimal expectedCash = (ts?.CashOutDrawerAmount ?? defaultDecimal) + withdrawnAmount;
            decimal shortOverDec = expectedCash - actualCash;

            // Safe payment processing - Adjusted for ReturnedAmount
            var payments = new Payments
            {
                Cash = orders.Sum(o => (o?.CashTendered ?? defaultDecimal) -
                                      (o?.ChangeAmount ?? defaultDecimal) -
                                      (o?.ReturnedAmount ?? defaultDecimal)),
                OtherPayments = orders
                    .SelectMany(o => o.AlternativePayments ?? new List<AlternativePayments>())
                    .GroupBy(ap => ap.SaleType?.Name ?? "Unknown")
                    .Select(g => new PaymentDetail
                    {
                        Name = g.Key + $" ({g.Count()}) :",
                        Amount = g.Sum(x => x.Amount * (1 - ((x.Order?.ReturnedAmount ?? defaultDecimal) /
                            (x.Order?.TotalAmount > 0 ? x.Order.TotalAmount : 1m)))),
                    }).ToList()
            };

            var summary = new TransactionSummary
            {
                CashInDrawer = (ts?.CashOutDrawerAmount ?? defaultDecimal).ToString("C", pesoCulture),
                OtherPayments = payments.OtherPayments
            };

            // Build DTO with safe values
            var dto = new XInvoiceReportDTO
            {
                BusinessName = posInfo.RegisteredName ?? "N/A",
                OperatorName = posInfo.OperatedBy ?? "N/A",
                AddressLine = posInfo.Address ?? "N/A",
                VatRegTin = posInfo.VatTinNumber ?? "N/A",
                Min = posInfo.MinNumber ?? "N/A",
                SerialNumber = posInfo.PosSerialNumber ?? "N/A",

                ReportDate = DateTime.Now.ToString("MMMM dd, yyyy", pesoCulture),
                ReportTime = DateTime.Now.ToString("hh:mm tt", pesoCulture),
                StartDateTime = firstOrder?.CreatedAt.LocalDateTime.ToString("MM/dd/yy hh:mm tt", pesoCulture)
                              ?? defaultDate.ToString("MM/dd/yy hh:mm tt", pesoCulture),
                EndDateTime = lastOrder?.CreatedAt.LocalDateTime.ToString("MM/dd/yy hh:mm tt", pesoCulture)
                             ?? defaultDate.ToString("MM/dd/yy hh:mm tt", pesoCulture),

                Cashier = ts?.Cashier != null
                        ? $"{ts.Cashier.UserFName} {ts.Cashier.UserLName}"
                        : "N/A",
                BeginningOrNumber = firstOrder?.InvoiceNumber.ToString("D12") ?? "N/A",
                EndingOrNumber = lastOrder?.InvoiceNumber.ToString("D12") ?? "N/A",
                TransactCount = orderCount,

                OpeningFund = openingFundDec.ToString("C", pesoCulture),
                VoidAmount = voidDec.ToString("C", pesoCulture),
                VoidCount = voidCount,
                Refund = refundDec.ToString("C", pesoCulture),
                RefundCount = refundCount,
                Withdrawal = withdrawnAmount.ToString("C", pesoCulture),

                Payments = payments,
                TransactionSummary = summary,
                ShortOver = shortOverDec.ToString("C", pesoCulture)
            };

            // Mark orders as read if any exist
            if (orders.Any())
            {
                foreach (var order in orders)
                {
                    order.IsRead = true;
                }
                await _dataContext.SaveChangesAsync();
            }

            return dto;
        }

        public async Task<ZInvoiceReportDTO> ZInvoiceReport()
        {
            var pesoCulture = new CultureInfo("en-PH");
            var defaultDecimal = 0m;
            var today = DateTime.Today;
            var defaultDate = today;

            bool isTrainMode = await _dataContext.PosTerminalInfo
                .Select(o => o.IsTrainMode)
                .FirstOrDefaultAsync();

            var orders = await _dataContext.Order
                .Where(o => o.IsTrainMode == isTrainMode)
                .Include(o => o.Items)
                .Include(o => o.AlternativePayments)
                    .ThenInclude(ap => ap.SaleType)
                .ToListAsync() ?? new List<Order>();

            // Initialize empty collections to prevent null references
            var allTimestamps = await _dataContext.Timestamp
                .Where(t => t.IsTrainMode == isTrainMode)
                .Include(t => t.Cashier)
                .Include(t => t.ManagerLog)
                .ToListAsync() ?? new List<Timestamp>();

            var posInfo = await _dataContext.PosTerminalInfo.FirstOrDefaultAsync() ?? new PosTerminalInfo
            {
                RegisteredName = "N/A",
                OperatedBy = "N/A",
                Address = "N/A",
                VatTinNumber = "N/A",
                MinNumber = "N/A",
                PosSerialNumber = "N/A",
                AccreditationNumber = "N/A",
                DateIssued = DateTime.Now,
                PtuNumber = "N/A",
                ValidUntil = DateTime.Now
            };

            // Handle empty scenario for dates
            var startDate = orders.Any() ? orders.Min(t => t.CreatedAt.LocalDateTime) : DateTime.Now;
            var endDate = orders.Any() ? orders.Max(t => t.CreatedAt.LocalDateTime) : DateTime.Now;

            // Withdrawal Amount
            var withdrawnAmount = allTimestamps
                .Where(t => t.TsOut.HasValue
                    && t.TsOut.Value.Date == today)
                .SelectMany(t => t.ManagerLog)
                .Where(mw => mw?.Action == "Cash Withdrawal")
                .Sum(mw => mw?.WithdrawAmount ?? defaultDecimal);

            var allRegularOrders = orders.Where(o => !o.IsCancelled).ToList();
            var allVoidOrders = orders.Where(o => o.IsCancelled).ToList();
            var allReturnOrders = orders.Where(o => o.IsReturned).ToList();

            // BREAKDOWN OF SALES
            var regularOrders = allRegularOrders.Where(o => o.CreatedAt.Date == today).ToList();
            var voidOrders = allVoidOrders.Where(o => o.CreatedAt.Date == today).ToList();
            var returnOrders = allReturnOrders.Where(o => o.CreatedAt.Date == today).ToList();

            // Accumulated Sales - Now considering ReturnedAmount
            decimal salesForTheDay = regularOrders
                .Where(c => c.CreatedAt.Date == today)
                .Sum(o => o.TotalAmount - (o.DiscountAmount ?? defaultDecimal) - (o.ReturnedAmount ?? defaultDecimal));

            decimal previousAccumulatedSales = allRegularOrders
                .Where(c => c.CreatedAt.Date < today)
                .Sum(o => o.TotalAmount - (o.DiscountAmount ?? defaultDecimal) - (o.ReturnedAmount ?? defaultDecimal));

            decimal presentAccumulatedSales = previousAccumulatedSales + salesForTheDay;

            // Financial calculations with default values and ReturnedAmount
            decimal grossSales = regularOrders.Sum(o => o?.TotalAmount ?? defaultDecimal);
            decimal totalVoid = voidOrders.Sum(o => o?.TotalAmount ?? defaultDecimal);
            decimal totalReturns = returnOrders.Sum(o => o?.ReturnedAmount ?? defaultDecimal);
            decimal totalDiscounts = regularOrders.Sum(o => o?.DiscountAmount ?? defaultDecimal);
            decimal cashSales = regularOrders.Sum(o =>
                (o?.CashTendered ?? defaultDecimal) -
                (o?.ChangeAmount ?? defaultDecimal) -
                (o?.ReturnedAmount ?? defaultDecimal));

            decimal netAmount = grossSales - totalReturns - totalVoid - totalDiscounts;

            // VAT calculations with defaults - Adjusted for ReturnedAmount
            decimal vatableSales = regularOrders.Sum(v =>
                (v?.VatSales ?? defaultDecimal) *
                (1 - ((v?.ReturnedAmount ?? defaultDecimal) / (v?.TotalAmount ?? 1m))));

            decimal vatAmount = regularOrders.Sum(o =>
                (o?.VatAmount ?? defaultDecimal) *
                (1 - ((o?.ReturnedAmount ?? defaultDecimal) / (o?.TotalAmount ?? 1m))));

            decimal vatExempt = regularOrders.Sum(o =>
                (o?.VatExempt ?? defaultDecimal) *
                (1 - ((o?.ReturnedAmount ?? defaultDecimal) / (o?.TotalAmount ?? 1m))));

            decimal zeroRated = 0m;

            // Cash in Drawer
            decimal cashInDrawer = allTimestamps
                .Where(t => t.TsOut.HasValue
                    && t.TsOut.Value.Date == today)
                .Sum(s => s.CashOutDrawerAmount) ?? defaultDecimal;

            // Opening Fund
            decimal openingFund = allTimestamps
                .Where(t => t.TsIn.Value.Date == today)
                .Sum(s => s.CashInDrawerAmount) ?? defaultDecimal;

            decimal actualCash = openingFund + cashSales;
            decimal expectedCash = cashInDrawer + withdrawnAmount;
            decimal shortOver = expectedCash - actualCash;

            var knownDiscountTypes = Enum.GetNames(typeof(DiscountTypeEnum)).ToList();

            // Discount calculations adjusted for ReturnedAmount
            decimal seniorDiscount = regularOrders
                .Where(s => s.DiscountType == DiscountTypeEnum.Senior.ToString() || s.DiscountType == "s-" + DiscountTypeEnum.Senior.ToString())
                .Sum(s => (s.DiscountAmount ?? 0m) *
                    (1 - ((s.ReturnedAmount ?? 0m) / (s.TotalAmount > 0 ? s.TotalAmount : 1m))));

            string seniorCount = regularOrders
                .Where(s => s.DiscountType == DiscountTypeEnum.Senior.ToString() || s.DiscountType == "s-" + DiscountTypeEnum.Senior.ToString())
                .Count()
                .ToString();

            decimal pwdDiscount = regularOrders
                .Where(s => s.DiscountType == DiscountTypeEnum.Pwd.ToString() || s.DiscountType == "s-" + DiscountTypeEnum.Pwd.ToString())
                .Sum(s => (s.DiscountAmount ?? 0m) *
                    (1 - ((s.ReturnedAmount ?? 0m) / (s.TotalAmount > 0 ? s.TotalAmount : 1m))));

            string pwdCount = regularOrders
                .Where(s => s.DiscountType == DiscountTypeEnum.Pwd.ToString() || s.DiscountType == "s-" + DiscountTypeEnum.Pwd.ToString())
                .Count()
                .ToString();

            decimal otherDiscount = regularOrders
                .Where(s => s.DiscountType != null && !knownDiscountTypes.Contains(s.DiscountType) &&
                    s.DiscountType != "s-" + DiscountTypeEnum.Pwd.ToString() &&
                    s.DiscountType != "s-" + DiscountTypeEnum.Senior.ToString())
                .Sum(s => (s.DiscountAmount ?? 0m) *
                    (1 - ((s.ReturnedAmount ?? 0m) / (s.TotalAmount > 0 ? s.TotalAmount : 1m))));

            string otherCount = regularOrders
                .Where(s => s.DiscountType != null && !knownDiscountTypes.Contains(s.DiscountType) &&
                    s.DiscountType != "s-" + DiscountTypeEnum.Pwd.ToString() &&
                    s.DiscountType != "s-" + DiscountTypeEnum.Senior.ToString())
                .Count()
                .ToString();

            // Safe payment processing - Adjusted for ReturnedAmount
            var payments = new Payments
            {
                Cash = cashSales,
                OtherPayments = orders
                .SelectMany(o => o.AlternativePayments != null && o.CreatedAt.Date == today ? o.AlternativePayments : new List<AlternativePayments>())
                .GroupBy(ap => ap.SaleType?.Name ?? "Unknown")
                .Select(g => new PaymentDetail
                {
                    Name = g.Key + $" ({g.Count()}):",
                    Amount = g.Sum(x => x.Amount * (1 - ((x.Order?.ReturnedAmount ?? defaultDecimal) / (x.Order?.TotalAmount ?? 1m)))),
                }).ToList()
            };

            // Build DTO with zero defaults
            var dto = new ZInvoiceReportDTO
            {
                BusinessName = posInfo.RegisteredName ?? "N/A",
                OperatorName = posInfo.OperatedBy ?? "N/A",
                AddressLine = posInfo.Address ?? "N/A",
                VatRegTin = posInfo.VatTinNumber ?? "N/A",
                Min = posInfo.MinNumber ?? "N/A",
                SerialNumber = posInfo.PosSerialNumber ?? "N/A",

                ReportDate = DateTime.Now.ToString("MMMM dd, yyyy", pesoCulture),
                ReportTime = DateTime.Now.ToString("hh:mm tt", pesoCulture),
                StartDateTime = startDate.ToString("MM/dd/yy hh:mm tt", pesoCulture),
                EndDateTime = endDate.ToString("MM/dd/yy hh:mm tt", pesoCulture),

                // Order numbers
                BeginningSI = GetOrderNumber(orders.Min(o => o?.InvoiceNumber)),
                EndingSI = GetOrderNumber(orders.Max(o => o?.InvoiceNumber)),
                BeginningVoid = GetOrderNumber(allVoidOrders.Min(o => o?.InvoiceNumber)),
                EndingVoid = GetOrderNumber(allVoidOrders.Max(o => o?.InvoiceNumber)),
                BeginningReturn = GetOrderNumber(allReturnOrders.Min(o => o?.InvoiceNumber)),
                EndingReturn = GetOrderNumber(allReturnOrders.Max(o => o?.InvoiceNumber)),
                TransactCount = orders.Count().ToString(),

                // Always zero when empty
                ResetCounter = isTrainMode ? posInfo.ResetCounterTrainNo.ToString() : posInfo.ResetCounterNo.ToString(),
                ZCounter = isTrainMode ? posInfo.ZCounterTrainNo.ToString() : posInfo.ZCounterNo.ToString(),

                // Financial summaries - Now using ReturnedAmount
                PresentAccumulatedSales = presentAccumulatedSales.ToString("C", pesoCulture),
                PreviousAccumulatedSales = previousAccumulatedSales.ToString("C", pesoCulture),
                SalesForTheDay = salesForTheDay.ToString("C", pesoCulture),

                SalesBreakdown = new SalesBreakdown
                {
                    VatableSales = vatableSales.ToString("C", pesoCulture),
                    VatAmount = vatAmount.ToString("C", pesoCulture),
                    VatExemptSales = vatExempt.ToString("C", pesoCulture),
                    ZeroRatedSales = zeroRated.ToString("C", pesoCulture),
                    GrossAmount = grossSales.ToString("C", pesoCulture),
                    LessDiscount = totalDiscounts.ToString("C", pesoCulture),
                    LessReturn = totalReturns.ToString("C", pesoCulture),
                    LessVoid = totalVoid.ToString("C", pesoCulture),
                    LessVatAdjustment = defaultDecimal.ToString("C", pesoCulture),
                    NetAmount = netAmount.ToString("C", pesoCulture)
                },

                TransactionSummary = new TransactionSummary
                {
                    CashInDrawer = cashInDrawer.ToString("C", pesoCulture),
                    OtherPayments = payments.OtherPayments
                },

                DiscountSummary = new DiscountSummary
                {
                    SeniorCitizen = seniorDiscount.ToString("C", pesoCulture),
                    SeniorCitizenCount = seniorCount,
                    PWD = pwdDiscount.ToString("C", pesoCulture),
                    PWDCount = pwdCount,
                    Other = otherDiscount.ToString("C", pesoCulture),
                    OtherCount = otherCount
                },

                SalesAdjustment = new SalesAdjustment
                {
                    Return = totalReturns.ToString("C", pesoCulture),
                    ReturnCount = returnOrders.Count().ToString(),
                    Void = totalVoid.ToString("C", pesoCulture),
                    VoidCount = voidOrders.Count().ToString(),
                },

                VatAdjustment = new VatAdjustment
                {
                    SCTrans = defaultDecimal.ToString("C", pesoCulture),
                    PWDTrans = defaultDecimal.ToString("C", pesoCulture),
                    RegDiscTrans = defaultDecimal.ToString("C", pesoCulture),
                    ZeroRatedTrans = defaultDecimal.ToString("C", pesoCulture),
                    VatOnReturn = defaultDecimal.ToString("C", pesoCulture),
                    OtherAdjustments = defaultDecimal.ToString("C", pesoCulture)
                },

                OpeningFund = openingFund.ToString("C", pesoCulture),
                Withdrawal = withdrawnAmount.ToString("C", pesoCulture),
                PaymentsReceived = (cashSales + payments.OtherPayments.Sum(s => s.Amount)).ToString("C", pesoCulture),
                ShortOver = shortOver.ToString("C", pesoCulture)
            };

            if (isTrainMode)
            {
                posInfo.ZCounterTrainNo += 1;
            }
            else
            {
                posInfo.ZCounterNo += 1;
            }
            await _dataContext.SaveChangesAsync();

            return dto;
        }

        private string GetOrderNumber(long? orderId)
        {
            return orderId.HasValue ? orderId.Value.ToString("D12") : 0.ToString("D12");
        }

        public async Task<List<UserActionLogDTO>> UserActionLog(bool isManagerLog, DateTime fromDate, DateTime toDate)
        {
            var logs = new List<UserActionLogDTO>();
            var start = fromDate.Date;
            var end = toDate.Date.AddDays(1);

            // Common query parts
            var userLogsQuery = _dataContext.UserLog
                .Include(m => m.Cashier)
                .Include(m => m.Manager)
                .Where(c =>
                    ((isManagerLog && c.Manager != null) || (!isManagerLog && c.Cashier != null)) &&
                    c.CreatedAt >= start &&
                    c.CreatedAt < end);

            var userLogs = await userLogsQuery
                .Select(m => new UserActionLogDTO
                {
                    Name = m.Manager.UserFName + " " + m.Manager.UserLName,
                    CashierName = m.Cashier.UserFName + " " + m.Cashier.UserLName,
                    Action = m.Action,
                    ManagerEmail = m.Manager.UserEmail,
                    CashierEmail = m.Cashier.UserEmail,
                    Amount = m.WithdrawAmount > 0
                        ? string.Format(CultureInfo.InvariantCulture, "₱{0:N2}", m.WithdrawAmount)
                        : null,
                    ActionDate = m.CreatedAt.ToLocalTime().ToString("MM/dd/yyyy hh:mm tt"),
                    SortActionDate = m.CreatedAt.ToLocalTime()
                })
                .ToListAsync();

            logs.AddRange(userLogs);

            // Process timestamps - Modified query for SQLite compatibility
            var timestamps = await _dataContext.Timestamp
                .AsNoTracking()
                .Include(t => t.Cashier)
                .Include(t => t.ManagerIn)
                .Include(t => t.ManagerOut)
                .ToListAsync();

            // Filter timestamps in memory after fetching
            var filteredTimestamps = timestamps.Where(t =>
                (t.TsIn.HasValue && t.TsIn.Value.DateTime.Date >= start && t.TsIn.Value.DateTime.Date < end) ||
                (t.TsOut.HasValue && t.TsOut.Value.DateTime.Date >= start && t.TsOut.Value.DateTime.Date < end))
                .ToList();

            ProcessTimestamps(filteredTimestamps, logs);

            return logs.OrderBy(l => l.SortActionDate).ToList();
        }
        private void ProcessTimestamps(List<Timestamp> timestamps, List<UserActionLogDTO> logs)
        {
            foreach (var t in timestamps)
            {
                var cashierName = t.Cashier != null
                    ? $"{t.Cashier.UserFName} {t.Cashier.UserLName}"
                    : "—";
                var cashierEmail = t.Cashier?.UserEmail ?? "—";

                // Login (TsIn)
                if (t.TsIn.HasValue)
                {
                    var tsIn = t.TsIn.Value;
                    if (t.CashInDrawerAmount.HasValue)
                    {
                        AddTimestampLog(logs, t.ManagerIn, tsIn,
                            "Set Cash in Drawer", t.CashInDrawerAmount, cashierName, cashierEmail);
                    }
                    else
                    {
                        AddTimestampLog(logs, t.ManagerIn, tsIn,
                            "Log In", null, cashierName, cashierEmail);
                    }
                }

                // Logout and/or Cash Out (TsOut)
                if (t.TsOut.HasValue)
                {
                    var tsOut = t.TsOut.Value;
                    var mgr = t.ManagerOut ?? t.ManagerIn;

                    if (t.CashOutDrawerAmount.HasValue)
                    {
                        AddTimestampLog(logs, mgr, tsOut,
                            "Set Cash out Drawer", t.CashOutDrawerAmount, cashierName, cashierEmail);
                    }
                    else
                    {
                        AddTimestampLog(logs, mgr, tsOut,
                            "Log Out", null, cashierName, cashierEmail);
                    }
                }
            }
        }


        public async Task<(List<AuditTrailDTO> Data, string FilePath)> GetAuditTrail(DateTime fromDate, DateTime toDate, string folderPath)
        {
            try
            {
                await InitializePDFServices();
                // Get the audit trail data
                var auditTrail = await GetAuditTrailData(fromDate, toDate);

                // Generate PDF
                var pdfBytes = _auditTrailPDFService.GenerateAuditTrailPDF(auditTrail, fromDate, toDate);

                // BASE name (no suffix):
                var baseName = $"AuditTrail_{fromDate:yyyyMMdd}_to_{toDate:yyyyMMdd}";

                // Append a timestamp so it's always unique
                var uniqueSuffix = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                var fileName = $"{baseName}_{uniqueSuffix}.pdf";

                // Ensure directory exists
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);
                // Save PDF file
                await File.WriteAllBytesAsync(filePath, pdfBytes);

                return (auditTrail, filePath);
            }
            catch (Exception ex)
            {
                // Log the error appropriately
                throw new Exception($"Error generating audit trail report: {ex.Message}", ex);
            }
        }

        public async Task<(List<TransactionListDTO> Data, TotalTransactionListDTO Totals, string FilePath)> GetTransactList(DateTime fromDate, DateTime toDate, string folderPath)
        {
            try
            {
                await InitializePDFServices();
                // Get the transaction list data
                var (transactions, totals) = await GetTransactListData(fromDate, toDate);

                // Generate PDF
                var pdfBytes = _transactionListPDFService.GenerateTransactionListPDF(transactions, fromDate, toDate);


                // BASE name (no suffix):
                var baseName = $"TranxList_{fromDate:yyyyMMdd}_to_{toDate:yyyyMMdd}";

                // Append a timestamp so it's always unique
                var uniqueSuffix = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                var fileName = $"{baseName}_{uniqueSuffix}.pdf";

                // Ensure directory exists
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);
                // Save PDF file
                await File.WriteAllBytesAsync(filePath, pdfBytes);

                // Save PDF file
                await File.WriteAllBytesAsync(filePath, pdfBytes);

                return (transactions, totals, filePath);
            }
            catch (Exception ex)
            {
                // Log the error appropriately
                throw new Exception($"Error generating transaction list report: {ex.Message}", ex);
            }
        }

        // Private helper methods to separate data retrieval from PDF generation
        private async Task<List<AuditTrailDTO>> GetAuditTrailData(DateTime fromDate, DateTime toDate)
        {
            var auditTrail = new List<AuditTrailDTO>();
            var startDate = fromDate.Date;
            var endDate = toDate.Date.AddDays(1);
            var phCulture = new CultureInfo("en-PH");
            const string DATE_FORMAT = "MM/dd/yyyy";
            const string TIME_FORMAT = "hh:mm tt";
            const string CURRENCY_FORMAT = "₱{0:N2}";

            // Get user logs
            var userLogs = await _dataContext.UserLog
                .Include(m => m.Cashier)
                .Include(m => m.Manager)
                .Where(c => c.CreatedAt >= startDate && c.CreatedAt < endDate)
                .Select(m => new AuditTrailDTO
                {
                    Date = m.CreatedAt.ToLocalTime().ToString(DATE_FORMAT, phCulture),
                    Time = m.CreatedAt.ToLocalTime().ToString(TIME_FORMAT, phCulture),
                    UserName = m.Manager != null
                        ? $"{m.Manager.UserFName} {m.Manager.UserLName}"
                        : $"{m.Cashier.UserFName} {m.Cashier.UserLName}",
                    Action = m.Action,
                    Amount = m.WithdrawAmount > 0
                        ? string.Format(CURRENCY_FORMAT, m.WithdrawAmount)
                        : null,
                    SortDateTime = m.CreatedAt.ToLocalTime()
                })
                .ToListAsync();

            auditTrail.AddRange(userLogs);

            // Get timestamps
            var timestamps = await _dataContext.Timestamp
                .Include(t => t.Cashier)
                .Include(t => t.ManagerIn)
                .Include(t => t.ManagerOut)
                .ToListAsync();

            // Filter timestamps in memory
            var filteredTimestamps = timestamps.Where(t =>
                (t.TsIn.HasValue && t.TsIn.Value.DateTime.Date >= startDate && t.TsIn.Value.DateTime.Date < endDate) ||
                (t.TsOut.HasValue && t.TsOut.Value.DateTime.Date >= startDate && t.TsOut.Value.DateTime.Date < endDate))
                .ToList();

            foreach (var t in filteredTimestamps)
            {
                // Process TsIn events
                if (t.TsIn.HasValue)
                {
                    var tsInDateTime = t.TsIn.Value;
                    var action = t.CashInDrawerAmount.HasValue ? "Set Cash in Drawer" : "Log In";
                    var amount = t.CashInDrawerAmount.HasValue
                        ? string.Format(CURRENCY_FORMAT, t.CashInDrawerAmount.Value)
                        : null;

                    auditTrail.Add(new AuditTrailDTO
                    {
                        Date = tsInDateTime.ToLocalTime().ToString(DATE_FORMAT, phCulture),
                        Time = tsInDateTime.ToLocalTime().ToString(TIME_FORMAT, phCulture),
                        UserName = t.ManagerIn != null
                            ? $"{t.ManagerIn.UserFName} {t.ManagerIn.UserLName}"
                            : "Super Admin",
                        Action = action,
                        Amount = amount,
                        SortDateTime = tsInDateTime.LocalDateTime
                    });
                }

                // Process TsOut events
                if (t.TsOut.HasValue)
                {
                    var tsOutDateTime = t.TsOut.Value;
                    var manager = t.ManagerOut ?? t.ManagerIn;
                    var action = t.CashOutDrawerAmount.HasValue ? "Set Cash out Drawer" : "Log Out";
                    var amount = t.CashOutDrawerAmount.HasValue
                        ? string.Format(CURRENCY_FORMAT, t.CashOutDrawerAmount.Value)
                        : null;

                    auditTrail.Add(new AuditTrailDTO
                    {
                        Date = tsOutDateTime.ToLocalTime().ToString(DATE_FORMAT, phCulture),
                        Time = tsOutDateTime.ToLocalTime().ToString(TIME_FORMAT, phCulture),
                        UserName = manager != null
                            ? $"{manager.UserFName} {manager.UserLName}"
                            : "Super Admin",
                        Action = action,
                        Amount = amount,
                        SortDateTime = tsOutDateTime.LocalDateTime
                    });
                }
            }

            return auditTrail.OrderBy(a => a.SortDateTime).ToList();
        }

        private async Task<(List<TransactionListDTO>, TotalTransactionListDTO)> GetTransactListData(DateTime fromDate, DateTime toDate)
        {
            // Set start date to beginning of day and end date to end of day
            var startDate = fromDate.Date;
            var endDate = toDate.Date.AddDays(1).AddTicks(-1);

            var isTrainMode = await _auth.IsTrainMode();

            // Get all orders with necessary includes
            var orders = _dataContext.Order
                .Include(o => o.Items)
                .Include(o => o.Cashier)
                .Where(o => o.IsTrainMode == isTrainMode)
                .AsEnumerable()
                .Where(o => o.CreatedAt.DateTime >= startDate && o.CreatedAt.DateTime <= endDate)
                .OrderBy(o => o.InvoiceNumber)
                .ToList();

            var transactionList = new List<TransactionListDTO>();
            var totalTransactionList = new TotalTransactionListDTO();

            foreach (var order in orders)
            {
                // Calculate base amounts and round to 2 decimal places
                var subTotal = Math.Round(order.TotalAmount, 2);
                var amountDue = Math.Round(order.DueAmount ?? 0m, 2);
                var grossSales = Math.Round(subTotal, 2);
                var returns = Math.Round(order.ReturnedAmount ?? 0m, 2);
                var lessDiscount = Math.Round(order.DiscountAmount ?? 0m, 2);
                var netOfSales = Math.Round(subTotal - lessDiscount - returns, 2);

                // Calculate VAT amounts proportionally based on refunded amount
                var refundRatio = order.TotalAmount > 0 ? (order.ReturnedAmount ?? 0m) / order.TotalAmount : 0m;
                var vatable = Math.Round((order.VatSales ?? 0m) * (1 - refundRatio), 2);
                var zeroRated = Math.Round(order.VatZero ?? 0m, 2);
                var exempt = Math.Round((order.VatExempt ?? 0m) * (1 - refundRatio), 2);
                var vat = Math.Round((order.VatAmount ?? 0m) * (1 - refundRatio), 2);

                var discType = !string.IsNullOrWhiteSpace(order.DiscountType)
                    ? (order.DiscountType.StartsWith("s-") ? order.DiscountType.Substring(2) : order.DiscountType)
                    : "";

                // Create initial transaction entry
                var baseTransaction = new TransactionListDTO
                {
                    Date = order.CreatedAt.ToString("MM/dd/yyyy"),
                    InvoiceNum = order.InvoiceNumber.ToString("D12"),
                    Src = "",
                    DiscType = discType,
                    Percent = order.DiscountPercent?.ToString() ?? "",
                    SubTotal = subTotal,
                    AmountDue = amountDue,
                    GrossSales = grossSales,
                    Returns = 0m,
                    NetOfReturns = Math.Round(grossSales - returns, 2),
                    LessDiscount = lessDiscount,
                    NetOfSales = netOfSales,
                    Vatable = vatable,
                    ZeroRated = zeroRated,
                    Exempt = exempt,
                    Vat = vat
                };

                // Add the initial transaction
                transactionList.Add(baseTransaction);

                // Update totals for base transaction
                UpdateTotals(totalTransactionList, grossSales, returns, lessDiscount, netOfSales, vatable, exempt, vat);

                // If order was cancelled, add a cancellation entry
                if (order.IsCancelled && order.StatusChangeDate.HasValue)
                {
                    var cancelledTransaction = new TransactionListDTO
                    {
                        Date = order.StatusChangeDate.Value.ToString("MM/dd/yyyy"),
                        InvoiceNum = $"{order.InvoiceNumber:D12}",
                        Src = "VOIDED",
                        DiscType = discType,
                        Percent = order.DiscountPercent?.ToString() ?? "",
                        SubTotal = Math.Round(-subTotal, 2),
                        AmountDue = Math.Round(-amountDue, 2),
                        GrossSales = Math.Round(-grossSales, 2),
                        Returns = 0m,
                        NetOfReturns = Math.Round(-grossSales, 2),
                        LessDiscount = Math.Round(-lessDiscount, 2),
                        NetOfSales = Math.Round(-netOfSales, 2),
                        Vatable = Math.Round(-vatable, 2),
                        ZeroRated = 0m,
                        Exempt = Math.Round(-exempt, 2),
                        Vat = Math.Round(-vat, 2)
                    };
                    transactionList.Add(cancelledTransaction);

                    // Update totals for cancellation
                    UpdateTotals(totalTransactionList, -grossSales, 0m, -lessDiscount, -netOfSales, -vatable, -exempt, -vat);
                }

                // If order has refunds, add a return entry
                if (order.IsReturned && order.StatusChangeDate.HasValue && returns > 0)
                {
                    var returnedTransaction = new TransactionListDTO
                    {
                        Date = order.StatusChangeDate.Value.ToString("MM/dd/yyyy"),
                        InvoiceNum = $"{order.InvoiceNumber:D12}",
                        Src = "REFUNDED",
                        DiscType = discType,
                        Percent = order.DiscountPercent?.ToString() ?? "",
                        SubTotal = Math.Round(-returns, 2),
                        AmountDue = 0m,
                        GrossSales = 0m,
                        Returns = returns,
                        NetOfReturns = 0m,
                        LessDiscount = 0m, // No discount on refunds
                        NetOfSales = -returns,
                        Vatable = Math.Round(-vatable * (returns / grossSales), 2),
                        ZeroRated = 0m,
                        Exempt = Math.Round(-exempt * (returns / grossSales), 2),
                        Vat = Math.Round(-vat * (returns / grossSales), 2)
                    };
                    transactionList.Add(returnedTransaction);

                    // Update totals for return
                    UpdateTotals(totalTransactionList, -returns, returns, 0m, -returns,
                        -vatable * (returns / grossSales),
                        -exempt * (returns / grossSales),
                        -vat * (returns / grossSales));
                }
            }

            return (transactionList.OrderBy(t => t.InvoiceNum).ToList(), totalTransactionList);
        }

        private void UpdateTotals(TotalTransactionListDTO totals, decimal grossSales, decimal returns,
            decimal lessDiscount, decimal netOfSales, decimal vatable, decimal exempt, decimal vat)
        {
            totals.TotalGrossSales = Math.Round(totals.TotalGrossSales + grossSales, 2);
            totals.TotalReturns = Math.Round(totals.TotalReturns + returns, 2);
            totals.TotalNetOfReturns = Math.Round(totals.TotalNetOfReturns + (grossSales - returns), 2);
            totals.TotalLessDiscount = Math.Round(totals.TotalLessDiscount + lessDiscount, 2);
            totals.TotalNetOfSales = Math.Round(totals.TotalNetOfSales + netOfSales, 2);
            totals.TotalVatable = Math.Round(totals.TotalVatable + vatable, 2);
            totals.TotalExempt = Math.Round(totals.TotalExempt + exempt, 2);
            totals.TotalVat = Math.Round(totals.TotalVat + vat, 2);
        }

        private void AddTimestampLog(
            List<UserActionLogDTO> logs,
            User? manager,
            DateTimeOffset timestamp,
            string actionType,
            decimal? amount,
            string? cashierName,
            string? cashierEmail)
        {
            logs.Add(new UserActionLogDTO
            {
                Name = $"{manager.UserFName} {manager.UserLName}",
                Action = actionType.ToString(),
                ManagerEmail = manager.UserEmail,
                CashierName = cashierName,
                CashierEmail = cashierEmail,
                Amount = amount.HasValue
                    ? string.Format(CultureInfo.InvariantCulture, "₱{0:N2}", amount.Value)
                    : null,
                ActionDate = timestamp.ToLocalTime().ToString("MM/dd/yyyy hh:mm tt"),
                SortActionDate = timestamp.ToLocalTime().DateTime
            });
        }

        public async Task<(List<SalesReportDTO> Data, string FilePath)> GetSalesReport(DateTime fromDate, DateTime toDate, string folderPath)
        {
            try
            {
                await InitializePDFServices();
                // Get the sales report data
                var salesData = await GetSalesReportData(fromDate, toDate);

                // Generate PDF
                var pdfBytes = _salesReportPDF.GenerateSalesReportPDF(salesData, fromDate, toDate);

                // BASE name (no suffix):
                var baseName = $"SalesReport_{fromDate:yyyyMMdd}_to_{toDate:yyyyMMdd}";

                // Append a timestamp so it's always unique
                var uniqueSuffix = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                var fileName = $"{baseName}_{uniqueSuffix}.pdf";

                // Ensure directory exists
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);
                // Save PDF file
                await File.WriteAllBytesAsync(filePath, pdfBytes);

                return (salesData, filePath);
            }
            catch (Exception ex)
            {
                // Log the error appropriately
                throw new Exception($"Error generating sales report: {ex.Message}", ex);
            }
        }

        private async Task<List<SalesReportDTO>> GetSalesReportData(DateTime fromDate, DateTime toDate)
        {
            // Convert DateTime to DateTimeOffset for proper comparison
            var startDate = new DateTimeOffset(fromDate.Date);
            var endDate = new DateTimeOffset(toDate.Date.AddDays(1).AddTicks(-1));

            var isTrainMode = await _auth.IsTrainMode();

            // Get all orders with necessary includes and switch to client evaluation
            var orders = await _dataContext.Order
                .Include(o => o.Items)
                    .ThenInclude(i => i.Menu)
                        .ThenInclude(m => m.Category)
                .ToListAsync(); // First get all data from database

            // Then filter and sort in memory
            var filteredOrders = orders
                .Where(o => o.IsTrainMode == isTrainMode &&
                           o.CreatedAt >= startDate &&
                           o.CreatedAt <= endDate)
                .OrderBy(o => o.InvoiceNumber)
                .ToList();

            var salesReport = new List<SalesReportDTO>();

            foreach (var order in filteredOrders)
            {
                // Skip cancelled orders entirely
                if (order.IsCancelled) continue;

                foreach (var item in order.Items.Where(i => !i.IsVoid)) // Exclude voided items
                {
                    // Skip if no menu item (shouldn't happen, but just in case)
                    if (item.Menu == null) continue;

                    // Calculate the return amount for this specific item if it was refunded
                    decimal itemTotal = (item.ItemPrice ?? 0m) * (item.ItemQTY ?? 0);
                    decimal returnAmount = 0m;
                    if (item.IsRefund && order.ReturnedAmount.HasValue && order.TotalAmount > 0)
                    {
                        // Proportional calculation based on the item's contribution to the total order
                        decimal returnRatio = itemTotal / order.TotalAmount;
                        returnAmount = order.ReturnedAmount.Value * returnRatio;
                    }

                    // Create a single report entry for the item
                    salesReport.Add(new SalesReportDTO
                    {
                        InvoiceDate = item.IsRefund && order.StatusChangeDate.HasValue ? order.StatusChangeDate.Value : order.CreatedAt, // Use return date if refunded
                        InvoiceNumber = order.InvoiceNumber,
                        MenuName = item.Menu.MenuName,
                        BaseUnit = item.Menu.BaseUnit ?? "",
                        Quantity = item.ItemQTY ?? 0,
                        Cost = item.IsRefund ? -item.Menu.MenuCost ?? 0m : item.Menu.MenuCost ?? 0m, // Cost remains the same whether sold or returned
                        Price = item.IsRefund ? -item.ItemPrice ?? 0m:  item.ItemPrice ?? 0m,
                        ItemGroup = item.Menu.Category?.CtgryName ?? "",
                        Barcode = item.Menu.SearchId,
                        IsReturned = item.IsRefund, // Use item's IsRefund flag
                        ReturnDate = item.IsRefund ? order.StatusChangeDate : null, // Include return date only if refunded
                        ReturnAmount = returnAmount // Include calculated return amount
                    });
                }
            }

            return salesReport;
        }
    }
}
