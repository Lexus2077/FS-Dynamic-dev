using System.Collections.Generic;
using System.Globalization;

namespace FS_Dynamic.Models
{
    public class BracketCompetition
    {
        public int id { get; set; }
        public string name { get; set; }
        public string status { get; set; }
        public List<string> disciplines { get; set; }
    }

    public class AvailableMatch
    {
        public string match_id { get; set; }
        public string match_label { get; set; }
        public string kind { get; set; }
        public string round_type { get; set; }
        public string stage_key { get; set; }
        public BracketTeamInfo team1 { get; set; }
        public BracketTeamInfo team2 { get; set; }
        public double? team1_time_seconds { get; set; }
        public int team1_busts { get; set; }
        public int team1_skips { get; set; }
        public double? team2_time_seconds { get; set; }
        public int team2_busts { get; set; }
        public int team2_skips { get; set; }
        public string display { get; set; }

        public bool team1_has_result
        {
            get { return team1_time_seconds.HasValue; }
        }

        public bool team2_has_result
        {
            get { return team2_time_seconds.HasValue; }
        }

        public string team1_result_display
        {
            get
            {
                return team1_has_result
                    ? string.Format(CultureInfo.InvariantCulture, "{0:F3}s | Б:{1} С:{2}",
                        team1_time_seconds.Value, team1_busts, team1_skips)
                    : "—";
            }
        }

        public string team2_result_display
        {
            get
            {
                return team2_has_result
                    ? string.Format(CultureInfo.InvariantCulture, "{0:F3}s | Б:{1} С:{2}",
                        team2_time_seconds.Value, team2_busts, team2_skips)
                    : "—";
            }
        }
    }

    public class BracketTeamInfo
    {
        public int id { get; set; }
        public string name { get; set; }
        public int number { get; set; }
    }

    public class SaveMatchResultResponse
    {
        public string match_id { get; set; }
        public int updated_slot { get; set; }
        public double? time_seconds { get; set; }
        public int busts { get; set; }
        public int skips { get; set; }
        public double? total_time_seconds { get; set; }
        public string match_status { get; set; }
        public int? winner { get; set; }
        public BracketTeamInfo winner_team { get; set; }
        public BracketTeamInfo loser_team { get; set; }
        public string next_match_winner { get; set; }
        public string next_match_loser { get; set; }
        public string message { get; set; }
    }
}
