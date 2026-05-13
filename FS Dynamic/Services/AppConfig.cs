using System;
using System.Configuration;
using System.IO;

namespace FS_Dynamic.Services
{
    /// <summary>
    /// Читает конфигурацию из .env (в папке exe) → App.config → встроенный дефолт.
    /// Создайте файл .env рядом с FS Dynamic.exe, чтобы переопределить сервер без перекомпиляции.
    ///
    /// Доступные переменные .env:
    ///   API_BASE_URL              — домен (используется, если полные URL не заданы явно)
    ///   API_LEGACY_BASE_URL       — полный базовый URL для авторизации/команд/результатов
    ///   API_COMPETITIONS_BASE_URL — полный базовый URL для Joker/квалификации/сетки
    ///
    /// Примеры:
    ///   # fsdynamic.ru — проект в подпапке /fs-dynamic-web/
    ///   API_BASE_URL=http://fsdynamic.ru
    ///
    ///   # competition-service — проект в корне сервера
    ///   API_LEGACY_BASE_URL=http://competition-service.se.effps.ru/api
    ///   API_COMPETITIONS_BASE_URL=http://competition-service.se.effps.ru/competitions/api
    /// </summary>
    internal static class AppConfig
    {
        private static readonly string _legacyApiUrl;
        private static readonly string _competitionsApiUrl;

        static AppConfig()
        {
            var env = LoadEnvFile();

            // 1. Читаем явно заданные полные базовые URL
            string legacyFull = GetValue(env, "API_LEGACY_BASE_URL",
                ConfigurationManager.AppSettings["ApiLegacyBaseUrl"]);

            string compFull = GetValue(env, "API_COMPETITIONS_BASE_URL",
                ConfigurationManager.AppSettings["ApiCompetitionsBaseUrl"]);

            if (!string.IsNullOrWhiteSpace(legacyFull) && !string.IsNullOrWhiteSpace(compFull))
            {
                // Оба URL заданы явно — используем как есть
                _legacyApiUrl = legacyFull.TrimEnd('/') + "/";
                _competitionsApiUrl = compFull.TrimEnd('/');
                return;
            }

            // 2. Строим URL из базового домена + стандартные пути
            string domain = GetValue(env, "API_BASE_URL",
                ConfigurationManager.AppSettings["ApiBaseUrl"]);

            if (string.IsNullOrWhiteSpace(domain))
                domain = "http://fsdynamic.ru";

            domain = domain.TrimEnd('/');

            _legacyApiUrl = string.IsNullOrWhiteSpace(legacyFull)
                ? domain + "/fs-dynamic-web/api/"
                : legacyFull.TrimEnd('/') + "/";

            _competitionsApiUrl = string.IsNullOrWhiteSpace(compFull)
                ? domain + "/fs-dynamic-web/competitions/api"
                : compFull.TrimEnd('/');
        }

        /// <summary>URL для auth.php, wpf_teams.php, wpf_save_result.php</summary>
        public static string LegacyApiUrl => _legacyApiUrl;

        /// <summary>URL для brackets.php, qualification*.php, jocker*.php</summary>
        public static string CompetitionsApiUrl => _competitionsApiUrl;

        // ─────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────

        private static System.Collections.Generic.Dictionary<string, string> LoadEnvFile()
        {
            var result = new System.Collections.Generic.Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            var envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
            if (!File.Exists(envPath)) return result;

            foreach (var line in File.ReadAllLines(envPath))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("#") || !trimmed.Contains("=")) continue;

                var idx = trimmed.IndexOf('=');
                var key = trimmed.Substring(0, idx).Trim();
                var value = trimmed.Substring(idx + 1).Trim().Trim('"', '\'');
                result[key] = value;
            }

            return result;
        }

        private static string GetValue(
            System.Collections.Generic.Dictionary<string, string> env,
            string envKey,
            string appConfigValue)
        {
            if (env.TryGetValue(envKey, out var v) && !string.IsNullOrWhiteSpace(v))
                return v;
            return appConfigValue;
        }
    }
}
