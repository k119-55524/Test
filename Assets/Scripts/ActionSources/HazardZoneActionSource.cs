using UnityEngine;

namespace ScenarioSystem.ActionSources
{
	/// <summary>
	/// Контекстный источник действия №2: «Опасная зона оборудования» (ТЗ п. 3.1).
	/// Шаг «войти в зону только при нужном состоянии другого объекта».
	///
	/// Логика входа игрока в зону:
	/// | Опасность  | Шаг активен                  | Шаг не активен          |
	/// | включена   | ошибка шага                  | нарушение ТБ (всегда)   |
	/// | выключена  | шаг выполнен                 | ничего                  |
	///
	/// - Нарушение ТБ фиксируется в любой момент прохождения, даже если шаг уже закрыт
	///   или отключен правилом «вне очереди».
	/// - Состояние переключает SetHazardActive(bool): его можно назначить в любом UnityEvent
	///   инспектора (например, Button.onClick кнопки «Пуск оборудования»).
	/// - При сбросе сценария восстанавливается начальное состояние из инспектора.
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

		public bool IsHazardActive => isHazardActive;

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
