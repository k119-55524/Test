using System.Collections.Generic;
using ScenarioSystem.Core;

namespace ScenarioSystem.Analysis
{
	/// <summary>
	/// Анализатор: считает оценку по журналу.
	/// </summary>
	public interface IScenarioAnalyzer
	{
		/// <summary>Название алгоритма для отображения.</summary>
		string AnalyzerName { get; }

		/// <summary>Рассчитывает отчет по журналу.</summary>
		/// <param name="history">Записи журнала прохождения.</param>
		/// <param name="totalStepsInScenario">Количество шагов в сценарии.</param>
		ScenarioReport Analyze(IReadOnlyList<ScenarioLogEntry> history, int totalStepsInScenario);
	}
}
