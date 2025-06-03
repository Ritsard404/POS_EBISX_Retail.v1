using Microsoft.Extensions.Options;
using RestSharp;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;
using EBISX_POS.Services.DTO.Report;
using EBISX_POS.API.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace EBISX_POS.Services
{
    public class ReportService
    {
        private readonly ApiSettings _apiSettings;
        //private readonly RestClient _restClient;
        private readonly IReport _report;
        public ReportService(IOptions<ApiSettings> apiSettings)
        {
            _apiSettings = apiSettings.Value;

            //_restClient = new RestClient(_apiSettings.LocalAPI.BaseUrl);
            _report = App.Current.Services.GetRequiredService<IReport>();
        }


        public class CashTrackResponse
        {
            public string CashInDrawer { get; set; } = string.Empty;

            public string CurrentCashDrawer { get; set; } = string.Empty;
        }

        public async Task<(string CashInDrawer, string CurrentCashDrawer)> CashTrack()
        {
            try
            {
                return await _report.CashTrack(cashierEmail: CashierState.CashierEmail);

                //// Build URL and create a GET request
                //var url = $"{_apiSettings.LocalAPI.ReportEndpoint}/CashTrack";
                //var request = new RestRequest(url, Method.Get)
                //    .AddQueryParameter("cashierEmail", CashierState.CashierEmail);

                //// Execute the request and return the data, or an empty list if null
                //var response = await _restClient
                //    .ExecuteAsync<CashTrackResponse>(request);


                //if (!response.IsSuccessful || response.Data == null)
                //{
                //    Debug.WriteLine(
                //      $"[CashTrack] HTTP {(int)response.StatusCode}: {response.ErrorMessage}"
                //    );
                //    return (string.Empty, string.Empty);
                //}

                //return (
                //    response.Data.CashInDrawer,
                //    response.Data.CurrentCashDrawer
                //);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                return (string.Empty, string.Empty);
            }
        }

        public async Task<List<InvoiceDTO>> GetInvoicesByDateRange(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var getInvoicesByDateRange = await _report.GetInvoicesByDateRange(fromDate: fromDate, toDate: toDate);
                // Project each GetInvoicesDTO into your local InvoiceDTO
                var result = getInvoicesByDateRange.Select(x => new InvoiceDTO
                {
                    InvoiceNum = x.InvoiceNum,
                    InvoiceNumString = x.InvoiceNumString,
                    Date = x.Date,
                    Time = x.Time,
                    CashierEmail = x.CashierEmail,
                    CashierName = x.CashierName,
                    InvoiceStatus = x.InvoiceStatus
                }).ToList();

                return result;



                //// Build URL and create a GET request
                //var url = $"{_apiSettings.LocalAPI.ReportEndpoint}/GetInvoicesByDateRange";
                //var request = new RestRequest(url, Method.Get)
                //    .AddQueryParameter("fromDate", fromDate.ToString("yyyy-MM-dd"))
                //    .AddQueryParameter("toDate", toDate.ToString("yyyy-MM-dd"));


                //var response = await _restClient.ExecuteAsync<List<InvoiceDTO>>(request);


                //if (!response.IsSuccessful || response.Data == null)
                //{
                //    return new List<InvoiceDTO>();
                //}

                //return (
                //    response.Data
                //);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                return new List<InvoiceDTO>();
            }
        }
        public async Task<List<UserActionLogDTO>> UserActionLog(bool isManagerLog, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var actionLogs = await _report.UserActionLog(isManagerLog: isManagerLog, fromDate: fromDate, toDate: toDate);
                var result = actionLogs
                    .Select(x => new UserActionLogDTO
                    {
                        Name = x.Name,
                        ManagerEmail = x.ManagerEmail,
                        CashierName = x.CashierName,
                        CashierEmail = x.CashierEmail,
                        Amount = x.Amount,
                        Action = x.Action,
                        ActionDate = x.ActionDate
                    })
                    .ToList();

                return result;
                // Build URL and create a GET request
                //var url = $"{_apiSettings.LocalAPI.ReportEndpoint}/UserActionLog";
                //var request = new RestRequest(url, Method.Get)
                //    .AddQueryParameter("isManagerLog", isManagerLog)
                //    .AddQueryParameter("fromDate", fromDate.ToString("yyyy-MM-dd"))
                //    .AddQueryParameter("toDate", toDate.ToString("yyyy-MM-dd"));


                //var response = await _restClient.ExecuteAsync<List<UserActionLogDTO>>(request);


                //if (!response.IsSuccessful || response.Data == null)
                //{
                //    return new List<UserActionLogDTO>();
                //}

                //return (
                //    response.Data
                //);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                return new List<UserActionLogDTO>();
            }
        }

        public async Task<ZInvoiceDTO> ZInvoiceReport()
        {
            try
            {
                var apiResponse = await _report.ZInvoiceReport();

                // Map API DTO to client DTO
                return new ZInvoiceDTO
                {
                    BusinessName = apiResponse.BusinessName,
                    OperatorName = apiResponse.OperatorName,
                    AddressLine = apiResponse.AddressLine,
                    VatRegTin = apiResponse.VatRegTin,
                    Min = apiResponse.Min,
                    SerialNumber = apiResponse.SerialNumber,
                    ReportDate = apiResponse.ReportDate,
                    ReportTime = apiResponse.ReportTime,
                    StartDateTime = apiResponse.StartDateTime,
                    EndDateTime = apiResponse.EndDateTime,
                    BeginningSI = apiResponse.BeginningSI,
                    EndingSI = apiResponse.EndingSI,
                    BeginningVoid = apiResponse.BeginningVoid,
                    EndingVoid = apiResponse.EndingVoid,
                    BeginningReturn = apiResponse.BeginningReturn,
                    EndingReturn = apiResponse.EndingReturn,
                    TransactCount = apiResponse.TransactCount,
                    ResetCounter = apiResponse.ResetCounter,
                    ZCounter = apiResponse.ZCounter,
                    PresentAccumulatedSales = apiResponse.PresentAccumulatedSales,
                    PreviousAccumulatedSales = apiResponse.PreviousAccumulatedSales,
                    SalesForTheDay = apiResponse.SalesForTheDay,
                    SalesBreakdown = new SalesBreakdown
                    {
                        VatableSales = apiResponse.SalesBreakdown.VatableSales,
                        VatAmount = apiResponse.SalesBreakdown.VatAmount,
                        VatExemptSales = apiResponse.SalesBreakdown.VatExemptSales,
                        ZeroRatedSales = apiResponse.SalesBreakdown.ZeroRatedSales,
                        GrossAmount = apiResponse.SalesBreakdown.GrossAmount,
                        LessDiscount = apiResponse.SalesBreakdown.LessDiscount,
                        LessReturn = apiResponse.SalesBreakdown.LessReturn,
                        LessVoid = apiResponse.SalesBreakdown.LessVoid,
                        LessVatAdjustment = apiResponse.SalesBreakdown.LessVatAdjustment,
                        NetAmount = apiResponse.SalesBreakdown.NetAmount
                    },
                    DiscountSummary = new DiscountSummary
                    {
                        SeniorCitizen = apiResponse.DiscountSummary.SeniorCitizen,
                        SeniorCitizenCount = apiResponse.DiscountSummary.SeniorCitizenCount,
                        Pwd = apiResponse.DiscountSummary.PWD,
                        PwdCount = apiResponse.DiscountSummary.PWDCount,
                        Other = apiResponse.DiscountSummary.Other,
                        OtherCount = apiResponse.DiscountSummary.OtherCount
                    },
                    SalesAdjustment = new SalesAdjustment
                    {
                        Void = apiResponse.SalesAdjustment.Void,
                        VoidCount = apiResponse.SalesAdjustment.VoidCount,
                        Return = apiResponse.SalesAdjustment.Return,
                        ReturnCount = apiResponse.SalesAdjustment.ReturnCount
                    },
                    VatAdjustment = new VatAdjustment
                    {
                        ScTrans = apiResponse.VatAdjustment.SCTrans,
                        PwdTrans = apiResponse.VatAdjustment.PWDTrans,
                        RegDiscTrans = apiResponse.VatAdjustment.RegDiscTrans,
                        ZeroRatedTrans = apiResponse.VatAdjustment.ZeroRatedTrans,
                        VatOnReturn = apiResponse.VatAdjustment.VatOnReturn,
                        OtherAdjustments = apiResponse.VatAdjustment.OtherAdjustments
                    },
                    TransactionSummary = new TransactionSummary
                    {
                        CashInDrawer = apiResponse.TransactionSummary.CashInDrawer,
                        OtherPayments = apiResponse.TransactionSummary.OtherPayments?.Select(p => new OtherPayment
                        {
                            AmountString = p.AmountString,
                            Name = p.Name,
                        }).ToList() ?? new List<OtherPayment>()
                    },
                    OpeningFund = apiResponse.OpeningFund,
                    Withdrawal = apiResponse.Withdrawal,
                    PaymentsReceived = apiResponse.PaymentsReceived,
                    ShortOver = apiResponse.ShortOver
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ZInvoiceReport: {ex.Message}");
                return CreateDefaultZInvoice();
            }
        }

        private ZInvoiceDTO CreateDefaultZInvoice()
        {
            return new ZInvoiceDTO
            {
                BusinessName = string.Empty,
                OperatorName = string.Empty,
                AddressLine = string.Empty,
                VatRegTin = string.Empty,
                Min = string.Empty,
                SerialNumber = string.Empty,
                ReportDate = string.Empty,
                ReportTime = string.Empty,
                StartDateTime = string.Empty,
                EndDateTime = string.Empty,
                BeginningSI = string.Empty,
                EndingSI = string.Empty,
                BeginningVoid = string.Empty,
                EndingVoid = string.Empty,
                BeginningReturn = string.Empty,
                EndingReturn = string.Empty,
                ResetCounter = string.Empty,
                ZCounter = string.Empty,
                PresentAccumulatedSales = "₱0.00",
                PreviousAccumulatedSales = "₱0.00",
                SalesForTheDay = "₱0.00",
                SalesBreakdown = new SalesBreakdown
                {
                    VatableSales = "₱0.00",
                    VatAmount = "₱0.00",
                    VatExemptSales = "₱0.00",
                    ZeroRatedSales = "₱0.00",
                    GrossAmount = "₱0.00",
                    LessDiscount = "₱0.00",
                    LessReturn = "₱0.00",
                    LessVoid = "₱0.00",
                    LessVatAdjustment = "₱0.00",
                    NetAmount = "₱0.00"
                },
                DiscountSummary = new DiscountSummary
                {
                    SeniorCitizen = "₱0.00",
                    Pwd = "₱0.00",
                    Other = "₱0.00"
                },
                SalesAdjustment = new SalesAdjustment
                {
                    Void = "₱0.00",
                    Return = "₱0.00"
                },
                VatAdjustment = new VatAdjustment
                {
                    ScTrans = "₱0.00",
                    PwdTrans = "₱0.00",
                    RegDiscTrans = "₱0.00",
                    ZeroRatedTrans = "₱0.00",
                    VatOnReturn = "₱0.00",
                    OtherAdjustments = "₱0.00"
                },
                TransactionSummary = new TransactionSummary
                {
                    CashInDrawer = "₱0.00",
                    OtherPayments = new List<OtherPayment>()
                },
                OpeningFund = "₱0.00",
                Withdrawal = "₱0.00",
                PaymentsReceived = "₱0.00",
                ShortOver = "₱0.00"
            };
        }
        public async Task<XInvoiceDTO> XInvoiceReport()
        {
            try
            {
                var apiResponse = await _report.XInvoiceReport();

                return new XInvoiceDTO
                {
                    BusinessName = apiResponse.BusinessName,
                    OperatorName = apiResponse.OperatorName,
                    AddressLine = apiResponse.AddressLine,
                    VatRegTin = apiResponse.VatRegTin,
                    Min = apiResponse.Min,
                    SerialNumber = apiResponse.SerialNumber,
                    ReportDate = apiResponse.ReportDate,
                    ReportTime = apiResponse.ReportTime,
                    StartDateTime = apiResponse.StartDateTime,
                    EndDateTime = apiResponse.EndDateTime,
                    Cashier = apiResponse.Cashier,
                    BeginningOrNumber = apiResponse.BeginningOrNumber,
                    EndingOrNumber = apiResponse.EndingOrNumber,
                    TransactCount = apiResponse.TransactCount,
                    OpeningFund = apiResponse.OpeningFund,
                    Payments = new Payment
                    {
                        CashString = apiResponse.Payments.CashString,
                        OtherPayments = apiResponse.Payments.OtherPayments?.Select(p => new OtherPayment
                        {
                            Name = p.Name,
                            AmountString = p.AmountString,
                        }).ToList() ?? new List<OtherPayment>(),
                        Total = apiResponse.Payments.Total
                    },
                    VoidAmount = apiResponse.VoidAmount,
                    VoidCount = apiResponse.VoidCount,
                    Refund = apiResponse.Refund,
                    RefundCount = apiResponse.RefundCount,
                    Withdrawal = apiResponse.Withdrawal,
                    TransactionSummary = new TransactionSummary
                    {
                        CashInDrawer = apiResponse.TransactionSummary.CashInDrawer,
                        OtherPayments = apiResponse.TransactionSummary.OtherPayments?.Select(p => new OtherPayment
                        {
                            Name = p.Name,
                            AmountString = p.AmountString
                        }).ToList() ?? new List<OtherPayment>()
                    },
                    ShortOver = apiResponse.ShortOver
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in XInvoiceReport: {ex.Message}");
                return new XInvoiceDTO
                {
                    BusinessName = string.Empty,
                    OperatorName = string.Empty,
                    AddressLine = string.Empty,
                    VatRegTin = string.Empty,
                    Min = string.Empty,
                    SerialNumber = string.Empty,
                    ReportDate = string.Empty,
                    ReportTime = string.Empty,
                    StartDateTime = string.Empty,
                    EndDateTime = string.Empty,
                    Cashier = string.Empty,
                    BeginningOrNumber = string.Empty,
                    EndingOrNumber = string.Empty,
                    OpeningFund = string.Empty,
                    Payments = new Payment(),
                    VoidAmount = string.Empty,
                    Refund = string.Empty,
                    Withdrawal = string.Empty,
                    TransactionSummary = new TransactionSummary
                    {
                        CashInDrawer = string.Empty,
                        OtherPayments = new List<OtherPayment>()
                    },
                    ShortOver = string.Empty
                };
            }
        }

        public async Task<InvoiceDetailsDTO> GetInvoiceById(long invId)
        {
            try
            {
                var apiResponse = await _report.GetInvoiceById(invId);

                return new InvoiceDetailsDTO
                {
                    // Business Details
                    PrintCount = apiResponse.PrintCount,
                    RegisteredName = apiResponse.RegisteredName,
                    Address = apiResponse.Address,
                    VatTinNumber = apiResponse.VatTinNumber,
                    MinNumber = apiResponse.MinNumber,

                    // Invoice Details
                    InvoiceNum = apiResponse.InvoiceNum,
                    InvoiceDate = apiResponse.InvoiceDate,
                    OrderType = apiResponse.OrderType,
                    CashierName = apiResponse.CashierName,

                    // Items
                    Items = apiResponse.Items?.Select((item, index) => new ItemDTO
                    {
                        Qty = item.Qty,
                        itemInfos = item.itemInfos?.Select(info => new ItemInfoDTO
                        {
                            IsFirstItem = info.IsFirstItem,
                            Description = info.Description,
                            Amount = info.Amount
                        }).ToList() ?? new List<ItemInfoDTO>()
                    }).ToList() ?? new List<ItemDTO>(),

                    // Totals
                    TotalAmount = apiResponse.TotalAmount,
                    DiscountAmount = apiResponse.DiscountAmount,
                    DueAmount = apiResponse.DueAmount,
                    OtherPayments = apiResponse.OtherPayments?.Select(payment => new OtherPaymentDTO
                    {
                        SaleTypeName = payment.SaleTypeName,
                        Amount = payment.Amount
                    }).ToList() ?? new List<OtherPaymentDTO>(),
                    CashTenderAmount = apiResponse.CashTenderAmount,
                    TotalTenderAmount = apiResponse.TotalTenderAmount,
                    ChangeAmount = apiResponse.ChangeAmount,
                    VatExemptSales = apiResponse.VatExemptSales,
                    VatSales = apiResponse.VatSales,
                    VatAmount = apiResponse.VatAmount,

                    ElligiblePeopleDiscounts = apiResponse.ElligiblePeopleDiscounts ?? new List<string>(),

                    // POS Details
                    PosSerialNumber = apiResponse.PosSerialNumber,
                    DateIssued = apiResponse.DateIssued,
                    ValidUntil = apiResponse.ValidUntil
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetInvoiceById: {ex.Message}");
                return InvoiceDetailsDTO.CreateEmpty();
            }
        }
    }
}
