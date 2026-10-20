using ScenarioSystem.Core;
using System.Collections.Generic;

namespace ScenarioSystem.Analysis
{
	/// <summary>
	/// Оценка обучения: 10 / 5 / 0 баллов за шаг по номеру попытки.
	/// </summary>
	public class TrainingScoreAnalyzer : ScenarioAnalyzerBase
	{
		public override string AnalyzerName => "оценка обучения";

		private int ScoreForAttempt(int attempt) => attempt switch
		{
			1 => 10,
			2 => 5,
			_ => 0
		};

		public override ScenarioReport Analyze(IReadOnlyList<ScenarioLogEntry> history, int totalStepsInScenario)
		{
			var details = new List<string>();

			if (history == null || history.Count == 0)
			{
				details.Add("Предупреждение: журнал пуст. Прохождение не зафиксировано.");
				return new ScenarioReport(
					AnalyzerName,
					0, totalStepsInScenario * 10,
					0, 0, 0, 0, 0f, details);
			}

			AddDiagnostics(history, ScenarioMode.Training, details);

			float duration = history[^1].Timestamp;
			int totalViolations = 0;

			// Порядок первого упоминания шага — для стабильного вывода
			var stepOrder = new List<string>();
			var completedAttempt = new Dictionary<string, int>();
			var violationsPerStep = new Dictionary<string, int>();
			var titles = new Dictionary<string, string>();

			foreach (var entry in history)
			{
				if (string.IsNullOrEmpty(entry.StepId)) continue;

				if (!titles.ContainsKey(entry.StepId))
				{
					titles[entry.StepId] = entry.StepTitle;
					stepOrder.Add(entry.StepId);
				}

				switch (entry.EventType)
				{
					case ScenarioEventType.StepFailed:
					case ScenarioEventType.OutOfOrderViolation:
					case ScenarioEventType.HazardViolation:
						totalViolations++;
						violationsPerStep.TryGetValue(entry.StepId, out int v);
						violationsPerStep[entry.StepId] = v + 1;
						break;

					case ScenarioEventType.StepCompleted:
						if (entry.AttemptIndex <= 0)
						{
							details.Add($"Предупреждение: у шага '{entry.StepId}' не указан номер попытки — засчитан как первая.");
						}
						completedAttempt[entry.StepId] = entry.AttemptIndex <= 0 ? 1 : entry.AttemptIndex;
						break;
				}
			}

			int totalScore = 0;
			foreach (var stepId in stepOrder)
			{
				violationsPerStep.TryGetValue(stepId, out int violations);
				string title = string.IsNullOrEmpty(titles[stepId]) ? stepId : $"{stepId} {titles[stepId]}";

				if (completedAttempt.TryGetValue(stepId, out int attempt))
				{
					int score = ScoreForAttempt(attempt);
					totalScore += score;
					details.Add($"{title}: попытка {attempt}, нарушений {violations} → +{score}");
				}
				else
				{
					details.Add($"{title}: не выполнено, нарушений {violations} → 0");
				}
			}

			int completedCount = completedAttempt.Count;
			int maxScore = (totalStepsInScenario > 0 ? totalStepsInScenario : stepOrder.Count) * 10;

			return new ScenarioReport(
				AnalyzerName,
				totalScore,
				maxScore,
				totalViolations,
				completedCount,
				failedStepsCount: 0,
				skippedStepsCount: 0,
				duration,
				details);
		}
	}
}
