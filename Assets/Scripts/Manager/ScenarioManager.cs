using System;
using System.Collections.Generic;
using ScenarioSystem.Core;
using ScenarioSystem.Observers.Analysis;
using ScenarioSystem.Observers.Logging;
using UnityEngine;

namespace ScenarioSystem.Manager
{
	/// <summary>
	/// Менеджер сценария: ведет прохождение по списку «группы → линии → тесты».
	///
	/// Логика:
	/// 1. Менеджер знает тесты только по своему списку. При подготовке прохождения передает
	///    каждому тесту ссылку на себя (StepInstance.Bind); дальше тест сам сообщает о действиях
	///    игрока (ReportStepAction) и о нарушениях ТБ (ReportSafetyViolation).
	/// 2. Цикл прохождения один для обоих режимов: линии идут строго по очереди, тесты линии
	///    активируются вместе и выполняются в любом порядке; следующая линия — когда закрыты все.
	/// 3. Режим задает только два правила:
	///    - ошибка на активном тесте: Обучение — тест остается активным (повтор),
	///      Экзамен — тест получает Failed и закрывается;
	///    - порог нарушений группы: только Экзамен — при достижении оставшиеся тесты группы
	///      получают Skipped, начинается следующая группа. В Обучении группы идут подряд как одна.
	/// 4. Журнал (ScenarioLogger) и анализаторы (ScenarioAnalyzer) — необязательные компоненты
	///    на этом же объекте: повешены — работают. Оценку считает анализатор по готовому журналу,
	///    поэтому прохождение можно переоценить другим анализатором без перезапуска (ReEvaluate).
	/// </summary>
	public class ScenarioManager : MonoBehaviour
	{
		[Header("Режим")]
		[Tooltip("Training — обучение (повтор после ошибки), Exam — экзамен (Failed/Skipped, порог группы)")]
		[SerializeField] private ScenarioMode currentMode = ScenarioMode.Training;

		[Tooltip("Действие на тесте вне его очереди: нарушение или игнорировать (тест может переопределить у себя)")]
		[SerializeField] private OutOfOrderPolicy outOfOrderPolicy = OutOfOrderPolicy.RejectAsViolation;

		[Header("Сценарий")]
		[Tooltip("Группы → линии → тесты. Линии выполняются по очереди, тесты линии — в любом порядке")]
		[SerializeField] private List<ScenarioGroup> groups = new();

		[Header("Журнал и оценка")]
		[Tooltip("Журнал прохождения. Если пусто — берется компонент ScenarioLogger с этого объекта")]
		[SerializeField] private ScenarioLogger journal;

		[Tooltip("Основной анализатор. Если пусто — первый ScenarioAnalyzer с этого объекта")]
		[SerializeField] private ScenarioAnalyzer analyzer;

		// Состояние прохождения
		private float _startTime;
		private bool _isRunning;
		private int _groupIndex;
		private int _lineIndex;
		private int _groupViolations;
		private readonly HashSet<StepInstance> _activeSteps = new();

		/// <summary>Прохождение завершено; отчет основного анализатора (null, если анализатор не задан).</summary>
		public event Action<ScenarioReport> OnScenarioCompleted;

		/// <summary>Сообщение для игрока: успех, ошибка с подсказкой, нарушение.</summary>
		public event Action<string> OnNotificationMessage;

		/// <summary>Изменился набор активных тестов или состояние прохождения.</summary>
		public event Action OnStateChanged;

		public ScenarioMode CurrentMode => currentMode;
		public bool IsRunning => _isRunning;
		public IReadOnlyCollection<StepInstance> ActiveSteps => _activeSteps;
		public IReadOnlyList<ScenarioGroup> Groups => groups;
		public int CurrentGroupIndex => _groupIndex;
		public int GroupViolationsCount => _groupViolations;
		public ScenarioLogger Journal => journal;
		public ScenarioAnalyzer Analyzer => analyzer;

		/// <summary>Все анализаторы на объекте (для повторной оценки).</summary>
		public ScenarioAnalyzer[] Analyzers => GetComponents<ScenarioAnalyzer>();

		private bool IsExam => currentMode == ScenarioMode.Exam;
		private float Elapsed => Time.time - _startTime;
		private int LogGroupIndex => IsExam ? _groupIndex : 0;

		private IReadOnlyList<ScenarioLogEntry> JournalEntries =>
			journal != null ? journal.Entries : (IReadOnlyList<ScenarioLogEntry>)Array.Empty<ScenarioLogEntry>();

		private void Awake()
		{
			if (journal == null) journal = GetComponent<ScenarioLogger>();
			if (analyzer == null) analyzer = GetComponent<ScenarioAnalyzer>();
		}

		private void Start()
		{
			// Готовим тесты, но не стартуем автоматически — запуск из UI
			PrepareSteps();
		}

		// ---------- Управление прохождением ----------

		/// <summary>Переключает режим (останавливает текущее прохождение).</summary>
		public void SetMode(ScenarioMode newMode)
		{
			if (_isRunning) StopScenario();
			currentMode = newMode;
			PrepareSteps();
			OnStateChanged?.Invoke();
		}

		/// <summary>Запускает сценарий с начала.</summary>
		public void StartScenario()
		{
			PrepareSteps();
			if (journal != null) journal.Clear();
			_startTime = Time.time;
			_isRunning = true;

			WriteLog(new ScenarioLogEntry(
				0f, "", "Сценарий", ScenarioEventType.ScenarioStarted, StepStatus.Pending,
				message: $"Запуск сценария: режим '{currentMode}', вне очереди: {outOfOrderPolicy}",
				extraData: new Dictionary<string, string>
				{
					{ ScenarioLogEntry.KeyMode, currentMode.ToString() },
					{ ScenarioLogEntry.KeyTotalSteps, CountSteps().ToString() }
				}));

			ActivateNextLine();
			OnStateChanged?.Invoke();
		}

		/// <summary>Останавливает прохождение без формирования отчета.</summary>
		public void StopScenario()
		{
			_isRunning = false;
			foreach (var step in _activeSteps) step.DeactivateStep();
			_activeSteps.Clear();
			OnStateChanged?.Invoke();
		}

		/// <summary>
		/// Повторная оценка завершенного прохождения другим анализатором (ТЗ п. 5), без перезапуска.
		/// </summary>
		public ScenarioReport ReEvaluate(ScenarioAnalyzer otherAnalyzer)
		{
			if (otherAnalyzer == null) return null;
			return otherAnalyzer.Analyze(JournalEntries, CountSteps());
		}

		// ---------- Сообщения от тестов ----------

		/// <summary>
		/// Тест сообщает о действии игрока. isSuccess — выполнено ли условие теста.
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

			OnStateChanged?.Invoke();
		}

		/// <summary>
		/// Тест сообщает о нарушении ТБ, не связанном с его выполнением (например, вход в
		/// работающую опасную зону). Засчитывается в любой момент прохождения.
		/// </summary>
		public void ReportSafetyViolation(StepInstance step, string message)
		{
			if (!_isRunning || step == null) return;

			string text = $"[Нарушение ТБ] {step.StepTitle}: {message}";
			WriteLog(new ScenarioLogEntry(
				Elapsed, step.StepId, step.StepTitle,
				ScenarioEventType.HazardViolation, step.CurrentStatus,
				step.AttemptCount, LogGroupIndex, message));

			OnNotificationMessage?.Invoke(text);
			RegisterGroupViolation();
			OnStateChanged?.Invoke();
		}

		private void HandleOutOfOrderAction(StepInstance step, string message)
		{
			if (step.CurrentStatus != StepStatus.Pending) return;
			if (step.ResolveOutOfOrderPolicy(outOfOrderPolicy) == OutOfOrderPolicy.IgnoreUntilActive) return;

			string text = $"[Нарушение порядка] Тест '{step.StepTitle}' еще не доступен. {message}";
			WriteLog(new ScenarioLogEntry(
				Elapsed, step.StepId, step.StepTitle,
				ScenarioEventType.OutOfOrderViolation, StepStatus.Pending,
				step.AttemptCount, LogGroupIndex, text));

			OnNotificationMessage?.Invoke(text);
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
				attempt, LogGroupIndex, message));

			OnNotificationMessage?.Invoke($"[Успех] {step.StepTitle}: {message}");
			CloseStepAndContinue();
		}

		private void HandleStepError(StepInstance step, string message)
		{
			int attempt = step.RegisterAttempt();

			if (!IsExam)
			{
				// Обучение: тест остается активным, игрок повторяет; остальные активные тесты не трогаем
				WriteLog(new ScenarioLogEntry(
					Elapsed, step.StepId, step.StepTitle,
					ScenarioEventType.StepFailed, StepStatus.Active,
					attempt, LogGroupIndex, $"{message} | Подсказка: {step.HintOnFailure}"));

				OnNotificationMessage?.Invoke($"[Ошибка, попытка {attempt}] {message}\nПодсказка: {step.HintOnFailure}");
				return;
			}

			// Экзамен: тест получает Failed без повтора, ошибка идет в порог группы
			step.MarkFailed();
			_activeSteps.Remove(step);

			WriteLog(new ScenarioLogEntry(
				Elapsed, step.StepId, step.StepTitle,
				ScenarioEventType.StepFailed, StepStatus.Failed,
				attempt, LogGroupIndex, message));

			OnNotificationMessage?.Invoke($"[Экзамен: тест не сдан] {step.StepTitle}: {message}");

			if (!RegisterGroupViolation())
			{
				CloseStepAndContinue();
			}
		}

		// ---------- Ход прохождения ----------

		/// <summary>
		/// Учитывает нарушение в текущей группе. Экзамен: при достижении порога закрывает группу
		/// (оставшиеся тесты — Skipped) и возвращает true. Обучение: порога нет, всегда false.
		/// </summary>
		private bool RegisterGroupViolation()
		{
			if (!IsExam || !_isRunning || _groupIndex >= groups.Count) return false;

			_groupViolations++;
			var group = groups[_groupIndex];
			if (group == null || _groupViolations < group.ViolationThreshold) return false;

			string text = $"Порог нарушений в группе '{group.GroupName}' достигнут " +
				$"({_groupViolations}/{group.ViolationThreshold}). Оставшиеся тесты группы пропускаются.";

			WriteLog(new ScenarioLogEntry(
				Elapsed, "", group.GroupName,
				ScenarioEventType.GroupSkipped, StepStatus.Skipped,
				0, _groupIndex, text));

			OnNotificationMessage?.Invoke(text);

			for (int i = _lineIndex; i < group.Stages.Count; i++)
			{
				var line = group.Stages[i];
				if (line == null) continue;

				foreach (var step in line.Steps)
				{
					if (step == null) continue;
					if (step.CurrentStatus != StepStatus.Pending && step.CurrentStatus != StepStatus.Active) continue;

					step.MarkSkipped();
					WriteLog(new ScenarioLogEntry(
						Elapsed, step.StepId, step.StepTitle,
						ScenarioEventType.StepSkipped, StepStatus.Skipped,
						step.AttemptCount, _groupIndex,
						"Пропущен: достигнут порог нарушений группы"));
				}
			}

			_activeSteps.Clear();
			_groupIndex++;
			_lineIndex = 0;
			_groupViolations = 0;
			ActivateNextLine();
			return true;
		}

		/// <summary>Тест закрыт: если в линии не осталось активных тестов — переходим к следующей.</summary>
		private void CloseStepAndContinue()
		{
			if (_activeSteps.Count > 0) return;
			_lineIndex++;
			ActivateNextLine();
		}

		/// <summary>
		/// Активирует ближайшую линию, в которой есть невыполненные тесты.
		/// Пустые линии пропускаются, после последней группы прохождение завершается.
		/// </summary>
		private void ActivateNextLine()
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
				if (group == null || _lineIndex >= group.Stages.Count)
				{
					if (IsExam && group != null)
					{
						WriteLog(new ScenarioLogEntry(
							Elapsed, "", group.GroupName,
							ScenarioEventType.GroupCompleted, StepStatus.Completed,
							0, _groupIndex, $"Группа '{group.GroupName}' завершена"));
					}

					_groupIndex++;
					_lineIndex = 0;
					_groupViolations = 0;
					continue;
				}

				var line = group.Stages[_lineIndex];
				if (line != null)
				{
					foreach (var step in line.Steps)
					{
						if (step == null || step.CurrentStatus != StepStatus.Pending) continue;
						ActivateStep(step);
					}
				}

				if (_activeSteps.Count > 0)
				{
					OnStateChanged?.Invoke();
					return;
				}

				_lineIndex++;
			}
		}

		private void ActivateStep(StepInstance step)
		{
			// Подсветка активных тестов — только в обучении
			step.ActivateStep(showHighlight: !IsExam);
			_activeSteps.Add(step);

			WriteLog(new ScenarioLogEntry(
				Elapsed, step.StepId, step.StepTitle,
				ScenarioEventType.StepActivated, StepStatus.Active,
				step.AttemptCount, LogGroupIndex,
				$"Активирован тест: {step.Description}"));
		}

		private void FinishScenario()
		{
			_isRunning = false;
			_activeSteps.Clear();

			WriteLog(new ScenarioLogEntry(
				Elapsed, "", "Сценарий",
				ScenarioEventType.ScenarioFinished, StepStatus.Completed,
				message: "Сценарий завершен"));

			ScenarioReport report = null;
			if (analyzer != null)
			{
				report = analyzer.Analyze(JournalEntries, CountSteps());
			}
			else
			{
				Debug.LogWarning("[ScenarioManager] Анализатор не задан — итоговая оценка не рассчитана.", this);
			}

			OnScenarioCompleted?.Invoke(report);
			OnStateChanged?.Invoke();
		}

		// ---------- Подготовка ----------

		/// <summary>
		/// Сбрасывает состояние прохождения и все тесты из списка, привязывает их к менеджеру.
		/// </summary>
		private void PrepareSteps()
		{
			_isRunning = false;
			_groupIndex = 0;
			_lineIndex = 0;
			_groupViolations = 0;
			_activeSteps.Clear();

			foreach (var step in EnumerateSteps())
			{
				step.Bind(this);
				bool listenWhileInactive =
					step.ResolveOutOfOrderPolicy(outOfOrderPolicy) == OutOfOrderPolicy.RejectAsViolation;
				step.ResetStep(listenWhileInactive);
			}
		}

		/// <summary>Уникальные тесты сценария в порядке их первого появления в списке.</summary>
		private IEnumerable<StepInstance> EnumerateSteps()
		{
			var seen = new HashSet<StepInstance>();
			foreach (var group in groups)
			{
				if (group == null) continue;
				foreach (var line in group.Stages)
				{
					if (line == null) continue;
					foreach (var step in line.Steps)
					{
						if (step != null && seen.Add(step)) yield return step;
					}
				}
			}
		}

		/// <summary>Количество уникальных тестов в сценарии.</summary>
		public int CountSteps()
		{
			int count = 0;
			foreach (var _ in EnumerateSteps()) count++;
			return count;
		}

		private void WriteLog(ScenarioLogEntry entry)
		{
			if (journal != null) journal.Log(entry);
		}
	}
}
