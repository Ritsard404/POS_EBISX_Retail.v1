using Avalonia.Controls;
using EBISX_POS.API.Models;
using EBISX_POS.API.Services.DTO.Order;
using EBISX_POS.API.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace EBISX_POS.Services
{
    public class OrderService
    {
        private readonly ApiSettings _apiSettings;
        private readonly RestClient _restClient; // Use RestClient instead of HttpClient
        private readonly IOrder _order;


        // Constructor to initialize RestClient and validate API configuration
        public OrderService(IOptions<ApiSettings> apiSettings)
        {
            _apiSettings = apiSettings.Value;

            _restClient = new RestClient();
            _order = App.Current.Services.GetRequiredService<IOrder>();
        }

        // Validates that the OrderEndpoint is configured in the API settings
        private void ValidateOrderEndpoint()
        {
            if (string.IsNullOrEmpty(_apiSettings.LocalAPI.OrderEndpoint))
            {
                throw new InvalidOperationException("Order endpoint is not configured.");
            }
        }

        // Executes the REST request and returns a tuple indicating success and a response message
        private async Task<(bool, string)> ExecuteRequestAsync(RestRequest request)
        {
            // Execute the request asynchronously
            var response = await _restClient.ExecuteAsync(request);

            // Return success if the response is successful
            if (response.IsSuccessful)
            {
                return (true, response.Content ?? string.Empty);
            }

            // Handle specific error status codes
            return response.StatusCode switch
            {
                HttpStatusCode.BadRequest => (false, response.Content ?? "Invalid request."),
                HttpStatusCode.Unauthorized => (false, "Unauthorized access. Please check your credentials."),
                _ => (false, $"Request failed with status code: {response.StatusCode}")
            };
        }

        // Calls the AddCurrentOrderVoid endpoint to void the current order
        public async Task<(bool, string)> AddCurrentOrderVoid(AddCurrentOrderVoidDTO voidOrder)
        {
            return await _order.AddCurrentOrderVoid(voidOrder);
            try
            {
                ValidateOrderEndpoint(); // Validate API endpoint configuration

                // Build URL and create request with JSON body
                var url = $"{_apiSettings.LocalAPI.OrderEndpoint}/AddCurrentOrderVoid";
                var request = new RestRequest(url, Method.Post).AddJsonBody(voidOrder);

                // Execute the request and return the result
                return await ExecuteRequestAsync(request);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error: {ex.Message}");
                return (false, "An unexpected error occurred.");
            }
        }

        // Calls the AddOrderItem endpoint to add a new order item
        public async Task<(bool, string)> AddOrderItem(AddOrderDTO addOrder)
        {
            return await _order.AddOrderItem(addOrder);
            try
            {
                ValidateOrderEndpoint(); // Validate API endpoint configuration

                // Build URL and create request with JSON body
                var url = $"{_apiSettings.LocalAPI.OrderEndpoint}/AddOrderItem";
                var request = new RestRequest(url, Method.Post).AddJsonBody(addOrder);

                // Execute the request and return the result
                return await ExecuteRequestAsync(request);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error: {ex.Message}");
                return (false, "An unexpected error occurred.");
            }
        }

        // Calls the EditQtyOrderItem endpoint to edit the quantity of an order item
        public async Task<(bool, string)> EditQtyOrderItem(EditOrderItemQuantityDTO editOrder)
        {
            return await _order.EditQtyOrderItem(editOrder);
            try
            {
                ValidateOrderEndpoint(); // Validate API endpoint configuration

                // Build URL and create request with JSON body using PUT method
                var url = $"{_apiSettings.LocalAPI.OrderEndpoint}/EditQtyOrderItem";
                var request = new RestRequest(url, Method.Put).AddJsonBody(editOrder);

                // Execute the request and return the result
                return await ExecuteRequestAsync(request);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error: {ex.Message}");
                return (false, "An unexpected error occurred.");
            }
        }

        // Calls the VoidOrderItem endpoint to void a specific order item
        public async Task<(bool, string)> VoidOrderItem(VoidOrderItemDTO voidOrder)
        {
            return await _order.VoidOrderItem(voidOrder);
            try
            {
                ValidateOrderEndpoint(); // Validate API endpoint configuration

                // Build URL and create request with JSON body using PUT method
                var url = $"{_apiSettings.LocalAPI.OrderEndpoint}/VoidOrderItem";
                var request = new RestRequest(url, Method.Put).AddJsonBody(voidOrder);

                // Execute the request and return the result
                var result = await ExecuteRequestAsync(request);
                return result.Item1
                    ? (true, result.Item2 ?? "Order voided successfully.")
                    : result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($" Error: {ex.Message}");
                return (false, "An unexpected error occurred.");
            }
        }

        // Calls the VoidOrderItem endpoint to void a specific order item
        public async Task<(bool, string)> CancelCurrentOrder(string managerEmail)
        {
            return await _order.CancelCurrentOrder(CashierState.CashierEmail, managerEmail);
            try
            {
                ValidateOrderEndpoint(); // Validate API endpoint configuration

                // Build URL correctly and encode the email to prevent any special character issues
                var url = $"{_apiSettings.LocalAPI.OrderEndpoint}/CancelCurrentOrder";

                // Create request using PUT method
                var request = new RestRequest(url, Method.Put)
                    .AddQueryParameter("cashierEmail", CashierState.CashierEmail)
                    .AddQueryParameter("managerEmail", managerEmail);

                // Execute the request and return the result
                var result = await ExecuteRequestAsync(request);
                return result.Item1
                    ? (true, result.Item2 ?? "Order Cancel successfully.")
                    : result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($" Error: {ex.Message}");
                return (false, "An unexpected error occurred.");
            }
        }

        public async Task<(bool, string)> RefundOrder(string managerEmail, long invoiceNumber)
        {
            return await _order.RefundOrder(managerEmail, invoiceNumber);
            try
            {
                ValidateOrderEndpoint(); // Validate API endpoint configuration

                // Build URL correctly and encode the email to prevent any special character issues
                var url = $"{_apiSettings.LocalAPI.OrderEndpoint}/RefundOrder";

                // Create request using PUT method
                var request = new RestRequest(url, Method.Put)
                    .AddQueryParameter("invoiceNumber", invoiceNumber)
                    .AddQueryParameter("managerEmail", managerEmail);

                // Execute the request and return the result
                var result = await ExecuteRequestAsync(request);
                return result.Item1
                    ? (true, result.Item2 ?? "Order Refund successfully.")
                    : result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($" Error: {ex.Message}");
                return (false, "An unexpected error occurred.");
            }
        }

        public async Task<(bool, string)> AddPwdScDiscount(AddPwdScDiscountDTO addPwdScDiscount)
        {
            return await _order.AddPwdScDiscount(addPwdScDiscount);
            try
            {
                ValidateOrderEndpoint(); // Validate API endpoint configuration

                // Build URL and create request with JSON body using PUT method
                var url = $"{_apiSettings.LocalAPI.OrderEndpoint}/AddPwdScDiscount";
                var request = new RestRequest(url, Method.Put).AddJsonBody(addPwdScDiscount);

                // Execute the request and return the result
                var result = await ExecuteRequestAsync(request);

                return result.Item1
                    ? (true, result.Item2 ?? "Order voided successfully.")
                    : result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($" Error: {ex.Message}");
                return (false, "An unexpected error occurred.");
            }
        }

        public async Task<(bool, string)> AddOtherDiscount(AddOtherDiscountDTO addOtherDiscount)
        {
            return await _order.AddOtherDiscount(addOtherDiscount);

            try
            {
                ValidateOrderEndpoint(); // Validate API endpoint configuration

                // Build URL and create request with JSON body using PUT method
                var url = $"{_apiSettings.LocalAPI.OrderEndpoint}/AddOtherDiscount";
                var request = new RestRequest(url, Method.Put).AddJsonBody(addOtherDiscount);

                // Execute the request and return the result
                var result = await ExecuteRequestAsync(request);

                return result.Item1
                    ? (true, result.Item2 ?? "Order voided successfully.")
                    : result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($" Error: {ex.Message}");
                return (false, "An unexpected error occurred.");
            }
        }

        public async Task<(bool, string)> PromoDiscount(string managerEmail, string promoCode)
        {
            return await _order.PromoDiscount(cashierEmail: CashierState.CashierEmail, managerEmail: managerEmail, promoCode: promoCode);

            try
            {
                ValidateOrderEndpoint(); // Validate API endpoint configuration

                // Build URL and create request with JSON body using PUT method
                var url = $"{_apiSettings.LocalAPI.OrderEndpoint}/PromoDiscount";
                var request = new RestRequest(url, Method.Put)
                    .AddQueryParameter("cashierEmail", CashierState.CashierEmail)
                    .AddQueryParameter("managerEmail", managerEmail)
                    .AddQueryParameter("promoCode", promoCode);

                // Execute the request and return the result
                var result = await ExecuteRequestAsync(request);

                if (result.Item1)
                {
                    // Remove surrounding quotes from the response content if present
                    var cleanedContent = result.Item2.Trim('\"');
                    return (true, cleanedContent);
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($" Error: {ex.Message}");
                return (false, "An unexpected error occurred.");
            }
        }

        public async Task<(bool, string)> AvailCoupon(string managerEmail, string couponCode)
        {
            return await _order.AvailCoupon(cashierEmail: CashierState.CashierEmail, managerEmail: managerEmail, couponCode: couponCode);

            try
            {
                ValidateOrderEndpoint(); // Validate API endpoint configuration

                // Build URL and create request with JSON body using PUT method
                var url = $"{_apiSettings.LocalAPI.OrderEndpoint}/AvailCoupon";
                var request = new RestRequest(url, Method.Put)
                    .AddQueryParameter("cashierEmail", CashierState.CashierEmail)
                    .AddQueryParameter("managerEmail", managerEmail)
                    .AddQueryParameter("couponCode", couponCode);

                // Execute the request and return the result
                var result = await ExecuteRequestAsync(request);

                if (result.Item1)
                {
                    // Remove surrounding quotes from the response content if present
                    var cleanedContent = result.Item2.Trim('\"');
                    return (true, cleanedContent);
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($" Error: {ex.Message}");
                return (false, "An unexpected error occurred.");
            }
        }

        // Calls the FinalizeOrder endpoint to void a specific order item
        public async Task<(bool, string, FinalizeOrderResponseDTO Response)> FinalizeOrder(FinalizeOrderDTO finalizeOrder)
        {
            return await _order.FinalizeOrder(finalizeOrder);

            try
            {
                // Validate the API endpoint configuration
                ValidateOrderEndpoint();

                // Build URL and create request with JSON body using PUT method
                var url = $"{_apiSettings.LocalAPI.OrderEndpoint}/FinalizeOrder";
                var request = new RestRequest(url, Method.Put)
                    .AddJsonBody(finalizeOrder);

                // Execute the request
                var response = await _restClient.ExecuteAsync<FinalizeOrderResponseDTO>(request);

                // Check if the response is successful and contains valid data
                if (response.IsSuccessful && response.Data != null)
                {
                    return (true, "Order finalized successfully.", response.Data);
                }
                else
                {
                    // If response is not successful, return the error message
                    return (false, response.ErrorMessage ?? "Failed to finalize order.", null);
                }
            }
            catch (Exception ex)
            {
                // Log the exception message
                Debug.WriteLine($"Error: {ex.Message}");
                return (false, "An unexpected error occurred.", null);
            }
        }


        // Calls the GetCurrentOrderItems endpoint to retrieve the current order items
        public async Task<List<GetCurrentOrderItemsDTO>> GetCurrentOrderItems()
        {
            return await _order.GetCurrentOrderItems(cashierEmail: CashierState.CashierEmail);

            try
            {
                ValidateOrderEndpoint(); // Validate API endpoint configuration
                // Build URL and create a GET request
                var url = $"{_apiSettings.LocalAPI.OrderEndpoint}/GetCurrentOrderItems";
                var request = new RestRequest(url, Method.Get)
                    .AddQueryParameter("cashierEmail", CashierState.CashierEmail);

                // Execute the request and return the data, or an empty list if null
                var response = await _restClient.ExecuteAsync<List<GetCurrentOrderItemsDTO>>(request);
                return response.Data ?? new List<GetCurrentOrderItemsDTO>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                return new List<GetCurrentOrderItemsDTO>();
            }
        }

        public async Task<List<string>> GetElligiblePWDSCDiscount()
        {
            return await _order.GetElligiblePWDSCDiscount(cashierEmail: CashierState.CashierEmail);

            try
            {
                ValidateOrderEndpoint(); // Validate API endpoint configuration
                // Build URL and create a GET request
                var url = $"{_apiSettings.LocalAPI.OrderEndpoint}/GetElligiblePWDSCDiscount";
                var request = new RestRequest(url, Method.Get)
                    .AddQueryParameter("cashierEmail", CashierState.CashierEmail);

                // Execute the request and return the data, or an empty list if null
                var response = await _restClient.ExecuteAsync<List<string>>(request);
                return response.Data ?? new List<string>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                return new List<string>();
            }
        }

    }
}
