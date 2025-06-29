using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace TelegramBot
{
    public class ApiService
    {
        private readonly string? _baseUrl;
        private readonly string? _apiKey;
        private readonly HttpClient _httpClient;

        public ApiService(IConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            
            _baseUrl = config["Api:BaseUrl"];
            _apiKey = config["Api:ApiKey"];
            
            if (string.IsNullOrEmpty(_baseUrl))
            {
                throw new InvalidOperationException("API Base URL not configured");
            }
            
            _httpClient = new HttpClient();
        }

        public async Task<JObject> GetTickerDataAsync(string ticker)
        {
            if (string.IsNullOrEmpty(ticker))
            {
                throw new ArgumentException("Ticker cannot be null or empty", nameof(ticker));
            }

            try
            {
                var url = $"{_baseUrl}?symbol={ticker}";
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    url += $"&apikey={_apiKey}";
                }

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var result = JObject.Parse(jsonString);
                
                if (result == null)
                {
                    throw new InvalidOperationException("Failed to parse API response");
                }

                return result;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP request error for ticker {ticker}: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching data for ticker {ticker}: {ex.Message}");
                throw;
            }
        }
    }
}
