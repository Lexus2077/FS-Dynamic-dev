using FS_Dynamic.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FS_Dynamic.Services
{
    public class BracketApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl = "http://fsdynamic.ru/fs-dynamic-web/competitions/api";

        public BracketApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public Task<ApiResponse<List<BracketCompetition>>> GetCompetitions()
        {
            return GetAsync<List<BracketCompetition>>("qualification_competitions_list.php");
        }

        public Task<ApiResponse<List<AvailableMatch>>> GetAvailableMatches(int competitionId, string discipline)
        {
            return PostAsync<List<AvailableMatch>>("brackets.php", new
            {
                action = "get_available_matches",
                competition_id = competitionId,
                discipline = discipline
            });
        }

        public Task<ApiResponse<SaveMatchResultResponse>> SaveMatchResult(
            int competitionId,
            string discipline,
            string matchId,
            int teamSlot,
            int timeMs,
            int busts,
            int skips,
            string stageKey)
        {
            var payload = new Dictionary<string, object>
            {
                { "action", "save_match_result" },
                { "competition_id", competitionId },
                { "discipline", discipline },
                { "match_id", matchId },
                { "team_slot", teamSlot },
                { "time_ms", timeMs },
                { "busts", busts },
                { "skips", skips }
            };
            if (stageKey != null)
            {
                payload["stage_key"] = stageKey;
            }

            return PostAsync<SaveMatchResultResponse>("brackets.php", payload);
        }

        private async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync(_apiBaseUrl + "/" + endpoint);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (content.TrimStart().StartsWith("<"))
                    {
                        return new ApiResponse<T> { success = false, error = "Ошибка на сервере (HTML вместо JSON)" };
                    }
                    return JsonConvert.DeserializeObject<ApiResponse<T>>(content);
                }
                return new ApiResponse<T> { success = false, error = "HTTP Error: " + response.StatusCode };
            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException || ex is OperationCanceledException)
                {
                    return new ApiResponse<T> { success = false, error = "Нет связи с сервером (таймаут)" };
                }
                return new ApiResponse<T> { success = false, error = "Exception: " + ex.Message };
            }
        }

        private async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_apiBaseUrl + "/" + endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    if (responseContent.TrimStart().StartsWith("<"))
                    {
                        return new ApiResponse<T> { success = false, error = "Ошибка на сервере (HTML вместо JSON)" };
                    }
                    return JsonConvert.DeserializeObject<ApiResponse<T>>(responseContent);
                }
                return new ApiResponse<T> { success = false, error = "HTTP " + response.StatusCode + ": " + responseContent };
            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException || ex is OperationCanceledException)
                {
                    return new ApiResponse<T> { success = false, error = "Нет связи с сервером (таймаут)" };
                }
                return new ApiResponse<T> { success = false, error = "Exception: " + ex.Message };
            }
        }
    }
}
