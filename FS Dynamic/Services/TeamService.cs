using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FS_Dynamic.Models;
using FS_Dynamic.Services;

namespace FS_Dynamic.Services
{
    public class TeamService
    {
        private readonly HttpClient _httpClient;
        private static readonly string ApiBaseUrl = AppConfig.LegacyApiUrl;

        public TeamService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<List<Team>> GetTeamsAsync(string discipline = "")
        {
            try
            {
                string url = "wpf_teams.php";
                if (!string.IsNullOrEmpty(discipline))
                {
                    url += $"?discipline={discipline}";
                }

                var response = await _httpClient.GetAsync(ApiBaseUrl + url);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<TeamResponse>(responseJson);
                    if (result.Success)
                    {
                        return result.Teams;
                    }
                }

                return new List<Team>();
            }
            catch (Exception ex)
            {
                // Обработка ошибок
                return new List<Team>();
            }
        }
    }

   
}
