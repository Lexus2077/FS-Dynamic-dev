using System.Collections.Generic;

namespace FS_Dynamic.Models
{
    public class QualCompetition
    {
        public int id { get; set; }
        public string name { get; set; }
        public string status { get; set; }
        public List<string> disciplines { get; set; }
    }

    public class QualDiscipline
    {
        public string discipline { get; set; }
        public string mode { get; set; }
        public int has_free_routine { get; set; }

        public string display_name
        {
            get
            {
                switch (discipline)
                {
                    case "DS":
                        return "DS — Dynamic Solo";
                    case "D2W":
                        return "D2W — Dynamic 2-Way";
                    case "D4W":
                        return "D4W — Dynamic 4-Way";
                    default:
                        return discipline;
                }
            }
        }
    }

    public class QualRound
    {
        public int round_number { get; set; }
        public int team_count { get; set; }
        public bool has_results { get; set; }

        public string display
        {
            get
            {
                return "Раунд " + round_number
                    + (has_results ? " (" + team_count + " рез.)" : " (пусто)");
            }
        }
    }

    public class QualTeam
    {
        public int id { get; set; }
        public int number { get; set; }
        public string name { get; set; }

        public string display
        {
            get { return number + ". " + name; }
        }

        public QualExistingResult existing_result { get; set; }
    }

    public class QualExistingResult
    {
        public int? time_ms { get; set; }
        public int? busts { get; set; }
        public int? skips { get; set; }
        public int? total_time_ms { get; set; }

        public string time_display
        {
            get
            {
                if (!time_ms.HasValue)
                {
                    return "—";
                }
                int ms = time_ms.Value;
                return string.Format("{0:00}:{1:000}", ms / 1000, ms % 1000);
            }
        }
    }

    /// <summary>
    /// Строка ответа GET qualification.php (полная).
    /// </summary>
    public class QualificationResultRow
    {
        public int id { get; set; }
        public int competition_id { get; set; }
        public string discipline { get; set; }
        public int team_id { get; set; }
        public string team_name { get; set; }
        public int? team_number { get; set; }
        public int round_number { get; set; }
        public int? time_ms { get; set; }
        public double? time_seconds { get; set; }
        public int? busts { get; set; }
        public int? skips { get; set; }
        public int? total_time_ms { get; set; }
        public double? total_time_seconds { get; set; }
        public double? complexity_score { get; set; }
    }

    public class QualSaveResult
    {
        public string type { get; set; }
        public int? time_ms { get; set; }
        public int? busts { get; set; }
        public int? skips { get; set; }
        public int? total_time_ms { get; set; }
    }
}
