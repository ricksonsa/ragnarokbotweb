using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;

namespace RagnarokBotClient
{
    public class WebApi
    {
        private readonly HttpClient _httpClient;

        public WebApi(Settings settings)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(settings.WebApiUrl)
            };
        }

        public void SetAuthToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<T> PostAsync<T>(string url, object body)
        {
            try
            {
                using StringContent jsonContent = new(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync())!;
                }
                else
                {
                    var messageString = await response.Content.ReadAsStringAsync();
                    var error = JsonConvert.DeserializeObject<ErrorDetail>(messageString);
                    if (error is not null) MessageBox.Show(string.Join('\n', error.details), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
            return default!;

        }

        public async Task<string> PostAsync(string url)
        {
            using StringContent jsonContent = new(JsonConvert.SerializeObject(new { Value = 1 }), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, jsonContent);
            Debug.WriteLine($"Http Request [{url}] responded with status [{response.StatusCode}]");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            return default!;
        }

        public async Task<string> PostAsync(string url, object body)
        {
            using StringContent jsonContent = new(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, jsonContent);
            Debug.WriteLine($"Http Request [{url}] responded with status [{response.StatusCode}]");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            return default!;
        }

        public async Task<HttpResponseMessage> PatchAsync(string url)
        {
            using StringContent jsonContent = new(JsonConvert.SerializeObject(new { Value = 1 }), Encoding.UTF8, "application/json");
            var response = await _httpClient.PatchAsync(url, jsonContent);
            return response;
        }

        public async Task<T?> GetAsync<T>(string url)
        {
            var response = await _httpClient.GetAsync(url);
            Debug.WriteLine($"Http Request [{url}] responded with status [{response.StatusCode}]");
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync())!;
            }

            return default;
        }

        public async Task<string> GetAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync()!;
            }

            throw new Exception(response.StatusCode.ToString());
        }

        public async Task<HttpContent> GetAsContentAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return response.Content;
            }

            return null!;
        }

        public async Task<T> PutAsync<T>(string url, object body)
        {
            using StringContent jsonContent = new(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
            var response = await _httpClient.PutAsync(url, jsonContent);
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync())!;
            }

            return default!;
        }

        public async Task<bool> DeleteAsync(string url)
        {
            var response = await _httpClient.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }


        class ErrorDetail
        {
            public int statusCode { get; set; }
            public string message { get; set; }
            public string details { get; set; }
        }

    }
}
