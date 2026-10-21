using System;
using UnityEngine;
using System.Collections.Generic;

namespace ScenarioSystem
{
	/// <summary>
	/// Режим прохождения: ведет группы по очереди, в группе — этапы по очереди, шаги этапа — в любом порядке.
	/// Как реагировать на ошибку и нарушения, решает наследник (обучение, экзамен).
	/// </summary>
	public abstract class ScenarioModeBase : MonoBehaviour
	{
		// Режим для журнала и выбора анализатора
		public abstract ScenarioMode Mode { get; }

		// Название в родительном падеже: «Старт обучения», «Конец экзамена»
		public abstract string ModeName { get; }

		// Группы по очереди; в каждой — этапы по очереди
		public abstract IReadOnlyList<ScenarioGroup> Groups { get; }

		// Подсвечивать активные шаги контуром
		protected abstract bool ShowOutline { get; }

		// Внедряется менеджером при старте
		private string _title;
		private OutOfOrderPolicy _policy;
		private Action<ScenarioLogEntry> _log;
		private Action _finished;

		// Состояние прохождения
		private float _startTime;
		private bool _isRunning;
		private int _groupIndex;
		private int _stageIndex;
		private readonly HashSet<StepBase> _activeSteps = new();

		protected ScenarioGroup CurrentGroup => Groups[_groupIndex];
		protected int CurrentGroupNumber => _groupIndex + 1;
		private ActivationStage CurrentStage => CurrentGroup.Stages[_stageIndex];
		private float Elapsed => Time.time - _startTime;

		// Все шаги режима без повторов, по порядку групп и этапов
		public IEnumerable<StepBase> Steps
		{
			get
			{
				var seen = new HashSet<StepBase>();
				foreach (var group in Groups)
					foreach (var stage in group.Stages)
						foreach (var step in StepsOf(stage))
							if (seen.Add(step))
								yield return step;
			}
		}

		// Старт: title — название сценария, policy — правило вне очереди, log — запись в журнал, finished — конец прохождения
		public void Begin(string title, OutOfOrderPolicy policy, Action<ScenarioLogEntry> log, Action finished)
		{
			_title = title;
			_policy = policy;
			_log = log;
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
			foreach (var step in Steps)
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
			step.NotifyError();
			OnViolation();
		}

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

		// Шаг закрыт (Failed и т.п.): убрать из активных
		protected void CloseStep(StepBase step, StepStatus status)
		{
			step.Finish(status);
			_activeSteps.Remove(step);
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

			Log(ScenarioEventType.OutOfOrderViolation, step, StepStatus.Pending, step.AttemptCount,
				$"не завершён этап «{CurrentStage.StageName}»");
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
				if (_groupIndex >= Groups.Count)
				{
					Finish();
					return;
				}

				var group = CurrentGroup;
				if (_stageIndex == 0)
					Log(ScenarioEventType.GroupStarted, group.GroupName, StepStatus.Active,
						$"Группа {CurrentGroupNumber} «{group.GroupName}»");

				if (_stageIndex >= group.Stages.Count)
				{
					Log(ScenarioEventType.GroupCompleted, group.GroupName, StepStatus.Completed,
						$"Группа {CurrentGroupNumber} «{group.GroupName}» завершена");
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
					return;

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
	/// Группа сценария: этапы активации по очереди и порог нарушений (0 — без порога).
	/// </summary>
	[Serializable]
	public class ScenarioGroup
	{
		[Tooltip("Название группы (журнал, сообщения)")]
		[SerializeField] private string groupName = "Группа";

		[Tooltip("Порог нарушений в группе: при достижении оставшиеся шаги группы — Skipped. По умолчанию 2")]
		[Min(1)]
		[SerializeField] private int violationThreshold = 2;

		[Tooltip("Этапы активации группы — по очереди")]
		[SerializeField] private List<ActivationStage> stages = new();

		public string GroupName => groupName;
		public int ViolationThreshold => violationThreshold;
		public IReadOnlyList<ActivationStage> Stages => stages;

		// Для сериализации Unity
		public ScenarioGroup() { }

		// Группа из кода (обучение: одна общая группа без порога)
		public ScenarioGroup(string groupName, int violationThreshold, List<ActivationStage> stages)
		{
			this.groupName = groupName;
			this.violationThreshold = violationThreshold;
			this.stages = stages;
		}

		// Порог меньше 1 (новый элемент списка) → 2
		public void ApplyDefaults()
		{
			if (violationThreshold < 1)
				violationThreshold = 2;
		}
	}

	/// <summary>
	/// Этап активации: набор тестов, которые активны одновременно и выполняются в любом порядке.
	/// </summary>
	[Serializable]
	public class ActivationStage
	{
		[Tooltip("Название этапа (журнал, сообщения)")]
		[SerializeField] private string stageName = "Этап";

		[Tooltip("Тесты этапа — все активны сразу. Перетащите объекты тестов со сцены")]
		[SerializeField] private List<GameObject> tests = new();

		public string StageName => stageName;
		public IReadOnlyList<GameObject> Tests => tests;
	}
}
