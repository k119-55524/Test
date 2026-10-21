using System;
using UnityEngine;

namespace ScenarioSystem
{
	/// <summary>
	/// Передает шагу вход и выход игрока из зоны-триггера (вешается на объект зоны в префабе).
	/// </summary>
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Collider))]
	public class ZoneTriggerRelay : MonoBehaviour
	{
		public event Action<Collider> Entered;
		public event Action<Collider> Exited;

		// Вход в зону
		private void OnTriggerEnter(Collider other) => Entered?.Invoke(other);

		// Выход из зоны
		private void OnTriggerExit(Collider other) => Exited?.Invoke(other);
	}
}
