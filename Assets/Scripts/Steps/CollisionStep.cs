using UnityEngine;

namespace ScenarioSystem
{
	/// <summary>
	/// Тест «Collision»: игрок упирается в объект (например, ящик) — шаг выполнен.
	/// </summary>
	[RequireComponent(typeof(Collider))]
	public class CollisionStep : StepBase
	{
		// Ошибка (например, вне очереди) — красный, пока игрок касается объекта
		protected override bool HoldsInteraction => true;

		// Игрок коснулся объекта
		private void OnCollisionEnter(Collision collision)
		{
			if (IsPlayer(collision.collider))
				ReportAction(true, "Столкновение с объектом");
		}

		// Игрок отошел от объекта
		private void OnCollisionExit(Collision collision)
		{
			if (IsPlayer(collision.collider))
				EndInteraction();
		}
	}
}
