using UnityEngine;
using System.Collections.Generic;

namespace ScenarioSystem
{
	/// <summary>
	/// Режим «Обучение»: все шаги в одной общей группе, этапы активации задаются в инспекторе.
	/// Ошибка — шаг остается активным, в журнал идет подсказка; порога нарушений нет.
	/// </summary>
	public class TrainingMode : ScenarioModeBase
	{
		[Tooltip("Этапы активации по очереди; тесты этапа активны одновременно")]
		[SerializeField] private List<ActivationStage> stages = new();

		private ScenarioGroup[] _groups;

		public override ScenarioMode Mode => ScenarioMode.Training;
		public override string ModeName => "обучения";

		// Одна общая группа из этапов
		public override IReadOnlyList<ScenarioGroup> Groups =>
			_groups ??= new[] { new ScenarioGroup("Все шаги", 0, stages) };

		protected override bool ShowOutline => true;

		// Ошибка: попытка засчитана, шаг остается активным, подсказка в журнал
		protected override void OnStepError(StepBase step, string message)
		{
			int attempt = step.RegisterAttempt();
			step.NotifyError();
			Log(ScenarioEventType.StepFailed, step, StepStatus.Active, attempt, $"{message}. Подсказка: {step.HintOnFailure}");
		}
	}
}
