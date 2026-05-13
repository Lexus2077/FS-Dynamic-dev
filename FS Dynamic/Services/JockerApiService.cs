// FS_Dynamic/Services/JockerApiService.cs
using FS_Dynamic.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FS_Dynamic.Services
{
    public class JockerApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl = AppConfig.CompetitionsApiUrl;

        public JockerApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        // 1. Получить список соревнований (режим Рулетка)
        public async Task<ApiResponse<List<Competition>>> GetCompetitions()
        {
            return await GetAsync<List<Competition>>("jocker_competitions.php");
        }

        // 2. Получить дисциплины соревнования
        public async Task<ApiResponse<List<Discipline>>> GetDisciplines(int competitionId)
        {
            return await GetAsync<List<Discipline>>($"jocker_disciplines.php?competition_id={competitionId}");
        }

        // 3. Получить раунды дисциплины
        public async Task<ApiResponse<List<Round>>> GetRounds(int competitionId, string discipline)
        {
            return await GetAsync<List<Round>>($"jocker_rounds.php?competition_id={competitionId}&discipline={discipline}");
        }

        // 4. Получить команды раунда (активные)
        public async Task<ApiResponse<List<JockerTeam>>> GetTeams(int competitionId, string discipline, int roundNumber)
        {
            //return await GetAsync<List<JockerTeam>>($"jocker_wpf.php?competition_id={competitionId}&discipline={discipline}&round_number={roundNumber}");
            return await GetAsync<List<JockerTeam>>($"jocker_teams.php?competition_id={competitionId}&discipline={discipline}&round_number={roundNumber}");
        }

        // 5. Сохранить результат команды
        public async Task<ApiResponse<TeamResult>> SaveTeamResult(
     int competitionId,
     string discipline,
     int roundNumber,
     int teamId,
     int timeMs,
     int busts,
     int skips)
        {
            // Согласно jocker_results.php, нужно отправлять поле save_team_result = true
            var data = new
            {
                save_team_result = true, // 🔴 ОБЯЗАТЕЛЬНОЕ ПОЛЕ!
                jocker_team_id = teamId,
                time_ms = timeMs,
                busts = busts,
                skips = skips
                // total_time_ms НЕ отправляем - сервер сам его рассчитает!
                // competition_id, discipline, round_number НЕ нужны - только jocker_team_id
            };

            Debug.WriteLine($"\n=== Отправка в jocker_results.php ===");
            Debug.WriteLine($"save_team_result: true");
            Debug.WriteLine($"jocker_team_id: {teamId}");
            Debug.WriteLine($"time_ms: {timeMs}");
            Debug.WriteLine($"busts: {busts}");
            Debug.WriteLine($"skips: {skips}");

            return await PostAsync<TeamResult>("jocker_results.php", data); // 🔴 ИЗМЕНИТЬ endpoint!
        }



        // Вспомогательные методы HTTP
        private async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/{endpoint}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<ApiResponse<T>>(content);
                }
                else
                {
                    return new ApiResponse<T>
                    {
                        success = false,
                        error = $"HTTP Error: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<T>
                {
                    success = false,
                    error = $"Exception: {ex.Message}"
                };
            }
        }

        private async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_apiBaseUrl}/{endpoint}";

                // Детальное логирование
                Debug.WriteLine($"\n📤 POST запрос на: {url}");
                Debug.WriteLine($"📦 JSON данные:");
                Debug.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));

                // Также можно вывести в консоль
                Console.WriteLine($"\n📤 POST {url}");
                Console.WriteLine($"📦 {json}");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"\n📥 Ответ сервера ({response.StatusCode}):");
                Debug.WriteLine(responseContent);

                Console.WriteLine($"\n📥 Ответ: {response.StatusCode}");
                Console.WriteLine(responseContent);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        return JsonConvert.DeserializeObject<ApiResponse<T>>(responseContent);
                    }
                    catch (Exception jsonEx)
                    {
                        Debug.WriteLine($"❌ Ошибка парсинга JSON: {jsonEx.Message}");
                        return new ApiResponse<T>
                        {
                            success = false,
                            error = $"JSON Parse Error: {jsonEx.Message}"
                        };
                    }
                }

                return new ApiResponse<T>
                {
                    success = false,
                    error = $"HTTP {response.StatusCode}: {responseContent}"
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 Exception in PostAsync: {ex.Message}");
                return new ApiResponse<T>
                {
                    success = false,
                    error = $"Exception: {ex.Message}"
                };
            }
        }
    }
}