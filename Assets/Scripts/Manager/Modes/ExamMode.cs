using System.Collections.Generic;

namespace ScenarioSystem
{
	/// <summary>
	/// Режим «Экзамен»: соседние этапы с одним номером группы — группа со своим счетчиком нарушений.
	/// Ошибка — Failed без повтора; на пороге оставшиеся шаги группы — Skipped, следующая группа.
	/// </summary>
	public class ExamMode : ScenarioModeBase
	{
		private readonly int _threshold;
		private int _violations;

		// threshold — порог нарушений группы
		public ExamMode(int threshold) => _threshold = threshold;

		public override ScenarioMode Mode => ScenarioMode.Exam;
		public override string ModeName => "экзамена";
		protected override bool ShowOutline => false;

		// Соседние этапы с одним номером группы — одна группа
		protected override IReadOnlyList<StageGroup> Arrange(IReadOnlyList<ActivationStage> stages)
		{
			var groups = new List<StageGroup>();
			foreach (var stage in stages)
			{
				if (groups.Count == 0 || groups[^1].Stages[^1].ExamGroup != stage.ExamGroup)
					groups.Add(new StageGroup { Title = $"Группа {stage.ExamGroup}" });

				groups[^1].Stages.Add(stage);
			}

			return groups;
		}

		// Новая группа — счетчик нарушений с нуля
		protected override void OnGroupStarted() => _violations = 0;

		// Ошибка: шаг Failed без повтора, это нарушение группы
		protected override void OnStepError(StepBase step, string message)
		{
			int attempt = step.RegisterAttempt();
			step.NotifyError();
			CloseStep(step, StepStatus.Failed);
			Log(ScenarioEventType.StepFailed, step, StepStatus.Failed, attempt, message);
			ShowMessage($"Провалено — {step.StepTitle}: {message}");

			if (!CountViolation())
				ContinueIfStageDone();
		}

		// Нарушение вне очереди или ТБ
		protected override void OnViolation() => CountViolation();

		// Учесть нарушение; на пороге — оставшиеся шаги группы Skipped. true — группа закрыта
		private bool CountViolation()
		{
			_violations++;
			if (_violations < _threshold)
				return false;

			string title = CurrentGroup.Title;
			string text = $"{title}: порог нарушений {_violations}/{_threshold} — оставшиеся шаги пропущены";
			Log(ScenarioEventType.GroupSkipped, title, StepStatus.Skipped, text);
			ShowMessage(text);
			SkipRestOfGroup("порог нарушений группы");
			return true;
		}
	}
}
