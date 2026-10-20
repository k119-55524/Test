using System.Collections.Generic;

namespace ScenarioSystem.Core
{
	/// <summary>
	/// Итог оценки прохождения, который возвращает анализатор.
	/// </summary>
	public class ScenarioReport
	{
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
			return $"{TotalScore}/{MaxScore} ({AnalyzerName}) — выполнено {CompletedStepsCount}, провалено {FailedStepsCount}, " +
				$"пропущено {SkippedStepsCount}, нарушений {TotalViolations}, время {DurationSeconds:F1} с";
		}
	}
}
