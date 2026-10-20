using ScenarioSystem.Core;
using System.Collections.Generic;
using UnityEngine;

namespace ScenarioSystem.Observers.Analysis
{
	/// <summary>
	/// Базовый класс алгоритма оценивания (ТЗ п. 5). Компонент на объекте ScenarioManager:
	/// можно повесить несколько анализаторов — основной выбирается в поле менеджера,
	/// остальные доступны для повторной оценки завершенного прохождения.
	///
	/// Логика:
	/// Анализатор — чистая функция над готовым журналом: принимает записи и возвращает ScenarioReport.
	/// В процессе прохождения не участвует, поэтому один и тот же журнал можно оценить
	/// по правилам Обучения и по правилам Экзамена без повторного запуска сцены.
	/// Новый алгоритм = новый наследник с переопределенным Analyze.
	/// Если данных недостаточно, анализатор не падает, а добавляет предупреждение в отчет.
	/// </summary>
	public abstract class ScenarioAnalyzer : MonoBehaviour
	{
		/// <summary>Название алгоритма для отображения в интерфейсе.</summary>
		public abstract string AnalyzerName { get; }

		/// <summary>
		/// Рассчитывает отчет по журналу.
		/// </summary>
		/// <param name="history">Записи журнала прохождения.</param>
		/// <param name="totalStepsInScenario">Количество шагов в конфигурации режима.</param>
		public abstract ScenarioReport Analyze(IReadOnlyList<ScenarioLogEntry> history, int totalStepsInScenario);

		/// <summary>
		/// Общие диагностические проверки журнала. На баллы не влияют — только предупреждения в отчете.
		/// </summary>
		protected static void AddDiagnostics(IReadOnlyList<ScenarioLogEntry> history, ScenarioMode analyzerMode, List<string> details)
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
