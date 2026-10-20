using System;
using ScenarioSystem.ActionSources;
using ScenarioSystem.Core;
using UnityEngine;

namespace ScenarioSystem.Manager
{
	/// <summary>
	/// Шаг сценария: слушает свой источник действия и сообщает менеджеру о действиях игрока.
	/// Пока шаг активен, он подсвечен контуром.
	/// </summary>
	public class StepInstance : MonoBehaviour
	{
		[Header("Описание шага")]
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

		[Header("Связи")]
		[Tooltip("Источник действия шага (кнопка, триггер, коллизия, шлюз). Если пусто, ищется на этом же объекте.")]
		[SerializeField] private ActionSourceBase actionSource;

		[Header("Контур активного шага")]
		[Tooltip("Материал контура (общий M_Outline): цвет и толщина настраиваются в нем — сразу для всех шагов")]
		[SerializeField] private Material outlineMaterial;

		/// <summary>Идентификатор шага: назначается менеджером автоматически по порядку в списке (step_01, step_02…).</summary>
		public string StepId { get; private set; } = string.Empty;
		public string StepTitle => stepTitle;
		public string Description => description;
		public string HintOnFailure => hintOnFailure;
		public bool IsActive { get; private set; }
		public StepStatus CurrentStatus { get; private set; } = StepStatus.Pending;
		public int AttemptCount { get; private set; }

		/// <summary>
		/// Статус шага изменился. Для внутреннего оформления префаба
		/// (например, StepStatusColor красит площадку зоны).
		/// </summary>
		public event Action<StepStatus> OnStatusChanged;

		/// <summary>
		/// На шаге зафиксирована ошибка или нарушение (неверное действие, вне очереди, нарушение ТБ).
		/// Для оформления префаба (например, StepStatusColor — красная вспышка).
		/// </summary>
		public event Action OnError;

		/// <summary>
		/// Длящееся действие игрока на шаге закончилось (вышел из зоны, перестал касаться).
		/// Для оформления префаба: подсветку ошибки можно держать до этого события.
		/// </summary>
		public event Action OnInteractionEnded;

		/// <summary>Тип источника действия шага: Trigger, Collision, Button, UnityEvent, Sequence, HazardZone.</summary>
		public string SourceType => actionSource != null ? actionSource.GetType().Name.Replace("ActionSource", "") : string.Empty;

		/// <summary>Действие шага длится во времени (зона, касание), а не мгновенное (клик, событие).</summary>
		public bool HoldsInteraction => actionSource != null && actionSource.HoldsInteraction;

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
		private StepOutline _outline;

		/// <summary>
		/// Привязывает шаг к менеджеру сценария и назначает ему идентификатор.
		/// Вызывается менеджером при подготовке прохождения.
		/// </summary>
		public void Bind(ScenarioManager manager, string stepId)
		{
			_manager = manager;
			StepId = stepId;
		}

		private void Awake()
		{
			if (actionSource == null)
			{
				actionSource = GetComponent<ActionSourceBase>();
			}

			_outline = new StepOutline(gameObject, outlineMaterial);
		}

		private void OnEnable()
		{
			if (actionSource != null)
			{
				actionSource.OnActionPerformed += HandleActionFromSource;
				actionSource.OnSafetyViolation += HandleSafetyViolation;
				actionSource.OnInteractionEnded += HandleInteractionEnded;
			}
		}

		private void OnDisable()
		{
			if (actionSource != null)
			{
				actionSource.OnActionPerformed -= HandleActionFromSource;
				actionSource.OnSafetyViolation -= HandleSafetyViolation;
				actionSource.OnInteractionEnded -= HandleInteractionEnded;
			}
		}

		/// <summary>
		/// Активирует шаг в сценарии.
		/// </summary>
		public void ActivateStep()
		{
			IsActive = true;
			SetStatus(StepStatus.Active);
			if (actionSource != null)
			{
				actionSource.EnableSource();
				actionSource.SetStepActive(true);
			}
			_outline?.SetVisible(true);
		}

		/// <summary>
		/// Деактивирует шаг при остановке прохождения.
		/// </summary>
		public void DeactivateStep()
		{
			IsActive = false;
			StopSource();
			_outline?.SetVisible(false);
		}

		/// <summary>
		/// Помечает шаг как успешно выполненный.
		/// </summary>
		public void MarkCompleted()
		{
			IsActive = false;
			SetStatus(StepStatus.Completed);
			StopSource();
			_outline?.SetVisible(false);
		}

		/// <summary>
		/// Помечает шаг как заваленный (Failed в Экзамене).
		/// </summary>
		public void MarkFailed()
		{
			IsActive = false;
			SetStatus(StepStatus.Failed);
			StopSource();
			_outline?.SetVisible(false);
		}

		/// <summary>
		/// Помечает шаг как пропущенный из-за порога нарушений этапа (Skipped в Экзамене).
		/// </summary>
		public void MarkSkipped()
		{
			IsActive = false;
			SetStatus(StepStatus.Skipped);
			StopSource();
			_outline?.SetVisible(false);
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
			SetStatus(StepStatus.Pending);
			AttemptCount = 0;
			if (actionSource != null)
			{
				actionSource.ResetState();
				actionSource.SetStepActive(false);
				if (listenWhileInactive) actionSource.EnableSource();
				else actionSource.DisableSource();
			}
			_outline?.SetVisible(false);
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

		/// <summary>
		/// Сообщает шагу, что на нем зафиксирована ошибка. Вызывается менеджером.
		/// </summary>
		public void NotifyError()
		{
			OnError?.Invoke();
		}

		private void SetStatus(StepStatus status)
		{
			CurrentStatus = status;
			OnStatusChanged?.Invoke(status);
		}

		private void HandleActionFromSource(bool isSuccess, string message)
		{
			if (_manager != null) _manager.ReportStepAction(this, isSuccess, message);
		}

		private void HandleSafetyViolation(string message)
		{
			if (_manager != null) _manager.ReportSafetyViolation(this, message);
		}

		private void HandleInteractionEnded()
		{
			OnInteractionEnded?.Invoke();
		}
	}
}
