using System.Collections.Generic;

namespace ScenarioSystem
{
	/// <summary>
	/// Анализатор: считает оценку по журналу.
	/// </summary>
	public interface IScenarioAnalyzer
	{
		// Название анализатора для вывода
		string AnalyzerName { get; }

		// Оценка по журналу; totalStepsInScenario — число шагов в сценарии
		ScenarioReport Analyze(IReadOnlyList<ScenarioLogEntry> history, int totalStepsInScenario);
	}
}
