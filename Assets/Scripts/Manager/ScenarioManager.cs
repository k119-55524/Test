using System;
using System.Collections.Generic;
using ScenarioSystem.Core;
using ScenarioSystem.Analysis;
using ScenarioSystem.Logging;
using UnityEngine;

namespace ScenarioSystem.Manager
{
	/// <summary>
	/// Ведет прохождение сценария: этапы по очереди, шаги этапа в любом порядке.
	/// Применяет правила режима и пишет события в логгер; в конце запускает анализатор.
	/// </summary>
	public class ScenarioManager : MonoBehaviour
	{
		[Header("Режим")]
		[Tooltip("Training — обучение (повтор после ошибки), Exam — экзамен (Failed/Skipped, порог нарушений этапа)")]
		[SerializeField] private ScenarioMode currentMode = ScenarioMode.Training;

		[Tooltip("Действие на шаге вне его очереди: нарушение или игнорировать (шаг может переопределить у себя)")]
		[SerializeField] private OutOfOrderPolicy outOfOrderPolicy = OutOfOrderPolicy.RejectAsViolation;

		[Header("Сценарий")]
		[Tooltip("Общее название сценария: выводится при старте и в итоге («Старт обучения «…»»)")]
		[SerializeField] private string scenarioTitle = "Сценарий";

		[Tooltip("Этапы (группы) выполняются строго по очереди; тесты внутри этапа — все сразу, в любом порядке")]
		[SerializeField] private List<ScenarioGroup> groups = new();

		[Header("Логгер и анализатор")]
		[Tooltip("Логгер (компонент с IScenarioLogger): ведет журнал событий. Если пусто — берется логгер с этого объекта")]
		[SerializeField] private MonoBehaviour logger;

		[Tooltip("Анализатор (компонент с IScenarioAnalyzer) итоговой оценки в режиме Обучение. Пусто — оценка не рассчитывается")]
		[SerializeField] private MonoBehaviour trainingAnalyzer;

		[Tooltip("Анализатор (компонент с IScenarioAnalyzer) итоговой оценки в режиме Экзамен. Пусто — оценка не рассчитывается")]
		[SerializeField] private MonoBehaviour examAnalyzer;

		// Состояние прохождения
		private float _startTime;
		private bool _isRunning;
		private int _groupIndex;
		private int _groupViolations;
		private readonly HashSet<StepInstance> _activeSteps = new();

		private IScenarioLogger Logger => logger as IScenarioLogger;

		/// <summary>Анализатор итоговой оценки для текущего режима.</summary>
		private IScenarioAnalyzer ActiveAnalyzer => (IsExam ? examAnalyzer : trainingAnalyzer) as IScenarioAnalyzer;

		private bool IsExam => currentMode == ScenarioMode.Exam;
		private string CurrentGroupName =>
			_groupIndex < groups.Count && groups[_groupIndex] != null ? groups[_groupIndex].GroupName : string.Empty;
		private float Elapsed => Time.time - _startTime;
		private string ModeName => IsExam ? "экзамена" : "обучения";

		private IReadOnlyList<ScenarioLogEntry> LoggedEntries =>
			Logger != null ? Logger.Entries : (IReadOnlyList<ScenarioLogEntry>)Array.Empty<ScenarioLogEntry>();

		private void Awake()
		{
			if (logger == null) logger = GetComponent<IScenarioLogger>() as MonoBehaviour;
		}

		private void OnValidate()
		{
			// Unity создает новые элементы списка с нулями — порог этапа по умолчанию 2
			foreach (var group in groups) group?.ApplyDefaults();

			// В поля принимаются только компоненты с нужным интерфейсом
			logger = RequireInterface<IScenarioLogger>(logger, "Logger");
			trainingAnalyzer = RequireInterface<IScenarioAnalyzer>(trainingAnalyzer, "Training Analyzer");
			examAnalyzer = RequireInterface<IScenarioAnalyzer>(examAnalyzer, "Exam Analyzer");
		}

		private MonoBehaviour RequireInterface<T>(MonoBehaviour component, string fieldName)
		{
			if (component == null || component is T) return component;
			Debug.LogWarning($"[ScenarioManager] {component.GetType().Name} не реализует {typeof(T).Name} — поле {fieldName} очищено.", this);
			return null;
		}

		private void Start()
		{
			// Прохождение стартует сразу при запуске сцены в выбранном режиме
			StartScenario();
		}

		// ---------- Управление прохождением ----------

		/// <summary>Запускает сценарий с начала.</summary>
		private void StartScenario()
		{
			PrepareSteps();
			Logger?.Clear();
			_startTime = Time.time;
			_isRunning = true;

			WriteLog(new ScenarioLogEntry(
				0f, "", "Сценарий", ScenarioEventType.ScenarioStarted, StepStatus.Pending,
				message: $"Старт {ModeName} «{scenarioTitle}»",
				extraData: new Dictionary<string, string>
				{
					{ ScenarioLogEntry.KeyMode, currentMode.ToString() }
				}));

			ActivateNextGroup();
		}

		[ContextMenu("Старт сценария")]
		private void StartFromContextMenu()
		{
			if (!Application.isPlaying)
			{
				Debug.LogWarning("[ScenarioManager] Запуск сценария доступен только в режиме Play.", this);
				return;
			}
			StartScenario();
		}

		[ContextMenu("Стоп сценария")]
		private void StopFromContextMenu()
		{
			if (Application.isPlaying) StopScenario();
		}

		/// <summary>Останавливает прохождение без формирования отчета.</summary>
		private void StopScenario()
		{
			_isRunning = false;
			foreach (var step in _activeSteps) step.DeactivateStep();
			_activeSteps.Clear();
		}

		/// <summary>
		/// Повторная оценка завершенного прохождения другим анализатором, без перезапуска.
		/// </summary>
		public ScenarioReport ReEvaluate(IScenarioAnalyzer otherAnalyzer)
		{
			if (otherAnalyzer == null) return null;
			return otherAnalyzer.Analyze(LoggedEntries, CountSteps());
		}

		// ---------- Сообщения от шагов ----------

		/// <summary>
		/// Шаг сообщает о действии игрока. isSuccess — выполнено ли условие шага.
		/// </summary>
		public void ReportStepAction(StepInstance step, bool isSuccess, string message)
		{
			if (!_isRunning || step == null) return;

			if (!step.IsActive)
			{
				HandleOutOfOrderAction(step, message);
			}
			else if (isSuccess)
			{
				HandleStepSuccess(step, message);
			}
			else
			{
				HandleStepError(step, message);
			}
		}

		/// <summary>
		/// Шаг сообщает о нарушении ТБ, не связанном с его выполнением (например, вход в
		/// работающую опасную зону). Засчитывается в любой момент прохождения.
		/// </summary>
		public void ReportSafetyViolation(StepInstance step, string message)
		{
			if (!_isRunning || step == null) return;

			WriteLog(new ScenarioLogEntry(
				Elapsed, step.StepId, step.StepTitle,
				ScenarioEventType.HazardViolation, step.CurrentStatus,
				step.AttemptCount, message));

			step.NotifyError();
			RegisterGroupViolation();
		}

		private void HandleOutOfOrderAction(StepInstance step, string message)
		{
			if (step.CurrentStatus != StepStatus.Pending) return;
			if (step.ResolveOutOfOrderPolicy(outOfOrderPolicy) == OutOfOrderPolicy.IgnoreUntilActive) return;

			WriteLog(new ScenarioLogEntry(
				Elapsed, step.StepId, step.StepTitle,
				ScenarioEventType.OutOfOrderViolation, StepStatus.Pending,
				step.AttemptCount, $"не завершён этап «{CurrentGroupName}»"));

			step.NotifyError();
			RegisterGroupViolation();
		}

		private void HandleStepSuccess(StepInstance step, string message)
		{
			int attempt = step.RegisterAttempt();
			step.MarkCompleted();
			_activeSteps.Remove(step);

			WriteLog(new ScenarioLogEntry(
				Elapsed, step.StepId, step.StepTitle,
				ScenarioEventType.StepCompleted, StepStatus.Completed,
				attempt, message));

			CloseStepAndContinue();
		}

		private void HandleStepError(StepInstance step, string message)
		{
			int attempt = step.RegisterAttempt();
			step.NotifyError();

			if (!IsExam)
			{
				// Обучение: шаг остается активным, игрок повторяет; остальные активные шаги не трогаем
				WriteLog(new ScenarioLogEntry(
					Elapsed, step.StepId, step.StepTitle,
					ScenarioEventType.StepFailed, StepStatus.Active,
					attempt, $"{message}. Подсказка: {step.HintOnFailure}"));

				return;
			}

			// Экзамен: шаг получает Failed без повтора, ошибка идет в порог этапа
			step.MarkFailed();
			_activeSteps.Remove(step);

			WriteLog(new ScenarioLogEntry(
				Elapsed, step.StepId, step.StepTitle,
				ScenarioEventType.StepFailed, StepStatus.Failed,
				attempt, message));

			if (!RegisterGroupViolation())
			{
				CloseStepAndContinue();
			}
		}

		// ---------- Ход прохождения ----------

		/// <summary>
		/// Учитывает нарушение в текущем этапе. Экзамен: при достижении порога закрывает этап
		/// (оставшиеся шаги — Skipped) и возвращает true. Обучение: порога нет, всегда false.
		/// </summary>
		private bool RegisterGroupViolation()
		{
			if (!IsExam || !_isRunning || _groupIndex >= groups.Count) return false;

			_groupViolations++;
			var group = groups[_groupIndex];
			if (group == null || _groupViolations < group.ViolationThreshold) return false;

			string text = $"Этап {_groupIndex + 1} «{group.GroupName}»: порог нарушений {_groupViolations}/{group.ViolationThreshold} — оставшиеся шаги пропущены";

			WriteLog(new ScenarioLogEntry(
				Elapsed, "", group.GroupName,
				ScenarioEventType.GroupSkipped, StepStatus.Skipped,
				0, text));

			foreach (var test in group.Tests)
			{
				foreach (var step in StepsOf(test))
				{
					if (step == null) continue;
					if (step.CurrentStatus != StepStatus.Pending && step.CurrentStatus != StepStatus.Active) continue;

					step.MarkSkipped();
					WriteLog(new ScenarioLogEntry(
						Elapsed, step.StepId, step.StepTitle,
						ScenarioEventType.StepSkipped, StepStatus.Skipped,
						step.AttemptCount,
						"порог нарушений этапа"));
				}
			}

			_activeSteps.Clear();
			_groupIndex++;
			_groupViolations = 0;
			ActivateNextGroup();
			return true;
		}

		/// <summary>Шаг закрыт: если в текущем этапе не осталось активных шагов — переходим к следующему этапу.</summary>
		private void CloseStepAndContinue()
		{
			if (_activeSteps.Count > 0) return;

			var group = groups[_groupIndex];
			if (group != null) LogGroupCompleted(group);

			_groupIndex++;
			_groupViolations = 0;
			ActivateNextGroup();
		}

		/// <summary>
		/// Активирует все шаги ближайшего непустого этапа; после последнего этапа завершает прохождение.
		/// </summary>
		private void ActivateNextGroup()
		{
			_activeSteps.Clear();

			while (_isRunning)
			{
				if (_groupIndex >= groups.Count)
				{
					FinishScenario();
					return;
				}

				var group = groups[_groupIndex];
				if (group != null)
				{
					AnnounceGroup(group);

					foreach (var test in group.Tests)
					{
						foreach (var step in StepsOf(test))
						{
							if (step.CurrentStatus == StepStatus.Pending) ActivateStep(step);
						}
					}

					if (_activeSteps.Count > 0) return;

					LogGroupCompleted(group);
				}

				_groupIndex++;
				_groupViolations = 0;
			}
		}

		private void LogGroupCompleted(ScenarioGroup group)
		{
			WriteLog(new ScenarioLogEntry(
				Elapsed, "", group.GroupName,
				ScenarioEventType.GroupCompleted, StepStatus.Completed,
				0, $"Этап {_groupIndex + 1} «{group.GroupName}» завершён"));
		}

		private void ActivateStep(StepInstance step)
		{
			step.ActivateStep();
			_activeSteps.Add(step);

			WriteLog(new ScenarioLogEntry(
				Elapsed, step.StepId, step.StepTitle,
				ScenarioEventType.StepActivated, StepStatus.Active,
				step.AttemptCount,
				step.Description));
		}

		/// <summary>
		/// Запись «начат этап» с его составом: названия шагов по порядку, со сдвигом на 4 символа.
		/// </summary>
		private void AnnounceGroup(ScenarioGroup group)
		{
			var text = new System.Text.StringBuilder($"Этап {_groupIndex + 1} «{group.GroupName}»");
			foreach (var test in group.Tests)
			{
				foreach (var step in StepsOf(test))
				{
					text.Append($"\n    {step.StepTitle}");
				}
			}

			WriteLog(new ScenarioLogEntry(
				Elapsed, "", group.GroupName,
				ScenarioEventType.GroupStarted, StepStatus.Active,
				0, text.ToString()));
		}

		private void FinishScenario()
		{
			_isRunning = false;
			_activeSteps.Clear();

			WriteLog(new ScenarioLogEntry(
				Elapsed, "", "Сценарий",
				ScenarioEventType.ScenarioFinished, StepStatus.Completed,
				message: $"Конец {ModeName} «{scenarioTitle}»"));

			var activeAnalyzer = ActiveAnalyzer;
			if (activeAnalyzer != null)
			{
				var report = activeAnalyzer.Analyze(LoggedEntries, CountSteps());
				Debug.Log($"Итог {ModeName} «{scenarioTitle}»: {report}\n    {string.Join("\n    ", report.Details)}", this);
			}
			else
			{
				Debug.LogWarning($"[ScenarioManager] Анализатор для режима {currentMode} не задан — итоговая оценка не рассчитана.", this);
			}
		}

		// ---------- Подготовка ----------

		/// <summary>
		/// Сбрасывает состояние прохождения и все тесты из списка, привязывает их к менеджеру
		/// и назначает идентификаторы по порядку обхода списка: step_01, step_02…
		/// </summary>
		private void PrepareSteps()
		{
			_isRunning = false;
			_groupIndex = 0;
			_groupViolations = 0;
			_activeSteps.Clear();

			int number = 0;
			foreach (var step in EnumerateSteps())
			{
				number++;
				step.Bind(this, $"step_{number:00}");
				bool listenWhileInactive =
					step.ResolveOutOfOrderPolicy(outOfOrderPolicy) == OutOfOrderPolicy.RejectAsViolation;
				step.ResetStep(listenWhileInactive);
			}
		}

		/// <summary>Уникальные шаги сценария в порядке их первого появления в списке.</summary>
		private IEnumerable<StepInstance> EnumerateSteps()
		{
			var seen = new HashSet<StepInstance>();
			foreach (var group in groups)
			{
				if (group == null) continue;
				foreach (var test in group.Tests)
				{
					foreach (var step in StepsOf(test))
					{
						if (seen.Add(step)) yield return step;
					}
				}
			}
		}

		/// <summary>
		/// Шаги теста: StepInstance на самом объекте и на всех дочерних (включая выключенные).
		/// </summary>
		private StepInstance[] StepsOf(GameObject test)
		{
			return test != null ? test.GetComponentsInChildren<StepInstance>(true) : Array.Empty<StepInstance>();
		}

		/// <summary>Количество уникальных шагов в сценарии.</summary>
		private int CountSteps()
		{
			int count = 0;
			foreach (var _ in EnumerateSteps()) count++;
			return count;
		}

		private void WriteLog(ScenarioLogEntry entry)
		{
			Logger?.Log(entry);
		}
	}
}
