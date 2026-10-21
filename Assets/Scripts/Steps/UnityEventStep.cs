using UnityEngine;

namespace ScenarioSystem
{
	/// <summary>
	/// Тест «UnityEvent»: любое событие сцены, связанное в инспекторе с TriggerAction(), — шаг выполнен.
	/// </summary>
	public class UnityEventStep : StepBase
	{
		[Header("Сообщения журнала")]
		[Tooltip("Событие сработало — шаг выполнен")]
		[SerializeField] private string actionMessage = "Сработало событие";

		// Вызывается из UnityEvent (например, рубильника)
		public void TriggerAction()
		{
			ReportAction(true, actionMessage);
		}
	}
}
