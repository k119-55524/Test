using UnityEngine;
using UnityEngine.Events;

namespace ScenarioSystem
{
	/// <summary>
	/// Рубильник: при каждом входе игрока в зону переключается (вкл/выкл)
	/// и вызывает UnityEvent с новым состоянием.
	/// </summary>
	[RequireComponent(typeof(Collider))]
	public class ToggleSwitch : MonoBehaviour
	{
		[Tooltip("Включен ли рубильник на старте")]
		[SerializeField] private bool isOn = true;

		[Tooltip("Тег игрока")]
		[SerializeField] private string requiredTag = "Player";

		[Tooltip("Ручка, наклоняется по состоянию (необязательно)")]
		[SerializeField] private Transform handle;

		[Tooltip("Вызывается при переключении; параметр — новое состояние (true — включен)")]
		[SerializeField] private UnityEvent<bool> onSwitched = new();

		private void Awake()
		{
			UpdateHandle();
		}

		// Игрок вошел — переключение и вызов On Switched
		private void OnTriggerEnter(Collider other)
		{
			if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;

			isOn = !isOn;
			UpdateHandle();
			onSwitched.Invoke(isOn);
		}

		// Наклон ручки по состоянию
		private void UpdateHandle()
		{
			if (handle != null) handle.localRotation = Quaternion.Euler(isOn ? -35f : 35f, 0f, 0f);
		}
	}
}
