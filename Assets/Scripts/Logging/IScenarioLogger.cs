using System.Collections.Generic;

namespace ScenarioSystem
{
	/// <summary>
	/// Логгер: ведет журнал событий прохождения.
	/// </summary>
	public interface IScenarioLogger
	{
		// Записи текущего или последнего прохождения
		IReadOnlyList<ScenarioLogEntry> Entries { get; }

		// Добавить запись
		void Log(ScenarioLogEntry entry);

		// Очистить перед новым прохождением
		void Clear();
	}
}
