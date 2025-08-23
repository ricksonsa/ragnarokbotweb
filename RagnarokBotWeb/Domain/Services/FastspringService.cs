using Newtonsoft.Json;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using static RagnarokBotWeb.Application.Models.FastSpringAccountResponseRoot;

namespace RagnarokBotWeb.Domain.Services
{
    public class FastspringService
    {
        private readonly HttpClient _httpClient;
        private readonly string _username;
        private readonly string _password;

        public FastspringService(HttpClient httpClient, string username, string password)
        {
            _httpClient = httpClient;
            _username = username;
            _password = password;

            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{_password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        }

        static string GetCountryCode(string countryName)
        {
            foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                RegionInfo ri = new RegionInfo(ci.Name);
                if (ri.EnglishName.Equals(countryName, StringComparison.OrdinalIgnoreCase))
                {
                    return ri.TwoLetterISORegionName;
                }
            }
            return null; // Not found
        }

        public async Task<FastspringAccountCreatedResponse?> CreateAccount(User user)
        {
            var account = new
            {
                contact = new
                {
                    first = user.Name,
                    last = user.LastName,
                    email = user.Email
                },
                language = "en",
                country = GetCountryCode(user.Country!)
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(account), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://api.fastspring.com/accounts", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error creating account: {error}");
            }

            var result = await response.Content.ReadAsStringAsync();
            var accountResponse = JsonConvert.DeserializeObject<FastspringAccountCreatedResponse>(result);

            return accountResponse;
        }

        public async Task<FastspringAccountCreatedResponse?> UpdateAccount(User user)
        {
            var account = new
            {
                contact = new
                {
                    first = user.Name,
                    last = user.LastName,
                    email = user.Email
                },
                language = "en",
                country = GetCountryCode(user.Country!)
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(account), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"https://api.fastspring.com/accounts/{user.FastspringAccountId}", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error creating account: {error}");
            }

            var result = await response.Content.ReadAsStringAsync();
            var accountResponse = JsonConvert.DeserializeObject<FastspringAccountCreatedResponse>(result);

            return accountResponse;
        }

        public async Task<FastSpringAccountResponse?> GetAccount(User user)
        {
            var response = await _httpClient.GetAsync($"https://api.fastspring.com/accounts/{user.FastspringAccountId}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error getting account: {error}");
            }

            var result = await response.Content.ReadAsStringAsync();
            var accountResponse = JsonConvert.DeserializeObject<FastSpringAccountResponse>(result);

            return accountResponse;
        }

        public async Task<FastspringSessionResponse?> CreateCheckoutSession(User user, string productId, int quantity = 1)
        {
            var account = await GetAccount(user);
            if (account is null) throw new DomainException("Please update your profile use add a payment");
            var session = new
            {
                account = account.Account,
                items = new[]
                {
                    new { product = productId, quantity = quantity }
                },
                returnUrl = "https://thescumbot.com/dashboard/payment-success"
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(session), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://api.fastspring.com/sessions", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Erro ao criar sessão FastSpring: {error}");
            }

            var result = await response.Content.ReadAsStringAsync();
            var sessionResponse = JsonConvert.DeserializeObject<FastspringSessionResponse>(result);

            return sessionResponse;
        }
    }

}
