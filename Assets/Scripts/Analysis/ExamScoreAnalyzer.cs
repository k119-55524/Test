using System;
using System.Collections.Generic;

namespace ScenarioSystem
{
	/// <summary>
	/// Оценка экзамена: 100 минус 10 за каждый Failed и 5 за каждый Skipped.
	/// </summary>
	public class ExamScoreAnalyzer : ScenarioAnalyzerBase
	{
		public override string AnalyzerName => "оценка экзамена";

		// 100 − 10 × Failed − 5 × Skipped, не ниже 0
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
					AnalyzerName,
					0, 100, 0, 0, 0, 0, 0f, details);
			}

			AddDiagnostics(history, ScenarioMode.Exam, details);
			duration = history[^1].Timestamp;

			// Отслеживаем финальный статус каждого шага
			var finalStepStatuses = new Dictionary<string, StepStatus>();
			var titles = new Dictionary<string, string>();

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
					if (!string.IsNullOrEmpty(entry.StepTitle))
						titles[entry.StepId] = entry.StepTitle;

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
				string name = titles.TryGetValue(kvp.Key, out var title) ? $"{kvp.Key} {title}" : kvp.Key;
				switch (kvp.Value)
				{
					case StepStatus.Completed:
						completedCount++;
						details.Add($"{name}: выполнено → 0");
						break;
					case StepStatus.Failed:
						failedCount++;
						details.Add($"{name}: провалено → −10");
						break;
					case StepStatus.Skipped:
						skippedCount++;
						details.Add($"{name}: пропущено → −5");
						break;
				}
			}

			int penalties = (failedCount * 10) + (skippedCount * 5);
			int totalScore = Math.Max(0, 100 - penalties);

			return new ScenarioReport(
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
