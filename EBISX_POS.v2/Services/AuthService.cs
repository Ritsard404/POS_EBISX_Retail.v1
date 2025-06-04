using EBISX_POS.API.Services.DTO.Auth;
using EBISX_POS.API.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace EBISX_POS.Services
{
    public class AuthService
    {
        private readonly ApiSettings _apiSettings;
        private readonly RestClient _client;
        private readonly IAuth _auth;
        public AuthService(IOptions<ApiSettings> apiSettings)
        {
            _apiSettings = apiSettings.Value;

            //var options = new RestClientOptions(_apiSettings.LocalAPI.BaseUrl)
            //{
            //    CookieContainer = _cookieContainer
            //};

            //_client = new RestClient(options);
            _client = new RestClient();
            _auth = App.Current.Services.GetRequiredService<IAuth>();
        }


        public async Task<List<CashierDTO>> GetCashiersAsync()
        {
            try
            {
                var cashiers = await _auth.Cashiers();
                Debug.WriteLine(cashiers);
                return await _auth.Cashiers();

                //if (string.IsNullOrEmpty(_apiSettings?.LocalAPI?.AuthEndpoint))
                //{
                //    throw new InvalidOperationException("API settings are not properly configured.");
                //}

                //var request = new RestRequest($"{_apiSettings.LocalAPI.AuthEndpoint}/Cashiers", Method.Get);
                //var response = await _client.ExecuteAsync<List<CashierDTO>>(request);

                //if (response.IsSuccessful && response.Data != null)
                //{
                //    return response.Data;
                //}

                //Debug.WriteLine($"HTTP Error: {response.ErrorMessage}");
                //return new List<CashierDTO>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected Error: {ex.Message}");
                return new List<CashierDTO>();
            }
        }

        public class LoginResponseDTO
        {
            public bool isManager { get; set; }
            public string email { get; set; } = string.Empty;
            public string name { get; set; } = string.Empty;
        }
        public class HasPendingResponseDTO
        {
            public string CashierEmail { get; set; } = string.Empty;
            public string CashierName { get; set; } = string.Empty;
        }

        public async Task<(bool success, bool isManager, string email, string name)> LogInAsync(LogInDTO logInDTO)
        {
            try
            {
                var logIn = await _auth.LogIn(logInDTO);
                if(!logIn.success)
                    return (false, false, logIn.name, "");

                return (logIn.success, logIn.isManager, logIn.email, logIn.name);  // Fixed order

                if (string.IsNullOrEmpty(_apiSettings?.LocalAPI?.AuthEndpoint))
                {
                    throw new InvalidOperationException("API settings are not properly configured.");
                }

                var request = new RestRequest($"{_apiSettings.LocalAPI.AuthEndpoint}/LogIn", Method.Post)
                    .AddJsonBody(logInDTO);

                var response = await _client.ExecuteAsync<LoginResponseDTO>(request);

                if (response.IsSuccessful && response.Data != null)
                {
                    return (true, response.Data.isManager, response.Data.name, response.Data.email);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return (false, false, "Invalid credentials. Please try again.", "");
                }

                return (false, false, $"Login failed.", "");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected Error: {ex.Message}");
                return (false, false, "Unexpected error occurred.", "");
            }
        }
        public class LogOutResponseDTO
        {
            public string Message { get; set; } = string.Empty;
        }
        public async Task<(bool, string)> IsManagerValid(string managerEmail)
        {
            return await _auth.IsManagerValid(managerEmail);
        }
        public async Task<(bool, string)> LogOut(string managerEmail)
        {
            try
            {

                return await _auth.LogOut(new LogInDTO
                {
                    ManagerEmail = managerEmail,
                    CashierEmail = CashierState.CashierEmail,
                });
                
                if (string.IsNullOrEmpty(_apiSettings?.LocalAPI?.AuthEndpoint))
                {
                    throw new InvalidOperationException("API settings are not properly configured.");
                }

                // Build URL and create request with JSON body using PUT method
                var url = $"{_apiSettings.LocalAPI.AuthEndpoint}/LogOut";
                var request = new RestRequest(url, Method.Put)
                    .AddQueryParameter("cashierEmail", CashierState.CashierEmail)
                    .AddQueryParameter("managerEmail", managerEmail);


                var response = await _client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    return (true, response.Content ?? string.Empty);
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Return the error message provided by the API.
                    return (false, response.Content ?? string.Empty);
                }

                return (false, $"LogOut failed. Status Code: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected Error: {ex.Message}");
                return (false, "Unexpected error occurred.");
            }
        }

        public async Task<(bool, string, string)> HasPendingOrder()
        {
            try
            {
                return await _auth.HasPendingOrder();

                if (string.IsNullOrEmpty(_apiSettings?.LocalAPI?.AuthEndpoint))
                {
                    throw new InvalidOperationException("API settings are not properly configured.");
                }

                var request = new RestRequest($"{_apiSettings.LocalAPI.AuthEndpoint}/HasPendingOrder", Method.Get);
                var response = await _client.ExecuteAsync<HasPendingResponseDTO>(request);

                if (response.IsSuccessful && response.Data != null)
                {
                    return (true, response.Data.CashierName, response.Data.CashierEmail);
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    return (false, "No Pending Orders", "");
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return (false, "Invalid credentials. Please try again.", "");
                }

                return (false, $"Request failed. Status Code: {response.StatusCode}", "");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected Error: {ex.Message}");
                return (false, "Unexpected error occurred.", "");
            }
        }
        public class MessageResult
        {
            public string Message { get; set; } = string.Empty;
        }

        public async Task<(bool Success, string Message)> LoadDataAsync()
        {
            return await _auth.LoadData();

            if (string.IsNullOrEmpty(_apiSettings.LocalAPI.AuthEndpoint))
                throw new InvalidOperationException("API settings are not configured.");

            var request = new RestRequest($"{_apiSettings.LocalAPI.AuthEndpoint}/LoadData", Method.Post);
            var response = await _client.ExecuteAsync<MessageResult>(request);

            if (response.IsSuccessful && response.Data != null)
                return (true, response.Data.Message);

            if (response.StatusCode == HttpStatusCode.BadRequest)
                return (false, response.Content ?? "Bad request.");

            return (false, response.ErrorMessage ?? $"Error {response.StatusCode}");
        }

        public async Task<(bool Success, string Message)> CheckData()
        {
            var (success, message) = await _auth.CheckData();
            Debug.WriteLine($"CheckData → Success={success}, Message={message}");
            return (success, message);

            //if (string.IsNullOrEmpty(_apiSettings.LocalAPI.AuthEndpoint))
            //    throw new InvalidOperationException("API settings are not configured.");

            //var request = new RestRequest($"{_apiSettings.LocalAPI.AuthEndpoint}/CheckData", Method.Get);
            //var response = await _client.ExecuteAsync<MessageResult>(request);

            //if (response.IsSuccessful && response.Data != null)
            //    return (true, response.Data.Message);

            //if (response.StatusCode == HttpStatusCode.BadRequest)
            //    return (false, response.Content ?? "Bad request.");

            //return (false, response.ErrorMessage ?? $"Error {response.StatusCode}");
        }
        public async Task<(bool, string)> SetCashInDrawer(decimal cash)
        {
            try
            {
                Debug.WriteLine($"Setting cash in drawer for cashier: {CashierState.CashierEmail}");
                return await _auth.SetCashInDrawer(CashierState.CashierEmail, cash);

                if (string.IsNullOrEmpty(_apiSettings?.LocalAPI?.AuthEndpoint))
                {
                    throw new InvalidOperationException("API settings are not properly configured.");
                }

                // Build URL and create request with JSON body using PUT method
                var url = $"{_apiSettings.LocalAPI.AuthEndpoint}/SetCashInDrawer";
                var request = new RestRequest(url, Method.Put)
                    .AddQueryParameter("cashierEmail", CashierState.CashierEmail)
                    .AddQueryParameter("cash", cash);


                var response = await _client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    return (true, response.Content ?? string.Empty);
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Return the error message provided by the API.
                    return (false, response.Content ?? string.Empty);
                }

                return (false, $"LogOut failed. Status Code: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected Error: {ex.Message}");
                return (false, "Unexpected error occurred.");
            }
        }
        public async Task<(bool, string)> SetCashOutDrawer(decimal cash)
        {
            return await _auth.SetCashOutDrawer(CashierState.CashierEmail, cash);
            try
            {
                if (string.IsNullOrEmpty(_apiSettings?.LocalAPI?.AuthEndpoint))
                {
                    throw new InvalidOperationException("API settings are not properly configured.");
                }

                // Build URL and create request with JSON body using PUT method
                var url = $"{_apiSettings.LocalAPI.AuthEndpoint}/SetCashOutDrawer";
                var request = new RestRequest(url, Method.Put)
                    .AddQueryParameter("cashierEmail", CashierState.CashierEmail)
                    .AddQueryParameter("cash", cash);


                var response = await _client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    return (true, response.Content ?? string.Empty);
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Return the error message provided by the API.
                    return (false, response.Content ?? string.Empty);
                }

                return (false, $"LogOut failed. Status Code: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected Error: {ex.Message}");
                return (false, "Unexpected error occurred.");
            }
        }
        public async Task<(bool, string)> CashWithdrawDrawer(string managerEmail, decimal cash)
        {
            return await _auth.CashWithdrawDrawer(CashierState.CashierEmail, managerEmail, cash);
            try
            {
                if (string.IsNullOrEmpty(_apiSettings?.LocalAPI?.AuthEndpoint))
                {
                    throw new InvalidOperationException("API settings are not properly configured.");
                }

                // Build URL and create request with JSON body using PUT method
                var url = $"{_apiSettings.LocalAPI.AuthEndpoint}/CashWithdrawDrawer";
                var request = new RestRequest(url, Method.Put)
                    .AddQueryParameter("cashierEmail", CashierState.CashierEmail)
                    .AddQueryParameter("managerEmail", managerEmail)
                    .AddQueryParameter("cash", cash);


                var response = await _client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    return (true, response.Content ?? string.Empty);
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Return the error message provided by the API.
                    return (false, response.Content ?? string.Empty);
                }

                return (false, $"LogOut failed. Status Code: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected Error: {ex.Message}");
                return (false, "Unexpected error occurred.");
            }
        }
        public async Task<bool> IsCashedDrawer()
        {
            return await _auth.IsCashedDrawer(CashierState.CashierEmail);
            try
            {
                if (string.IsNullOrEmpty(_apiSettings?.LocalAPI?.AuthEndpoint))
                {
                    throw new InvalidOperationException("API settings are not properly configured.");
                }
                var request = new RestRequest($"{_apiSettings.LocalAPI.AuthEndpoint}/IsCashedDrawer", Method.Get)
                    .AddQueryParameter("cashierEmail", CashierState.CashierEmail);

                var response = await _client.ExecuteAsync(request);
                if (response.IsSuccessful)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected Error: {ex.Message}");
                return false;
            }
        }
        // DTO for train‐mode responses
        private class TrainModeResponseDTO
        {
            public bool isTrainMode { get; set; }
        }

        // GET /IsTrainMode
        public async Task<bool> IsTrainModeAsync()
        {
            return await _auth.IsTrainMode();
            var request = new RestRequest($"{_apiSettings.LocalAPI.AuthEndpoint}/IsTrainMode", Method.Get);
            var response = await _client.ExecuteAsync<TrainModeResponseDTO>(request);
            if (response.IsSuccessful && response.Data != null)
                return response.Data.isTrainMode;
            throw new InvalidOperationException($"Failed to get train mode: {response.StatusCode} {response.ErrorMessage}");
        }

        // PUT /ChangeMode?managerEmail={managerEmail}
        public async Task<bool> ChangeModeAsync(string managerEmail)
        {
            return await _auth.ChangeMode(managerEmail);
            var request = new RestRequest($"{_apiSettings.LocalAPI.AuthEndpoint}/ChangeMode", Method.Put)
                .AddQueryParameter("managerEmail", managerEmail);
            var response = await _client.ExecuteAsync<TrainModeResponseDTO>(request);
            if (response.IsSuccessful && response.Data != null)
                return response.Data.isTrainMode;
            throw new InvalidOperationException($"Failed to change mode: {response.StatusCode} {response.ErrorMessage}");
        }
    }
}
