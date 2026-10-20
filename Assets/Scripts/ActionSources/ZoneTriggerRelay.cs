using System;
using UnityEngine;

namespace ScenarioSystem.ActionSources
{
	/// <summary>
	/// Служебный ретранслятор OnTriggerEnter с отдельного коллайдера-зоны.
	/// Добавляется автоматически источниками, которым нужно слушать несколько зон
	/// (например SequenceActionSource) — вручную вешать не нужно.
	/// </summary>
	[DisallowMultipleComponent]
	public class ZoneTriggerRelay : MonoBehaviour
	{
		public event Action<Collider> Entered;

		private void OnTriggerEnter(Collider other)
		{
			Entered?.Invoke(other);
		}
	}
}
