using UnityEngine;
using System.Collections.Generic;

namespace ScenarioSystem
{
	/// <summary>
	/// Запускает сценарий: выбирает режим (обучение или экзамен), дает ему логгер и настройки,
	/// в конце считает итог анализатором режима. Ход прохождения ведет сам режим.
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

		[Tooltip("Состав и порядок шагов в режиме Обучение")]
		[SerializeField] private TrainingMode trainingMode;

		[Tooltip("Группы и порядок шагов в режиме Экзамен")]
		[SerializeField] private ExamMode examMode;

		[Header("Логгер и анализатор")]
		[Tooltip("Логгер (компонент с IScenarioLogger): ведет журнал событий")]
		[SerializeField] private MonoBehaviour logger;

		[Tooltip("Анализатор (компонент с IScenarioAnalyzer) итоговой оценки в режиме Обучение. Пусто — оценка не рассчитывается")]
		[SerializeField] private MonoBehaviour trainingAnalyzer;

		[Tooltip("Анализатор (компонент с IScenarioAnalyzer) итоговой оценки в режиме Экзамен. Пусто — оценка не рассчитывается")]
		[SerializeField] private MonoBehaviour examAnalyzer;

		private ScenarioModeBase _mode;

		private IScenarioLogger Logger => logger as IScenarioLogger;

		// Проверка полей в редакторе: только компоненты с нужным интерфейсом
		private void OnValidate()
		{
			logger = RequireInterface<IScenarioLogger>(logger, "Logger");
			trainingAnalyzer = RequireInterface<IScenarioAnalyzer>(trainingAnalyzer, "Training Analyzer");
			examAnalyzer = RequireInterface<IScenarioAnalyzer>(examAnalyzer, "Exam Analyzer");
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

		// Запуск с начала: выбрать режим, раздать шагам id, передать режиму логгер и настройки
		private void StartScenario()
		{
			if (Logger == null)
			{
				Debug.LogError("[ScenarioManager] Не задан логгер — сценарий не запущен.", this);
				return;
			}

			_mode = currentMode == ScenarioMode.Exam ? examMode : (ScenarioModeBase)trainingMode;
			if (_mode == null)
			{
				Debug.LogError($"[ScenarioManager] Не задан компонент режима {currentMode} — сценарий не запущен.", this);
				return;
			}

			BindSteps();
			Logger.Clear();
			_mode.Begin(scenarioTitle, outOfOrderPolicy, Logger.Log, ShowResult);
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

		// Повторная оценка завершенного прохождения другим анализатором
		public ScenarioReport ReEvaluate(IScenarioAnalyzer otherAnalyzer)
		{
			if (otherAnalyzer == null || _mode == null)
				return null;

			return otherAnalyzer.Analyze(Logger.Entries, CountSteps());
		}

		// Конец прохождения: итог анализатором режима
		private void ShowResult()
		{
			var analyzer = (_mode.Mode == ScenarioMode.Exam ? examAnalyzer : trainingAnalyzer) as IScenarioAnalyzer;
			if (analyzer == null)
			{
				Debug.LogWarning($"[ScenarioManager] Анализатор для режима {_mode.Mode} не задан — итоговая оценка не рассчитана.", this);
				return;
			}

			var report = analyzer.Analyze(Logger.Entries, CountSteps());
			Debug.Log($"Итог {_mode.ModeName} «{scenarioTitle}»: {report}\n    {string.Join("\n    ", report.Details)}", this);
		}

		// Id шагам по порядку в обучении, затем в экзамене (одинаковые в обоих режимах); шаги режима — к нему
		private void BindSteps()
		{
			var ids = new Dictionary<StepBase, string>();
			foreach (var mode in new ScenarioModeBase[] { trainingMode, examMode })
			{
				if (mode == null)
					continue;

				foreach (var step in mode.Steps)
				{
					if (!ids.ContainsKey(step))
						ids[step] = $"step_{ids.Count + 1:00}";
				}
			}

			foreach (var step in _mode.Steps)
			{
				step.Bind(_mode, ids[step]);
				step.ResetStep();
			}
		}

		// Число шагов в текущем режиме
		private int CountSteps()
		{
			int count = 0;
			foreach (var _ in _mode.Steps)
				count++;

			return count;
		}
	}
}