using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;

namespace FS_Dynamic.Models
{
    public class Competition
    {
        public int id { get; set; }
        public string name { get; set; }
        public string status { get; set; }
    }

    public class Discipline
    {
        public string discipline { get; set; }
        public string discipline_name { get; set; }
    }

    public class Round
    {
        public int round_number { get; set; }
        public int team_count { get; set; }
        public int active_team_count { get; set; }
        public bool is_active => active_team_count > 0;
    }

    public class JockerTeam
    {
        public int id { get; set; }
        public int competition_id { get; set; }
        public string discipline { get; set; }
        public int round_number { get; set; }
        public int team_number { get; set; }
        public List<TeamMember> members { get; set; }
        public TeamResult results { get; set; }

        // Свойство для отображения в ComboBox (с номером команды в начале)
        public string team_members_display
        {
            get
            {
                string membersText;
                if (members != null && members.Count > 0)
                {
                    membersText = string.Join(", ", members.Select(m =>
                        $"{m.participant_number}. {m.athlete_name}"));
                }
                else
                {
                    membersText = "без состава";
                }

                // 🔴 Добавляем номер команды в начале
                return $"Команда {team_number}: {membersText}";
            }
        }
    }

    public class TeamMember
    {
        public int participant_id { get; set; }
        public string athlete_name { get; set; }
        public int participant_number { get; set; }
        public int position { get; set; }
    }

    public class TeamResult
    {
        // Поля, которые может возвращать сервер
        public int? id { get; set; } // ID из com_jocker_results
        public int? jocker_team_id { get; set; }
        public int? time_ms { get; set; }
        public int? busts { get; set; }
        public int? skips { get; set; }
        public int? total_time_ms { get; set; }
        public string judge_signature { get; set; }
        public DateTime? created_at { get; set; }

        // Вычисляемое свойство для отображения
        public double? time_seconds
        {
            get
            {
                if (time_ms.HasValue)
                {
                    return time_ms.Value / 1000.0;
                }
                else
                {
                    return null;
                }
            }
        }
    }

    public class ApiResponse<T>
    {
        public bool success { get; set; }
        public T data { get; set; }
        public string error { get; set; }
    }


}