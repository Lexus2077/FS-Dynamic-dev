using System;

namespace FS_Dynamic
{
    /// <summary>
    /// Данные таймера для окна демонстрации (Demo).
    /// </summary>
    public interface ITimerReadout
    {
        string TimeValue { get; }

        string FinalTimeValue { get; }

        string BustValue { get; }

        string SkipValue { get; }

        string SelectedTeam { get; }

        string SelectedRound { get; }

        event Action DataUpdated;
    }
}
