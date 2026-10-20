using System;
using UnityEngine;

namespace ScenarioSystem.ActionSources
{
	/// <summary>
	/// Передает вход и выход из триггера зоны источнику действия (добавляется автоматически).
	/// </summary>
	[DisallowMultipleComponent]
	public class ZoneTriggerRelay : MonoBehaviour
	{
		public event Action<Collider> Entered;
		public event Action<Collider> Exited;

		private void OnTriggerEnter(Collider other)
		{
			Entered?.Invoke(other);
		}

		private void OnTriggerExit(Collider other)
		{
			Exited?.Invoke(other);
		}
	}
}
