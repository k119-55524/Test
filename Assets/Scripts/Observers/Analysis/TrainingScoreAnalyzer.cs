using ScenarioSystem.Core;
using System.Collections.Generic;

namespace ScenarioSystem.Observers.Analysis
{
	/// <summary>
	/// Анализатор результатов для Режима 1 — «Тренировка» (ТЗ п. 4, п. 7).
	///
	/// Правила оценивания:
	/// - 10 баллов: шаг выполнен с первой попытки.
	/// - 5 баллов: шаг выполнен со второй попытки.
	/// - 0 баллов: шаг выполнен с третьей и последующих попыток (или не выполнен).
	/// - Максимальная оценка = 10 * количество шагов.
	///
	/// Контрольный пример из ТЗ:
	/// Шаг 1 с 1-й попытки (10), Шаг 2 со 2-й попытки (5), Шаг 3 с 3-й попытки (0).
	/// Итого по этим трем шагам = 15 баллов.
	/// </summary>
	public class TrainingScoreAnalyzer : ScenarioAnalyzer
	{
		public override string AnalyzerName => "Обучение (Training Evaluator)";

		public static int ScoreForAttempt(int attempt) => attempt switch
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
					ScenarioMode.Training, AnalyzerName,
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
				string title = string.IsNullOrEmpty(titles[stepId]) ? stepId : titles[stepId];

				if (completedAttempt.TryGetValue(stepId, out int attempt))
				{
					int score = ScoreForAttempt(attempt);
					totalScore += score;
					details.Add($"Шаг '{title}': выполнен с попытки {attempt}, нарушений {violations} -> +{score}");
				}
				else
				{
					details.Add($"Шаг '{title}': не выполнен, нарушений {violations} -> +0");
				}
			}

			int completedCount = completedAttempt.Count;
			int maxScore = (totalStepsInScenario > 0 ? totalStepsInScenario : stepOrder.Count) * 10;
			details.Add($"--- ИТОГО --- {totalScore}/{maxScore}");

			return new ScenarioReport(
				ScenarioMode.Training,
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
