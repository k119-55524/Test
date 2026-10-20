using System;
using UnityEngine;

namespace ScenarioSystem.ActionSources
{
	/// <summary>
	/// Базовый класс (и контракт) источников действий (ТЗ п. 3.1).
	///
	/// Логика архитектуры:
	/// Реализует общие механизмы включения/выключения, подписки и безопасного вызова событий.
	/// Наследуясь от этого класса, разработчик нового источника пишет только специфическую
	/// логику Unity (OnTriggerEnter, OnCollisionEnter, UI callback).
	///
	/// Два независимых флага:
	/// - IsEnabled — источник вообще принимает действия (у неактивного шага может слушать,
	///   чтобы менеджер зафиксировал действие вне очереди);
	/// - IsStepActive — шаг сейчас в очереди. Нужен источникам с внутренним состоянием
	///   (например, шлюз A→B засчитывает зону A только у активного шага).
	/// </summary>
	public abstract class ActionSourceBase : MonoBehaviour
	{
		[Header("Базовые настройки источника")]
		[Tooltip("Разрешено ли срабатывание источника")]
		[SerializeField] protected bool isSourceEnabled = true;

		/// <summary>
		/// Действие игрока: (isSuccess — успех или ошибка/нарушение, message — пояснение).
		/// </summary>
		public event Action<bool, string> OnActionPerformed;

		/// <summary>
		/// Нарушение техники безопасности, не связанное с выполнением шага
		/// (например, вход в опасную зону, когда шаг не активен). message — пояснение.
		/// </summary>
		public event Action<string> OnSafetyViolation;

		public bool IsEnabled => isSourceEnabled;

		/// <summary>Шаг, которому принадлежит источник, сейчас активен (в своей очереди).</summary>
		public bool IsStepActive { get; private set; }

		/// <summary>Источник принимает действия игрока.</summary>
		public virtual void EnableSource()
		{
			isSourceEnabled = true;
		}

		/// <summary>Источник перестает передавать действия шагу.</summary>
		public virtual void DisableSource()
		{
			isSourceEnabled = false;
		}

		/// <summary>Сброс внутреннего состояния при перезапуске сценария.</summary>
		public virtual void ResetState()
		{
		}

		/// <summary>Сообщает источнику, что его шаг вошел в очередь или вышел из нее.</summary>
		public void SetStepActive(bool active)
		{
			if (IsStepActive == active) return;
			IsStepActive = active;
			if (active) OnStepActivated();
			else OnStepDeactivated();
		}

		/// <summary>Шаг вошел в свою очередь. Переопределяется при необходимости.</summary>
		protected virtual void OnStepActivated() { }

		/// <summary>Шаг вышел из очереди (завершен, завален, пропущен, остановлен).</summary>
		protected virtual void OnStepDeactivated() { }

		/// <summary>
		/// Безопасный вызов события для подписчиков (StepInstance).
		/// </summary>
		protected void NotifyAction(bool isSuccess, string message)
		{
			OnActionPerformed?.Invoke(isSuccess, message);
		}

		/// <summary>
		/// Сообщает о нарушении ТБ (StepInstance передает его менеджеру).
		/// </summary>
		protected void NotifySafetyViolation(string message)
		{
			OnSafetyViolation?.Invoke(message);
		}
	}
}
