using Microsoft.Extensions.Options;
using RagnarokBotWeb.Configuration.Data;
using RagnarokBotWeb.Domain.Exceptions;
using System.Text;
using System.Text.Json;
using static RagnarokBotWeb.Application.Models.PayPal;

namespace RagnarokBotWeb.Domain.Services
{
    public class PayPalService
    {
        private readonly HttpClient _httpClient;
        private readonly PayPalConfig _config;

        public PayPalService(HttpClient httpClient, IOptions<AppSettings> options)
        {
            _httpClient = httpClient;
            var clientId = Environment.GetEnvironmentVariable("PAYPAL_CLIENT_ID") ?? Environment.GetEnvironmentVariable("PayPalClientId", EnvironmentVariableTarget.User);
            var secret = Environment.GetEnvironmentVariable("PAYPAL_CLIENT_SECRET") ?? Environment.GetEnvironmentVariable("PayPalSecret", EnvironmentVariableTarget.User);
            _config = new PayPalConfig
            {
                ClientId = clientId!,
                ClientSecret = secret!,
                BaseUrl = Environment.GetEnvironmentVariable("PAYPAL_URL") ?? options.Value.PayPalUrl
            };
        }

        // Obter token de acesso
        public async Task<string> GetAccessTokenAsync()
        {
            if (string.IsNullOrEmpty(_config.ClientId))
            {
                throw new DomainException("Empty clientId");
            }

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_config.BaseUrl}/v1/oauth2/token");

            // Credenciais em Base64
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            var parameters = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        };

            request.Content = new FormUrlEncodedContent(parameters);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = JsonSerializer.Deserialize<PayPalAccessToken>(responseContent);
                return tokenResponse.access_token;
            }

            throw new Exception($"Erro ao obter token: {responseContent}");
        }

        // Criar ordem de pagamento
        public async Task<PayPalOrderResponse> CreateOrderAsync(decimal amount, string currency, string description, string returnUrl, string cancelUrl)
        {
            var accessToken = await GetAccessTokenAsync();

            if (string.IsNullOrEmpty(currency)) currency = "USD";

            var orderRequest = new PayPalCreateOrderRequest
            {
                intent = "CAPTURE",
                purchase_units = new List<PayPalPurchaseUnit>
            {
                new PayPalPurchaseUnit
                {
                    amount = new PayPalAmount
                    {
                        value = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                        currency_code = currency
                    },
                    description = description
                }
            },
                application_context = new PayPalApplicationContext
                {
                    return_url = returnUrl,
                    cancel_url = cancelUrl,
                    brand_name = "Sua Loja",
                    shipping_preference = "NO_SHIPPING",
                    user_action = "PAY_NOW"
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_config.BaseUrl}/v2/checkout/orders");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("Prefer", "return=representation");
            request.Headers.Add("PayPal-Request-Id", Guid.NewGuid().ToString());

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var json = JsonSerializer.Serialize(orderRequest, options);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var orderResponse = JsonSerializer.Deserialize<PayPalOrderResponse>(responseContent, options);
                return orderResponse;
            }

            throw new Exception($"Erro ao criar ordem PayPal. Status: {response.StatusCode}, Resposta: {responseContent}");
        }


        // Capturar pagamento
        public async Task<dynamic> CaptureOrderAsync(string orderId)
        {
            var accessToken = await GetAccessTokenAsync();

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_config.BaseUrl}/v2/checkout/orders/{orderId}/capture");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("Prefer", "return=representation");

            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<dynamic>(responseContent);
            }

            throw new Exception($"Erro ao capturar pagamento: {responseContent}");
        }

        // Obter detalhes da ordem
        public async Task<dynamic> GetOrderDetailsAsync(string orderId)
        {
            var accessToken = await GetAccessTokenAsync();

            var request = new HttpRequestMessage(HttpMethod.Get, $"{_config.BaseUrl}/v2/checkout/orders/{orderId}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<dynamic>(responseContent);
            }

            throw new Exception($"Erro ao obter detalhes: {responseContent}");
        }
    }
}
