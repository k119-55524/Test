using System;
using UnityEngine;
using System.Collections.Generic;

namespace ScenarioSystem
{
	/// <summary>
	/// Режим прохождения: этапы из менеджера по очереди, шаги этапа — в любом порядке. Как разбить этапы на группы
	/// и как реагировать на ошибку и нарушения, решает наследник (обучение, экзамен).
	/// </summary>
	public abstract class ScenarioModeBase
	{
		// Режим для журнала и выбора анализатора
		public abstract ScenarioMode Mode { get; }

		// Название в родительном падеже: «Старт обучения», «Конец экзамена»
		public abstract string ModeName { get; }

		// Подсвечивать активные шаги контуром
		protected abstract bool ShowOutline { get; }

		// Группа этапов при прохождении: название для журнала и этапы по очереди
		protected sealed class StageGroup
		{
			public string Title;
			public List<ActivationStage> Stages = new();
		}

		// Внедряется менеджером при старте
		private string _title;
		private IReadOnlyList<ActivationStage> _stages;
		private IReadOnlyList<StageGroup> _groups;
		private OutOfOrderPolicy _policy;
		private Action<ScenarioLogEntry> _log;
		private IScenarioView _view;
		private Action _finished;

		// Состояние прохождения
		private float _startTime;
		private bool _isRunning;
		private int _groupIndex;
		private int _stageIndex;
		private readonly HashSet<StepBase> _activeSteps = new();

		protected StageGroup CurrentGroup => _groups[_groupIndex];
		private ActivationStage CurrentStage => CurrentGroup.Stages[_stageIndex];
		private float Elapsed => Time.time - _startTime;

		// Все шаги этапов без повторов, по порядку
		public static IEnumerable<StepBase> StepsOf(IEnumerable<ActivationStage> stages)
		{
			var seen = new HashSet<StepBase>();
			foreach (var stage in stages)
				foreach (var step in StepsOf(stage))
					if (seen.Add(step))
						yield return step;
		}

		// Старт: title — название сценария, stages — этапы из менеджера, policy — правило вне очереди,
		// log — запись в журнал, view — вывод для игрока (может быть null), finished — конец прохождения
		public void Begin(string title, IReadOnlyList<ActivationStage> stages, OutOfOrderPolicy policy,
			Action<ScenarioLogEntry> log, IScenarioView view, Action finished)
		{
			_title = title;
			_stages = stages;
			_groups = Arrange(stages);
			_policy = policy;
			_log = log;
			_view = view;
			_finished = finished;

			_startTime = Time.time;
			_isRunning = true;
			_groupIndex = 0;
			_stageIndex = 0;
			_activeSteps.Clear();
			OnGroupStarted();

			_log(new ScenarioLogEntry(
				0f, "", "Сценарий", ScenarioEventType.ScenarioStarted, StepStatus.Pending,
				message: $"Старт {ModeName} «{_title}»",
				extraData: new Dictionary<string, string> { { ScenarioLogEntry.KeyMode, Mode.ToString() } }));

			ActivateNextStage();
		}

		// Остановка без отчета; шаги сбрасываются
		public void Stop()
		{
			_isRunning = false;
			_activeSteps.Clear();
			_view?.ShowActiveSteps(_activeSteps);
			foreach (var step in StepsOf(_stages))
				step.ResetStep();
		}

		// Действие игрока (isSuccess — условие выполнено)
		public void ReportStepAction(StepBase step, bool isSuccess, string message)
		{
			if (!_isRunning)
				return;

			if (!step.IsActive)
				HandleOutOfOrder(step);
			else if (isSuccess)
				CompleteStep(step, message);
			else
				OnStepError(step, message);
		}

		// Нарушение ТБ — учитывается в любой момент
		public void ReportSafetyViolation(StepBase step, string message)
		{
			if (!_isRunning)
				return;

			Log(ScenarioEventType.HazardViolation, step, step.CurrentStatus, step.AttemptCount, message);
			ShowMessage($"Нарушение ТБ — {step.StepTitle}: {message}");
			step.NotifyError();
			OnViolation();
		}

		// Как режим делит этапы на группы
		protected abstract IReadOnlyList<StageGroup> Arrange(IReadOnlyList<ActivationStage> stages);

		// Началась группа: сброс своего счетчика
		protected virtual void OnGroupStarted() { }

		// Ошибка на активном шаге
		protected abstract void OnStepError(StepBase step, string message);

		// Нарушение (вне очереди, ТБ, ошибка экзамена) учтено в группе
		protected virtual void OnViolation() { }

		// Запись о шаге в журнал
		protected void Log(ScenarioEventType type, StepBase step, StepStatus status, int attempt, string message) =>
			_log(new ScenarioLogEntry(Elapsed, step.StepId, step.StepTitle, type, status, attempt, message));

		// Запись о группе или этапе в журнал
		protected void Log(ScenarioEventType type, string title, StepStatus status, string message) =>
			_log(new ScenarioLogEntry(Elapsed, "", title, type, status, 0, message));

		// Сообщение игроку об ошибке или нарушении
		protected void ShowMessage(string text) => _view?.ShowMessage(text);

		// Шаг закрыт (Failed и т.п.): убрать из активных
		protected void CloseStep(StepBase step, StepStatus status)
		{
			step.Finish(status);
			_activeSteps.Remove(step);
			_view?.ShowActiveSteps(_activeSteps);
		}

		// Если в этапе не осталось активных шагов — следующий этап
		protected void ContinueIfStageDone()
		{
			if (_activeSteps.Count > 0)
				return;

			Log(ScenarioEventType.StageCompleted, CurrentStage.StageName, StepStatus.Completed,
				$"Этап {_stageIndex + 1} «{CurrentStage.StageName}» завершён");
			_stageIndex++;
			ActivateNextStage();
		}

		// Оставшиеся шаги текущей группы — Skipped, переход к следующей группе
		protected void SkipRestOfGroup(string reason)
		{
			foreach (var stage in CurrentGroup.Stages)
			{
				foreach (var step in StepsOf(stage))
				{
					if (step.CurrentStatus != StepStatus.Pending && step.CurrentStatus != StepStatus.Active)
						continue;

					step.Finish(StepStatus.Skipped);
					Log(ScenarioEventType.StepSkipped, step, StepStatus.Skipped, step.AttemptCount, reason);
				}
			}

			NextGroup();
			ActivateNextStage();
		}

		// Действие вне очереди: нарушение или игнор — по политике
		private void HandleOutOfOrder(StepBase step)
		{
			if (step.CurrentStatus != StepStatus.Pending)
				return;
			if (step.ResolveOutOfOrderPolicy(_policy) == OutOfOrderPolicy.IgnoreUntilActive)
				return;

			string reason = $"не завершён этап «{CurrentStage.StageName}»";
			Log(ScenarioEventType.OutOfOrderViolation, step, StepStatus.Pending, step.AttemptCount, reason);
			ShowMessage($"Нарушение порядка — {step.StepTitle}: {reason}");
			step.NotifyError();
			OnViolation();
		}

		// Шаг выполнен
		private void CompleteStep(StepBase step, string message)
		{
			int attempt = step.RegisterAttempt();
			CloseStep(step, StepStatus.Completed);
			Log(ScenarioEventType.StepCompleted, step, StepStatus.Completed, attempt, message);
			ContinueIfStageDone();
		}

		// Активировать шаги следующего непустого этапа; группа кончилась — следующая; после последней — конец
		private void ActivateNextStage()
		{
			_activeSteps.Clear();

			while (_isRunning)
			{
				if (_groupIndex >= _groups.Count)
				{
					Finish();
					return;
				}

				var group = CurrentGroup;
				if (_stageIndex == 0)
					Log(ScenarioEventType.GroupStarted, group.Title, StepStatus.Active, group.Title);

				if (_stageIndex >= group.Stages.Count)
				{
					Log(ScenarioEventType.GroupCompleted, group.Title, StepStatus.Completed, $"{group.Title} завершена");
					NextGroup();
					continue;
				}

				var stage = CurrentStage;
				var text = new System.Text.StringBuilder($"Этап {_stageIndex + 1} «{stage.StageName}»");
				foreach (var step in StepsOf(stage))
					text.Append($"\n    {step.StepTitle}");
				Log(ScenarioEventType.StageStarted, stage.StageName, StepStatus.Active, text.ToString());

				foreach (var step in StepsOf(stage))
				{
					if (step.CurrentStatus != StepStatus.Pending)
						continue;

					step.ActivateStep(ShowOutline);
					_activeSteps.Add(step);
					Log(ScenarioEventType.StepActivated, step, StepStatus.Active, step.AttemptCount, step.Description);
				}

				if (_activeSteps.Count > 0)
				{
					_view?.ShowActiveSteps(_activeSteps);
					return;
				}

				_stageIndex++;
			}
		}

		// Следующая группа с первого этапа
		private void NextGroup()
		{
			_groupIndex++;
			_stageIndex = 0;
			_activeSteps.Clear();
			OnGroupStarted();
		}

		// Конец прохождения
		private void Finish()
		{
			_isRunning = false;
			_view?.ShowActiveSteps(_activeSteps);
			_log(new ScenarioLogEntry(
				Elapsed, "", "Сценарий", ScenarioEventType.ScenarioFinished, StepStatus.Completed,
				message: $"Конец {ModeName} «{_title}»"));
			_finished();
		}

		// Шаги этапа: на объектах тестов и их дочерних
		private static IEnumerable<StepBase> StepsOf(ActivationStage stage)
		{
			foreach (var test in stage.Tests)
			{
				if (test == null)
					continue;

				foreach (var step in test.GetComponentsInChildren<StepBase>(true))
					yield return step;
			}
		}
	}

	/// <summary>
	/// Этап активации: тесты, которые активны одновременно и выполняются в любом порядке;
	/// номер группы экзамена — соседние этапы с одним номером образуют группу.
	/// </summary>
	[Serializable]
	public class ActivationStage
	{
		[Tooltip("Название этапа — для журнала и сообщений")]
		[SerializeField] private string stageName = "Этап";

		[Tooltip("Номер группы экзамена (1, 2, 3…) — не количество нарушений. Этапы подряд с одним номером — одна группа: у нее общий счетчик нарушений до порога (Violation Threshold). В обучении не учитывается")]
		[Min(1)]
		[SerializeField] private int examGroup = 1;

		[Tooltip("Тесты этапа — активны одновременно, выполняются в любом порядке. Перетащите объекты тестов со сцены")]
		[SerializeField] private List<GameObject> tests = new();

		public string StageName => stageName;
		public int ExamGroup => examGroup;
		public IReadOnlyList<GameObject> Tests => tests;

		// Номер группы меньше 1 (новый элемент списка) → 1
		public void ApplyDefaults()
		{
			if (examGroup < 1)
				examGroup = 1;
		}
	}
}
