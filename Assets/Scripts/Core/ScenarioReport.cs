using System.Collections.Generic;

namespace ScenarioSystem.Core
{
	/// <summary>
	/// Итоговый структурированный отчет прохождения сценария.
	/// Формируется анализатором (ScenarioAnalyzer) на основе журнала (ScenarioLogger.Entries).
	///
	/// Логика архитектуры:
	/// Отделяет математику подсчета очков от процесса игры.
	/// Позволяет сериализовать результат, выводить в UI или экспортировать во внешнюю аналитику.
	/// </summary>
	public class ScenarioReport
	{
		public ScenarioMode EvaluatedMode { get; }
		public string AnalyzerName { get; }
		public int TotalScore { get; }
		public int MaxScore { get; }
		public int TotalViolations { get; }
		public int CompletedStepsCount { get; }
		public int FailedStepsCount { get; }
		public int SkippedStepsCount { get; }
		public float DurationSeconds { get; }
		public IReadOnlyList<string> Details { get; }

		public ScenarioReport(
			ScenarioMode mode,
			string analyzerName,
			int totalScore,
			int maxScore,
			int totalViolations,
			int completedStepsCount,
			int failedStepsCount,
			int skippedStepsCount,
			float durationSeconds,
			List<string> details = null)
		{
			EvaluatedMode = mode;
			AnalyzerName = analyzerName;
			TotalScore = totalScore;
			MaxScore = maxScore;
			TotalViolations = totalViolations;
			CompletedStepsCount = completedStepsCount;
			FailedStepsCount = failedStepsCount;
			SkippedStepsCount = skippedStepsCount;
			DurationSeconds = durationSeconds;
			Details = details ?? new List<string>();
		}

		public override string ToString()
		{
			return $"[{AnalyzerName}] Score: {TotalScore}/{MaxScore} (Completed: {CompletedStepsCount}, Failed: {FailedStepsCount}, Skipped: {SkippedStepsCount}, Violations: {TotalViolations}) Time: {DurationSeconds:F1}s";
		}
	}
}
