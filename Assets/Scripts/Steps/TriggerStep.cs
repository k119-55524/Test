using UnityEngine;

namespace ScenarioSystem
{
	/// <summary>
	/// Тест «Trigger»: игрок входит в зону — шаг выполнен.
	/// </summary>
	[RequireComponent(typeof(Collider))]
	public class TriggerStep : StepBase
	{
		// Ошибка (например, вне очереди) — красный, пока игрок в зоне
		protected override bool HoldsInteraction => true;

		// Игрок вошел в зону
		private void OnTriggerEnter(Collider other)
		{
			if (IsPlayer(other))
				ReportAction(true, "Вход в зону");
		}

		// Игрок вышел из зоны
		private void OnTriggerExit(Collider other)
		{
			if (IsPlayer(other))
				EndInteraction();
		}
	}
}
