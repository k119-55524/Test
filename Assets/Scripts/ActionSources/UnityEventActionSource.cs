using UnityEngine;

namespace ScenarioSystem.ActionSources
{
	/// <summary>
	/// Источник действия: вызов TriggerAction() из любого UnityEvent.
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
