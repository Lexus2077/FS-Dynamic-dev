using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FS_Dynamic.Models;

namespace FS_Dynamic.Services
{
    internal class AuthService
    {
        private readonly string _apiBaseUrl = AppConfig.LegacyApiUrl;
        private readonly HttpClient _httpClient;

        public AuthService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        
        public async Task<AuthResponse> LoginAsync(string login, string password)
        {
            System.Diagnostics.Debug.WriteLine("=== AUTH SERVICE DEBUG ===");

            try
            {
                var requestData = new
                {
                    action = "login",
                    login = login,
                    password = password,
                    is_wpf = true
                };

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine($"Sending to: {_apiBaseUrl}auth.php");
                System.Diagnostics.Debug.WriteLine($"Request JSON: {json}");

                var response = await _httpClient.PostAsync(_apiBaseUrl + "auth.php", content);

                System.Diagnostics.Debug.WriteLine($"Response Status: {(int)response.StatusCode} {response.StatusCode}");

                var responseJson = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"Response Length: {responseJson.Length} chars");
                System.Diagnostics.Debug.WriteLine($"Raw Response: '{responseJson}'");

                // Проверяем основные проблемы
                if (string.IsNullOrWhiteSpace(responseJson))
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Empty response from server");
                    return new AuthResponse { Success = false, Error = "Empty response from server" };
                }

                if (responseJson.Trim().StartsWith("<"))
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Server returned HTML instead of JSON");
                    return new AuthResponse { Success = false, Error = "Server returned HTML error" };
                }

                // Пробуем распарсить JSON
                try
                {
                    var testParse = JToken.Parse(responseJson);
                    System.Diagnostics.Debug.WriteLine("✅ JSON is valid!");

                    if (testParse is JObject jobj)
                    {
                        System.Diagnostics.Debug.WriteLine("JSON Structure:");
                        foreach (var prop in jobj.Properties())
                        {
                            System.Diagnostics.Debug.WriteLine($"  {prop.Name}: {prop.Value}");
                        }
                    }

                    // Десериализуем нормально
                    var authResult = JsonConvert.DeserializeObject<AuthResponse>(responseJson);
                    if (authResult != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ AuthResult - Success: {authResult.Success}, User: {authResult.User != null}");
                        return authResult;
                    }
                }
                catch (JsonException jex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ JSON PARSE ERROR: {jex.Message}");
                    System.Diagnostics.Debug.WriteLine($"First 100 chars: {responseJson.Substring(0, Math.Min(100, responseJson.Length))}...");
                }

                return new AuthResponse { Success = false, Error = "Invalid server response format" };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ EXCEPTION: {ex}");
                return new AuthResponse { Success = false, Error = $"Request failed: {ex.Message}" };
            }
        }
    }
}