using UnityEngine;

namespace ScenarioSystem.ActionSources
{
	/// <summary>
	/// Источник действия от кастомного события Unity (UnityEvent).
	///
	/// Логика:
	/// Точка входа — публичный метод TriggerAction(). Его выбирают в любом UnityEvent инспектора
	/// (Button.onClick, событие двери, таймера, рычага, Animation Event), связывая шаг сценария
	/// со сторонним компонентом без написания кода на C#.
	/// </summary>
	public class UnityEventActionSource : ActionSourceBase
	{
		[Header("Настройки события")]
		[Tooltip("Считать ли вызов успехом")]
		[SerializeField] private bool treatAsSuccess = true;

		[Tooltip("Текстовое пояснение")]
		[SerializeField] private string actionMessage = "Сработало пользовательское событие UnityEvent";

		/// <summary>
		/// Публичный метод, который можно вызывать из других скриптов или инспектора Unity.
		/// </summary>
		public void TriggerAction()
		{
			if (!isSourceEnabled) return;
			NotifyAction(treatAsSuccess, actionMessage);
		}
	}
}
