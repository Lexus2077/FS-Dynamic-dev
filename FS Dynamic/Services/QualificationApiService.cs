using FS_Dynamic.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FS_Dynamic.Services
{
    public class QualificationApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl = AppConfig.CompetitionsApiUrl;

        public QualificationApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<ApiResponse<List<QualCompetition>>> GetCompetitions()
        {
            return await GetAsync<List<QualCompetition>>("qualification_competitions_list.php");
        }

        public async Task<ApiResponse<List<QualDiscipline>>> GetDisciplines(int competitionId)
        {
            var resp = await GetAsync<List<QualDiscipline>>("disciplines.php?competition_id=" + competitionId);
            if (resp.success && resp.data != null)
            {
                resp.data = resp.data
                    .Where(d => d.mode == "standard" || d.mode == "qualification")
                    .ToList();
            }
            return resp;
        }

        public Task<ApiResponse<List<QualRound>>> GetRounds(int competitionId, string discipline)
        {
            return GetAsync<List<QualRound>>(
                "qualification_rounds.php?competition_id=" + competitionId + "&discipline=" + Uri.EscapeDataString(discipline));
        }

        public Task<ApiResponse<List<QualTeam>>> GetTeams(int competitionId, string discipline)
        {
            return GetAsync<List<QualTeam>>(
                "teams.php?competition_id=" + competitionId + "&discipline=" + Uri.EscapeDataString(discipline));
        }

        public Task<ApiResponse<List<QualificationResultRow>>> GetResults(int competitionId, string discipline)
        {
            return GetAsync<List<QualificationResultRow>>(
                "qualification.php?competition_id=" + competitionId + "&discipline=" + Uri.EscapeDataString(discipline));
        }

        public Task<ApiResponse<QualSaveResult>> SaveResult(
            int competitionId,
            string discipline,
            int teamId,
            int roundNumber,
            int timeMs,
            int busts,
            int skips)
        {
            var payload = new
            {
                competition_id = competitionId,
                discipline = discipline,
                team_id = teamId,
                round_number = roundNumber,
                time_ms = timeMs,
                busts = busts,
                skips = skips
            };
            return PostAsync<QualSaveResult>("qualification.php", payload);
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
                        return new ApiResponse<T>
                        {
                            success = false,
                            error = "Ошибка на сервере (HTML вместо JSON)"
                        };
                    }
                    return JsonConvert.DeserializeObject<ApiResponse<T>>(content);
                }

                return new ApiResponse<T>
                {
                    success = false,
                    error = "HTTP Error: " + response.StatusCode
                };
            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException || ex is OperationCanceledException)
                {
                    return new ApiResponse<T>
                    {
                        success = false,
                        error = "Нет связи с сервером (таймаут)"
                    };
                }
                return new ApiResponse<T>
                {
                    success = false,
                    error = "Exception: " + ex.Message
                };
            }
        }

        private async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = _apiBaseUrl + "/" + endpoint;
                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    if (responseContent.TrimStart().StartsWith("<"))
                    {
                        return new ApiResponse<T>
                        {
                            success = false,
                            error = "Ошибка на сервере (HTML вместо JSON)"
                        };
                    }
                    return JsonConvert.DeserializeObject<ApiResponse<T>>(responseContent);
                }

                return new ApiResponse<T>
                {
                    success = false,
                    error = "HTTP " + response.StatusCode + ": " + responseContent
                };
            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException || ex is OperationCanceledException)
                {
                    return new ApiResponse<T>
                    {
                        success = false,
                        error = "Нет связи с сервером (таймаут)"
                    };
                }
                return new ApiResponse<T>
                {
                    success = false,
                    error = "Exception: " + ex.Message
                };
            }
        }
    }
}
