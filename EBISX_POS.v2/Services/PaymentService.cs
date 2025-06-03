using Avalonia.Data;
using EBISX_POS.API.Models;
using EBISX_POS.API.Services.DTO.Order;
using EBISX_POS.API.Services.DTO.Payment;
using EBISX_POS.API.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EBISX_POS.Services
{
    public class PaymentService
    {
        private readonly ApiSettings _apiSettings;
        //private readonly RestClient _restClient;
        private readonly IPayment _payment;

        public PaymentService(IOptions<ApiSettings> apiSettings)
        {
            _apiSettings = apiSettings.Value;

            //_restClient = new RestClient(_apiSettings.LocalAPI.BaseUrl);
            _payment = App.Current.Services.GetRequiredService<IPayment>();
        }
        public async Task<List<SaleType>> SaleTypes()
        {
            try
            { 
                return await _payment.SaleTypes();
                // Build URL and create a GET request
                //var url = $"{_apiSettings.LocalAPI.PaymentEndpoint}/SaleTypes";
                //var request = new RestRequest(url, Method.Get);

                //// Execute the request and return the data, or an empty list if null
                //var response = await _restClient.ExecuteAsync<List<SaleType>>(request);
                //return response.Data ?? new List<SaleType>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                return new List<SaleType>();
            }
        }

        public async Task<List<AlternativePayments>> GetAltPaymentsByOrderId(long orderId)
        {
            try
            {
                return await _payment.GetAltPaymentsByOrderId(orderId);
                // Build URL and create a GET request
                //var url = $"{_apiSettings.LocalAPI.PaymentEndpoint}/GetAltPaymentsByOrderId";
                //var request = new RestRequest(url, Method.Get)
                //    .AddQueryParameter("orderId", orderId);

                //// Execute the request and return the data, or an empty list if null
                //var response = await _restClient.ExecuteAsync<List<AlternativePayments>>(request);
                //return response.Data ?? new List<AlternativePayments>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                return new List<AlternativePayments>();
            }
        }

        public async Task<(bool, string)> AddAlternativePayments(List<AddAlternativePaymentsDTO> addAlternatives)
        {
            try
            {
                return await _payment.AddAlternativePayments(addAlternatives, CashierState.CashierEmail);

                // Build URL and create a GET request
                //var url = $"{_apiSettings.LocalAPI.PaymentEndpoint}/AddAlternativePayments";
                //var request = new RestRequest(url, Method.Post)
                //    .AddJsonBody(addAlternatives)
                //    .AddQueryParameter("cashierEmail", CashierState.CashierEmail);

                //// Execute the request and return the data, or an empty list if null
                //var response = await _restClient.ExecuteAsync(request);



                //// Return success if the response is successful
                //if (response.IsSuccessful)
                //{
                //    return (true, response.Content ?? string.Empty);
                //}

                //Debug.WriteLine($"Error: {response.Content}");

                //return response.StatusCode switch
                //{
                //    HttpStatusCode.BadRequest => (false, response.Content ?? "Invalid request."),
                //    HttpStatusCode.Unauthorized => (false, "Unauthorized access. Please check your credentials."),
                //    _ => (false, $"Request failed with status code: {response.StatusCode}")                
                //};

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                return (false, "An unexpected error occurred.");
            }
        }
    }
}
