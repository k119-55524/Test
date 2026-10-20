using ScenarioSystem.ActionSources;
using ScenarioSystem.Core;
using UnityEngine;

namespace ScenarioSystem.Manager
{
	/// <summary>
	/// Автономный контроллер отдельного шага/теста на сцене или префабе.
	///
	/// Логика архитектуры:
	/// 1. Локальная автономия: префаб сам знает свои интерактивные компоненты (ActionSource),
	///    тексты подсказок и текущий визуальный статус (активен/неактивен/подсвечен).
	/// 2. Связь с менеджером: при старте ScenarioManager передает шагу ссылку на себя (Bind).
	///    Шаг слушает свой ActionSource и сам сообщает менеджеру о действии игрока
	///    (ReportStepAction) или о нарушении ТБ (ReportSafetyViolation). Активен ли шаг,
	///    менеджер видит по IsActive.
	/// 3. Действия вне очереди: шаг хранит собственное правило (или берет общее из ScenarioManager).
	///    При RejectAsViolation источник слушает и до очереди — менеджер фиксирует нарушение;
	///    при IgnoreUntilActive источник выключен до активации шага.
	/// 4. Попытки считает менеджер (RegisterAttempt) и только для действий на активном шаге.
	/// </summary>
	public class StepInstance : MonoBehaviour
	{
		[Header("Идентификация шага")]
		[Tooltip("Уникальный идентификатор шага (например 'step_button_start')")]
		[SerializeField] private string stepId = "step_id";

		[Tooltip("Человекочитаемое название задания")]
		[SerializeField] private string stepTitle = "Задание шага";

		[Tooltip("Подробная инструкция для игрока")]
		[TextArea(2, 4)]
		[SerializeField] private string description = "Выполните требуемое действие.";

		[Tooltip("Подсказка, отображаемая при ошибке (в режиме обучения)")]
		[TextArea(2, 4)]
		[SerializeField] private string hintOnFailure = "Обратите внимание на последовательность и технику безопасности.";

		[Header("Действия вне очереди")]
		[Tooltip("UseScenarioDefault — как задано в ScenarioManager. RejectAsViolation — действие до очереди считается нарушением. IgnoreUntilActive — шаг не принимает действия до своей очереди.")]
		[SerializeField] private OutOfOrderOverride outOfOrderOverride = OutOfOrderOverride.UseScenarioDefault;

		[Header("Связи и визуал")]
		[Tooltip("Источник действия шага (кнопка, триггер, коллизия, шлюз). Если пусто, ищется на этом же объекте.")]
		[SerializeField] private ActionSourceBase actionSource;

		[Tooltip("Объект подсветки (маркер, галочка или световой контур для режима Обучения)")]
		[SerializeField] private GameObject highlightMarker;

		[Tooltip("Рендерер для подсветки цветом (опционально)")]
		[SerializeField] private Renderer visualRenderer;

		[Header("Цвета подсветки")]
		[SerializeField] private Color normalColor = Color.gray;
		[SerializeField] private Color activeColor = Color.green;
		[SerializeField] private Color completedColor = Color.cyan;
		[SerializeField] private Color failedColor = Color.red;

		public string StepId => stepId;
		public string StepTitle => stepTitle;
		public string Description => description;
		public string HintOnFailure => hintOnFailure;
		public bool IsActive { get; private set; }
		public StepStatus CurrentStatus { get; private set; } = StepStatus.Pending;
		public int AttemptCount { get; private set; } = 0;

		/// <summary>
		/// Итоговое правило для действий вне очереди с учетом индивидуальной настройки шага.
		/// </summary>
		public OutOfOrderPolicy ResolveOutOfOrderPolicy(OutOfOrderPolicy scenarioDefault) => outOfOrderOverride switch
		{
			OutOfOrderOverride.RejectAsViolation => OutOfOrderPolicy.RejectAsViolation,
			OutOfOrderOverride.IgnoreUntilActive => OutOfOrderPolicy.IgnoreUntilActive,
			_ => scenarioDefault
		};

		private ScenarioManager _manager;

		/// <summary>
		/// Привязывает шаг к менеджеру сценария. Вызывается менеджером при подготовке прохождения.
		/// </summary>
		public void Bind(ScenarioManager manager)
		{
			_manager = manager;
		}

		private void Awake()
		{
			if (actionSource == null)
			{
				actionSource = GetComponent<ActionSourceBase>();
			}

			if (visualRenderer == null)
			{
				visualRenderer = GetComponent<Renderer>();
			}

			SetVisualState(StepStatus.Pending, showHighlight: false);
		}

		private void OnEnable()
		{
			if (actionSource != null)
			{
				actionSource.OnActionPerformed += HandleActionFromSource;
				actionSource.OnSafetyViolation += HandleSafetyViolation;
			}
		}

		private void OnDisable()
		{
			if (actionSource != null)
			{
				actionSource.OnActionPerformed -= HandleActionFromSource;
				actionSource.OnSafetyViolation -= HandleSafetyViolation;
			}
		}

		/// <summary>
		/// Активирует шаг в сценарии.
		/// </summary>
		/// <param name="showHighlight">Включать ли подсветку (в Обучении — true, в Экзамене — false).</param>
		public void ActivateStep(bool showHighlight)
		{
			IsActive = true;
			CurrentStatus = StepStatus.Active;
			if (actionSource != null)
			{
				actionSource.EnableSource();
				actionSource.SetStepActive(true);
			}
			SetVisualState(StepStatus.Active, showHighlight);
		}

		/// <summary>
		/// Деактивирует шаг при переходе к следующему этапу.
		/// </summary>
		public void DeactivateStep()
		{
			IsActive = false;
			StopSource();
			SetVisualState(CurrentStatus, showHighlight: false);
		}

		/// <summary>
		/// Помечает шаг как успешно выполненный.
		/// </summary>
		public void MarkCompleted()
		{
			IsActive = false;
			CurrentStatus = StepStatus.Completed;
			StopSource();
			SetVisualState(StepStatus.Completed, showHighlight: false);
		}

		/// <summary>
		/// Помечает шаг как заваленный (Failed в Экзамене).
		/// </summary>
		public void MarkFailed()
		{
			IsActive = false;
			CurrentStatus = StepStatus.Failed;
			StopSource();
			SetVisualState(StepStatus.Failed, showHighlight: false);
		}

		/// <summary>
		/// Помечает шаг как пропущенный из-за превышения порога ошибок в группе (Skipped в Экзамене).
		/// </summary>
		public void MarkSkipped()
		{
			IsActive = false;
			CurrentStatus = StepStatus.Skipped;
			StopSource();
			SetVisualState(StepStatus.Skipped, showHighlight: false);
		}

		/// <summary>
		/// Сбрасывает состояние шага для повторного прохождения сценария.
		/// </summary>
		/// <param name="listenWhileInactive">
		/// true — источник остается включенным до очереди шага, чтобы менеджер мог зафиксировать
		/// действие вне очереди как нарушение; false — шаг полностью пассивен до активации.
		/// </param>
		public void ResetStep(bool listenWhileInactive)
		{
			IsActive = false;
			CurrentStatus = StepStatus.Pending;
			AttemptCount = 0;
			if (actionSource != null)
			{
				actionSource.ResetState();
				actionSource.SetStepActive(false);
				if (listenWhileInactive) actionSource.EnableSource();
				else actionSource.DisableSource();
			}
			SetVisualState(StepStatus.Pending, showHighlight: false);
		}

		/// <summary>
		/// Засчитывает попытку выполнения. Вызывается менеджером только для действий
		/// на активном шаге: действия вне очереди попыткой не считаются.
		/// </summary>
		public int RegisterAttempt()
		{
			return ++AttemptCount;
		}

		private void StopSource()
		{
			if (actionSource == null) return;
			actionSource.SetStepActive(false);
			actionSource.DisableSource();
		}

		private void HandleActionFromSource(bool isSuccess, string message)
		{
			if (_manager != null) _manager.ReportStepAction(this, isSuccess, message);
		}

		private void HandleSafetyViolation(string message)
		{
			if (_manager != null) _manager.ReportSafetyViolation(this, message);
		}

		private void SetVisualState(StepStatus status, bool showHighlight)
		{
			if (highlightMarker != null)
			{
				highlightMarker.SetActive(showHighlight && status == StepStatus.Active);
			}

			if (visualRenderer != null)
			{
				Color targetColor = status switch
				{
					StepStatus.Active => activeColor,
					StepStatus.Completed => completedColor,
					StepStatus.Failed => failedColor,
					StepStatus.Skipped => Color.black,
					_ => normalColor
				};

				visualRenderer.material.color = targetColor;
			}
		}
	}
}
