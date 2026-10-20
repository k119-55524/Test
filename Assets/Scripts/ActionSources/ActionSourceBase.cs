using System;
using UnityEngine;

namespace ScenarioSystem.ActionSources
{
	/// <summary>
	/// Базовый класс источника действия: превращает событие Unity в успех или ошибку шага либо в нарушение ТБ.
	/// </summary>
	public abstract class ActionSourceBase : MonoBehaviour
	{
		/// <summary>Источник принимает действия игрока. Управляется шагом (EnableSource / DisableSource).</summary>
		protected bool isSourceEnabled = true;

		/// <summary>
		/// Действие игрока: (isSuccess — успех или ошибка/нарушение, message — пояснение).
		/// </summary>
		public event Action<bool, string> OnActionPerformed;

		/// <summary>
		/// Нарушение техники безопасности, не связанное с выполнением шага
		/// (например, вход в опасную зону, когда шаг не активен). message — пояснение.
		/// </summary>
		public event Action<string> OnSafetyViolation;

		/// <summary>
		/// Длящееся действие игрока закончилось (вышел из зоны, перестал касаться объекта).
		/// Нужно оформлению, чтобы держать подсветку ошибки, пока действие длится.
		/// </summary>
		public event Action OnInteractionEnded;

		/// <summary>
		/// Действие этого источника длится во времени (зона, касание) и завершается OnInteractionEnded.
		/// false — мгновенное действие (клик, событие).
		/// </summary>
		public virtual bool HoldsInteraction => false;

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
		}

		/// <summary>Шаг вошел в свою очередь. Переопределяется при необходимости.</summary>
		protected virtual void OnStepActivated() { }

		/// <summary>
		/// Безопасный вызов события для подписчиков (StepInstance).
		/// </summary>
		protected void NotifyAction(bool isSuccess, string message)
		{
			OnActionPerformed?.Invoke(isSuccess, message);
		}

		/// <summary>
		/// Сообщает, что длящееся действие игрока закончилось.
		/// </summary>
		protected void NotifyInteractionEnded()
		{
			OnInteractionEnded?.Invoke();
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
