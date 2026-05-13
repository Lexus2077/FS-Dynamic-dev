namespace FS_Dynamic
{
    /// <summary>
    /// Режим работы главного окна таймера (выбирается после входа администратора).
    /// </summary>
    public enum CompetitionMode
    {
        /// <summary>Результаты в локальные файлы (Teams.txt / Rounds.txt).</summary>
        Local = 0,

        /// <summary>Рулетка (Joker) — API.</summary>
        Jocker = 1,

        /// <summary>Квалификация — API.</summary>
        Qualification = 2,

        /// <summary>Турнирная сетка — API.</summary>
        Bracket = 3,
    }
}
