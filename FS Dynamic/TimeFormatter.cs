using System;
using System.Diagnostics;

namespace FS_Dynamic
{
    public static class TimeFormatter
    {
        /// <summary>
        /// Парсит строку вида «СС:ммм» (секунды и миллисекунды) в миллисекунды.
        /// </summary>
        public static int ConvertTimeToMilliseconds(string timeText)
        {
            if (string.IsNullOrWhiteSpace(timeText) || timeText == "00:000" || timeText == "--:--")
            {
                Debug.WriteLine("Пустое время, возвращаем 0");
                return 0;
            }

            try
            {
                timeText = timeText.Trim();

                if (!timeText.Contains(":"))
                {
                    Debug.WriteLine($"Некорректный формат времени: '{timeText}' (нет двоеточия)");
                    return 0;
                }

                string[] parts = timeText.Split(':');

                if (parts.Length != 2)
                {
                    Debug.WriteLine($"Некорректный формат времени: '{timeText}' (не 2 части)");
                    return 0;
                }

                if (int.TryParse(parts[0], out int seconds) &&
                    int.TryParse(parts[1], out int milliseconds))
                {
                    if (milliseconds < 0 || milliseconds > 999)
                    {
                        milliseconds = Math.Max(0, Math.Min(999, milliseconds));
                    }

                    return (seconds * 1000) + milliseconds;
                }

                Debug.WriteLine($"Не удалось распарсить время: '{timeText}'");
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка преобразования времени '{timeText}': {ex.Message}");
                return 0;
            }
        }
    }
}
