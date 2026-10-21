using System.Collections.Generic;

namespace ScenarioSystem
{
	/// <summary>
	/// Режим «Обучение»: все этапы — одна общая группа, номера групп экзамена не учитываются.
	/// Ошибка — шаг остается активным, в журнал идет подсказка; порога нарушений нет.
	/// </summary>
	public class TrainingMode : ScenarioModeBase
	{
		public override ScenarioMode Mode => ScenarioMode.Training;
		public override string ModeName => "обучения";

		// Все этапы — одна общая группа
		protected override IReadOnlyList<StageGroup> Arrange(IReadOnlyList<ActivationStage> stages)
		{
			var group = new StageGroup { Title = "Все шаги" };
			group.Stages.AddRange(stages);
			return new[] { group };
		}

		protected override bool ShowOutline => true;

		// Ошибка: попытка засчитана, шаг остается активным, подсказка в журнал и игроку
		protected override void OnStepError(StepBase step, string message)
		{
			int attempt = step.RegisterAttempt();
			step.NotifyError();
			Log(ScenarioEventType.StepFailed, step, StepStatus.Active, attempt, $"{message}. Подсказка: {step.HintOnFailure}");
			ShowMessage($"Ошибка — {step.StepTitle}: {message}\nПодсказка: {step.HintOnFailure}");
		}
	}
}
