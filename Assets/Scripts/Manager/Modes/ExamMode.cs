using UnityEngine;
using System.Collections.Generic;

namespace ScenarioSystem
{
	/// <summary>
	/// Режим «Экзамен»: шаги разбиты на группы со своим порогом нарушений.
	/// Ошибка — Failed без повтора; на пороге оставшиеся шаги группы — Skipped, следующая группа.
	/// </summary>
	public class ExamMode : ScenarioModeBase
	{
		[Tooltip("Группы экзамена по очереди; в каждой — свои этапы активации")]
		[SerializeField] private List<ScenarioGroup> groups = new();

		private int _violations;

		public override ScenarioMode Mode => ScenarioMode.Exam;
		public override string ModeName => "экзамена";
		public override IReadOnlyList<ScenarioGroup> Groups => groups;
		protected override bool ShowOutline => false;

		// Новые группы в инспекторе — порог по умолчанию 2
		private void OnValidate()
		{
			foreach (var group in groups)
				group.ApplyDefaults();
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

			if (!CountViolation())
				ContinueIfStageDone();
		}

		// Нарушение вне очереди или ТБ
		protected override void OnViolation() => CountViolation();

		// Учесть нарушение; на пороге — оставшиеся шаги группы Skipped. true — группа закрыта
		private bool CountViolation()
		{
			var group = CurrentGroup;
			_violations++;
			if (_violations < group.ViolationThreshold)
				return false;

			Log(ScenarioEventType.GroupSkipped, group.GroupName, StepStatus.Skipped,
				$"Группа {CurrentGroupNumber} «{group.GroupName}»: порог нарушений {_violations}/{group.ViolationThreshold} — оставшиеся шаги пропущены");
			SkipRestOfGroup("порог нарушений группы");
			return true;
		}
	}
}
