using UnityEngine;

namespace ScenarioSystem.ActionSources
{
	/// <summary>
	/// Источник действия «опасная зона»: при выключенной опасности вход выполняет шаг,
	/// при включенной — ошибка шага (если он активен) или нарушение ТБ.
	/// </summary>
	[RequireComponent(typeof(Collider))]
	public class HazardZoneActionSource : ActionSourceBase
	{
		[Header("Настройки опасности")]
		[Tooltip("Включена ли опасность на старте сценария")]
		[SerializeField] private bool isHazardActive = false;

		[Tooltip("Тег игрока для фильтрации")]
		[SerializeField] private string requiredTag = "Player";

		[Tooltip("Визуальный индикатор опасности (например, красная лампа или предупреждающий знак)")]
		[SerializeField] private GameObject dangerIndicator;

		[Header("Сообщения")]
		[SerializeField] private string safeEntryMessage = "Безопасный осмотр: оборудование обесточено, допуск разрешен.";
		[SerializeField] private string hazardEntryMessage = "Нарушение ТБ: вход в опасную зону работающего оборудования!";

		private bool _initialHazardActive;

		private void Awake()
		{
			var col = GetComponent<Collider>();
			if (col != null) col.isTrigger = true;
			_initialHazardActive = isHazardActive;
			UpdateVisuals();
		}

		/// <summary>
		/// Включает или выключает опасность (например, кнопкой «Пуск оборудования»).
		/// </summary>
		public void SetHazardActive(bool active)
		{
			isHazardActive = active;
			UpdateVisuals();
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;

			bool stepAcceptsAction = IsStepActive && isSourceEnabled;

			if (isHazardActive)
			{
				if (stepAcceptsAction) NotifyAction(false, hazardEntryMessage);
				else NotifySafetyViolation(hazardEntryMessage);
			}
			else if (stepAcceptsAction)
			{
				NotifyAction(true, safeEntryMessage);
			}
		}

		public override bool HoldsInteraction => true;

		private void OnTriggerExit(Collider other)
		{
			if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;
			NotifyInteractionEnded();
		}

		private void UpdateVisuals()
		{
			if (dangerIndicator != null)
			{
				dangerIndicator.SetActive(isHazardActive);
			}
		}

		public override void ResetState()
		{
			base.ResetState();
			isHazardActive = _initialHazardActive;
			UpdateVisuals();
		}
	}
}
