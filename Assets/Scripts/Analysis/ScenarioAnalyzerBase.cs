using System.Collections.Generic;
using ScenarioSystem.Core;
using UnityEngine;

namespace ScenarioSystem.Analysis
{
	/// <summary>
	/// Базовый класс анализаторов с общими проверками журнала.
	/// </summary>
	public abstract class ScenarioAnalyzerBase : MonoBehaviour, IScenarioAnalyzer
	{
		public abstract string AnalyzerName { get; }

		public abstract ScenarioReport Analyze(IReadOnlyList<ScenarioLogEntry> history, int totalStepsInScenario);

		/// <summary>Добавляет в отчет предупреждения о неполном или «чужом» журнале.</summary>
		protected void AddDiagnostics(IReadOnlyList<ScenarioLogEntry> history, ScenarioMode analyzerMode, List<string> details)
		{
			ScenarioLogEntry started = null;
			bool finished = false;

			foreach (var entry in history)
			{
				if (entry.EventType == ScenarioEventType.ScenarioStarted && started == null) started = entry;
				if (entry.EventType == ScenarioEventType.ScenarioFinished) finished = true;
			}

			if (started == null)
			{
				details.Add("Предупреждение: в журнале нет события начала сценария — режим прохождения неизвестен.");
			}
			else if (started.ExtraData.TryGetValue(ScenarioLogEntry.KeyMode, out var mode))
			{
				if (mode != analyzerMode.ToString())
				{
					details.Add($"Примечание: журнал записан в режиме '{mode}', оценка выполнена по правилам '{analyzerMode}'.");
				}
			}
			else
			{
				details.Add("Предупреждение: в журнале не указан режим прохождения.");
			}

			if (!finished)
			{
				details.Add("Предупреждение: прохождение не завершено — оценка выполнена по зафиксированным шагам.");
			}
		}
	}
}
