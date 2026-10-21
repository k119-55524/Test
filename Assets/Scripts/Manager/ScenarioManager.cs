using UnityEngine;
using System.Collections.Generic;

namespace ScenarioSystem
{
	/// <summary>
	/// Сценарий: этапы настраиваются здесь один раз для обоих режимов. При старте создает
	/// режим (обучение или экзамен), дает ему состав, логгер и вывод; в конце считает итог анализатором режима.
	/// </summary>
	public class ScenarioManager : MonoBehaviour
	{
		[Header("Режим")]
		[Tooltip("Training — обучение (повтор после ошибки), Exam — экзамен (Failed/Skipped, порог нарушений группы)")]
		[SerializeField] private ScenarioMode currentMode = ScenarioMode.Training;

		[Tooltip("Действие на шаге вне его очереди: нарушение или игнорировать (шаг может переопределить у себя)")]
		[SerializeField] private OutOfOrderPolicy outOfOrderPolicy = OutOfOrderPolicy.RejectAsViolation;

		[Header("Сценарий")]
		[Tooltip("Общее название сценария: выводится при старте и в итоге («Старт обучения «…»»)")]
		[SerializeField] private string scenarioTitle = "Сценарий";

		[Tooltip("Порог нарушений группы экзамена: при достижении оставшиеся шаги группы — Skipped")]
		[Min(1)]
		[SerializeField] private int violationThreshold = 2;

		[Tooltip("Этапы активации по очереди: тесты этапа активны одновременно; номер группы — для экзамена")]
		[SerializeField] private List<ActivationStage> stages = new();

		[Header("Логгер и анализатор")]
		[Tooltip("Логгер (компонент с IScenarioLogger): ведет журнал событий")]
		[SerializeField] private MonoBehaviour logger;

		[Tooltip("Анализатор (компонент с IScenarioAnalyzer) итоговой оценки в режиме Обучение. Пусто — оценка не рассчитывается")]
		[SerializeField] private MonoBehaviour trainingAnalyzer;

		[Tooltip("Анализатор (компонент с IScenarioAnalyzer) итоговой оценки в режиме Экзамен. Пусто — оценка не рассчитывается")]
		[SerializeField] private MonoBehaviour examAnalyzer;

		[Header("Интерфейс")]
		[Tooltip("Вывод для игрока (компонент с IScenarioView): задания, сообщения, итог. Пусто — только журнал")]
		[SerializeField] private MonoBehaviour view;

		private ScenarioModeBase _mode;

		// Пустое поле Unity — тоже null
		private IScenarioLogger Logger => logger != null ? logger as IScenarioLogger : null;
		private IScenarioView View => view != null ? view as IScenarioView : null;

		// Журнал текущего или последнего прохождения
		public IReadOnlyList<ScenarioLogEntry> Entries => Logger != null ? Logger.Entries : new List<ScenarioLogEntry>();

		// Проверка полей в редакторе: номер группы новых этапов — 1, в поля — только компоненты с нужным интерфейсом
		private void OnValidate()
		{
			foreach (var stage in stages)
				stage.ApplyDefaults();

			logger = RequireInterface<IScenarioLogger>(logger, "Logger");
			trainingAnalyzer = RequireInterface<IScenarioAnalyzer>(trainingAnalyzer, "Training Analyzer");
			examAnalyzer = RequireInterface<IScenarioAnalyzer>(examAnalyzer, "Exam Analyzer");
			view = RequireInterface<IScenarioView>(view, "View");
		}

		// В поле допускается только компонент с интерфейсом T
		private MonoBehaviour RequireInterface<T>(MonoBehaviour component, string fieldName)
		{
			if (component == null || component is T)
				return component;

			Debug.LogWarning($"[ScenarioManager] {component.GetType().Name} не реализует {typeof(T).Name} — поле {fieldName} очищено.", this);
			return null;
		}

		private void Start()
		{
			// Прохождение стартует сразу при запуске сцены в выбранном режиме
			StartScenario();
		}

		// Запуск с начала: создать режим, раздать шагам id, передать режиму состав, логгер и вывод
		private void StartScenario()
		{
			if (Logger == null)
			{
				Debug.LogError("[ScenarioManager] Не задан логгер — сценарий не запущен.", this);
				return;
			}

			if (_mode != null)
				_mode.Stop();

			_mode = currentMode == ScenarioMode.Exam ? new ExamMode(violationThreshold) : (ScenarioModeBase)new TrainingMode();
			BindSteps();
			Logger.Clear();
			_mode.Begin(scenarioTitle, stages, outOfOrderPolicy, Logger.Log, View, ShowResult);
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
			if (Application.isPlaying && _mode != null)
				_mode.Stop();
		}

		[ContextMenu("Повторная оценка: анализатор обучения")]
		private void ReEvaluateTraining() => ReEvaluate(ScenarioMode.Training);

		[ContextMenu("Повторная оценка: анализатор экзамена")]
		private void ReEvaluateExam() => ReEvaluate(ScenarioMode.Exam);

		// Повторная оценка последнего прохождения анализатором режима rules, без перезапуска — в консоль и игроку
		private void ReEvaluate(ScenarioMode rules)
		{
			var analyzer = AnalyzerFor(rules);
			if (_mode == null || analyzer == null)
			{
				Debug.LogWarning($"[ScenarioManager] Нет прохождения или не задан анализатор режима {rules}.", this);
				return;
			}

			ShowReport($"Повторная оценка по правилам {(rules == ScenarioMode.Exam ? "экзамена" : "обучения")}", analyzer);
		}

		// Анализатор режима; пустое поле — null
		private IScenarioAnalyzer AnalyzerFor(ScenarioMode rules)
		{
			var component = rules == ScenarioMode.Exam ? examAnalyzer : trainingAnalyzer;
			return component != null ? component as IScenarioAnalyzer : null;
		}

		// Конец прохождения: итог анализатором режима — в консоль и игроку
		private void ShowResult()
		{
			var analyzer = AnalyzerFor(_mode.Mode);
			if (analyzer == null)
			{
				Debug.LogWarning($"[ScenarioManager] Анализатор для режима {_mode.Mode} не задан — итоговая оценка не рассчитана.", this);
				return;
			}

			ShowReport($"Итог {_mode.ModeName} «{scenarioTitle}»", analyzer);
		}

		// Оценка журнала анализатором — в консоль и игроку
		private void ShowReport(string title, IScenarioAnalyzer analyzer)
		{
			var report = analyzer.Analyze(Logger.Entries, CountSteps());
			Debug.Log($"{title}: {report}\n    {string.Join("\n    ", report.Details)}", this);
			View?.ShowResult(title, report);
		}

		// Шагам — режим и id по порядку: step_01, step_02…
		private void BindSteps()
		{
			int number = 0;
			foreach (var step in ScenarioModeBase.StepsOf(stages))
			{
				number++;
				step.Bind(_mode, $"step_{number:00}");
				step.ResetStep();
			}
		}

		// Число шагов в сценарии
		private int CountSteps()
		{
			int count = 0;
			foreach (var _ in ScenarioModeBase.StepsOf(stages))
				count++;

			return count;
		}
	}
}