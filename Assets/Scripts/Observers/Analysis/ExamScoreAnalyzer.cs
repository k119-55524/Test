using System;
using ScenarioSystem.Core;
using System.Collections.Generic;

namespace ScenarioSystem.Observers.Analysis
{
	/// <summary>
	/// Анализатор результатов для Режима 2 — «Экзамен» (ТЗ п. 4, п. 7).
	///
	/// Правила оценивания:
	/// - Базовый пул = 100 баллов.
	/// - Штраф за каждый заваленный шаг (Failed) = 10 баллов.
	/// - Штраф за каждый пропущенный шаг (Skipped) = 5 баллов.
	/// - Итог = max(0, 100 - сумма штрафов).
	/// - Нарушения в отчете = заваленные шаги + действия вне очереди + нарушения ТБ.
	///
	/// Контрольный пример из ТЗ:
	/// В группе из 4 шагов два завалены (Failed), оставшиеся два пропущены (Skipped).
	/// Штраф: 2 * 10 (Failed) + 2 * 5 (Skipped) = 30 баллов.
	/// Итоговая оценка = 100 - 30 = 70 баллов.
	/// </summary>
	public class ExamScoreAnalyzer : ScenarioAnalyzer
	{
		public override string AnalyzerName => "Экзамен (Exam Evaluator)";

		public override ScenarioReport Analyze(IReadOnlyList<ScenarioLogEntry> history, int totalStepsInScenario)
		{
			int completedCount = 0;
			int failedCount = 0;
			int skippedCount = 0;
			int violationsCount = 0;
			float duration = 0f;
			var details = new List<string>();

			if (history == null || history.Count == 0)
			{
				details.Add("Предупреждение: журнал пуст. Экзамен не пройден.");
				return new ScenarioReport(
					ScenarioMode.Exam,
					AnalyzerName,
					0, 100, 0, 0, 0, totalStepsInScenario, 0f, details);
			}

			AddDiagnostics(history, ScenarioMode.Exam, details);
			duration = history[^1].Timestamp;

			// Отслеживаем финальный статус каждого шага
			var finalStepStatuses = new Dictionary<string, StepStatus>();

			foreach (var entry in history)
			{
				if (entry.EventType == ScenarioEventType.OutOfOrderViolation ||
					entry.EventType == ScenarioEventType.HazardViolation ||
					(entry.EventType == ScenarioEventType.StepFailed && entry.ResultStatus == StepStatus.Failed))
				{
					violationsCount++;
				}

				if (!string.IsNullOrEmpty(entry.StepId))
				{
					if (entry.ResultStatus == StepStatus.Completed ||
						entry.ResultStatus == StepStatus.Failed ||
						entry.ResultStatus == StepStatus.Skipped)
					{
						finalStepStatuses[entry.StepId] = entry.ResultStatus;
					}
				}
			}

			foreach (var kvp in finalStepStatuses)
			{
				switch (kvp.Value)
				{
					case StepStatus.Completed:
						completedCount++;
						details.Add($"Шаг '{kvp.Key}': Успешно (0 штрафа)");
						break;
					case StepStatus.Failed:
						failedCount++;
						details.Add($"Шаг '{kvp.Key}': Завален (Failed) -> штраф -10");
						break;
					case StepStatus.Skipped:
						skippedCount++;
						details.Add($"Шаг '{kvp.Key}': Пропущен (Skipped) -> штраф -5");
						break;
				}
			}

			int penalties = (failedCount * 10) + (skippedCount * 5);
			int totalScore = Math.Max(0, 100 - penalties);

			details.Add($"--- ИТОГО --- Штрафы: {penalties} (Failed: -{failedCount * 10}, Skipped: -{skippedCount * 5}) | Итоговый балл: {totalScore}/100");

			return new ScenarioReport(
				ScenarioMode.Exam,
				AnalyzerName,
				totalScore,
				100,
				violationsCount,
				completedCount,
				failedCount,
				skippedCount,
				duration,
				details);
		}
	}
}
