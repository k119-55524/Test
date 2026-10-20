using System.Collections.Generic;
using ScenarioSystem.Core;

namespace ScenarioSystem.Logging
{
	/// <summary>
	/// Логгер: ведет журнал событий прохождения.
	/// </summary>
	public interface IScenarioLogger
	{
		/// <summary>Все записи текущего (или последнего завершенного) прохождения.</summary>
		IReadOnlyList<ScenarioLogEntry> Entries { get; }

		/// <summary>Добавляет запись в журнал.</summary>
		void Log(ScenarioLogEntry entry);

		/// <summary>Очищает журнал перед новым прохождением.</summary>
		void Clear();
	}
}
