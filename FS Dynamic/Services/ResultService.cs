using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FS_Dynamic.Models;
using FS_Dynamic.Services;
using Newtonsoft.Json;

namespace FS_Dynamic.Services
{
    public class ResultService
    {
        private readonly HttpClient _httpClient;
        private static readonly string ApiBaseUrl = AppConfig.LegacyApiUrl;

        public ResultService()
        {
            _httpClient = new HttpClient();
        }

        //public async Task<bool> SaveTrainigResultsAsync(TrainingResult result)
        //{
        //    try
        //    {
        //        var json = JsonConvert.SerializeObject(result);
        //        var content = new StringContent(json, Encoding.UTF8, "application/json");

        //        var response = await _httpClient.PostAsync(ApiBaseUrl + "wpf_save_result.php", content);
        //        var responseJson = await response.Content.ReadAsStringAsync();

        //        if (response.IsSuccessStatusCode)
        //        {
        //            var resultResponse = JsonConvert.DeserializeObject<SaveResultResponse>(responseJson);
        //            return resultResponse?.Success ?? false;
        //        }
        //        return false;

        //    }
        //    catch (Exception ex)
        //    { 
        //        return false;
        //    }
        //}

        public async Task<bool> SaveTrainingResultAsync(TrainingResult result)
        {
            System.Diagnostics.Debug.WriteLine("=== SAVE TRAINING RESULT ===");

            try
            {
                var json = JsonConvert.SerializeObject(result);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine($"Saving to: {ApiBaseUrl}wpf_save_result.php");
                System.Diagnostics.Debug.WriteLine($"Request JSON: {json}");

                var response = await _httpClient.PostAsync(ApiBaseUrl + "wpf_save_result.php", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Response: {responseJson}");

                if (response.IsSuccessStatusCode)
                {
                    var resultResponse = JsonConvert.DeserializeObject<SaveResultResponse>(responseJson);
                    System.Diagnostics.Debug.WriteLine($"Save success: {resultResponse?.Success}");
                    return resultResponse?.Success ?? false;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("HTTP Error");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save Exception: {ex}");
                return false;
            }
        }
    }
}
