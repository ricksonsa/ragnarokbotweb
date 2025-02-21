using Newtonsoft.Json;
using System.Text;

namespace RagnarokBotClient
{
    public class WebApi
    {
        private readonly HttpClient _httpClient;
        public HttpClient HttpClient { get { return _httpClient; } }

        public WebApi(Settings settings)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(settings.WebApiUrl)
            };
        }

        public async Task<T> PostAsync<T>(string url, object body)
        {
            using StringContent jsonContent = new(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var response = await HttpClient.PostAsync(url, jsonContent);
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync())!;
            }

            return default!;
        }

        public async Task<string> PostAsync(string url, object body)
        {
            using StringContent jsonContent = new(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var response = await HttpClient.PostAsync(url, jsonContent);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            return default!;
        }

        public async Task<HttpResponseMessage> PatchAsync(string url, string body)
        {
            using StringContent jsonContent = new(body, Encoding.UTF8, "text/plain");
            HttpClient.DefaultRequestHeaders.Add("Content-Type", "text/plain");
            var response = await HttpClient.PatchAsync(url, jsonContent);
            return response;
        }

        public async Task<T> GetAsync<T>(string url)
        {
            var response = await HttpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync())!;
            }

            return default!;
        }

        public async Task<string> GetAsync(string url)
        {
            var response = await HttpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync()!;
            }

            return default!;
        }

        public async Task<HttpContent> GetAsContentAsync(string url)
        {
            var response = await HttpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return response.Content;
            }

            return default!;
        }

        public async Task<T> PutAsync<T>(string url, object body)
        {
            using StringContent jsonContent = new(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            HttpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
            var response = await HttpClient.PutAsync(url, jsonContent);
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync())!;
            }

            return default!;
        }

        public async Task<bool> DeleteAsync(string url)
        {
            HttpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
            var response = await HttpClient.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }
    }
}
