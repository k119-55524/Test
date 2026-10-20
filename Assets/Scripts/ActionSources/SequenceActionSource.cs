using System;
using UnityEngine;

namespace ScenarioSystem.ActionSources
{
	/// <summary>
	/// Контекстный источник действия №1: «Последовательность зон / Шлюз» (ТЗ п. 3.1).
	///
	/// Логика:
	/// - Пока шаг активен, вход в Зону A лишь запоминается (шаг еще не выполнен).
	/// - Вход в Зону B после A — успех; вход в B без A — ошибка.
	/// - Отметка в Зоне A сбрасывается при каждой активации шага, поэтому заход в A
	///   до наступления очереди не засчитывается.
	/// Зоны — обычные коллайдеры (isTrigger выставляется автоматически), достаточно
	/// перетащить их в поля Zone A / Zone B.
	/// </summary>
	public class SequenceActionSource : ActionSourceBase
	{
		[Header("Зоны шлюза")]
		[Tooltip("Первая зона, которую необходимо посетить (Trigger A)")]
		[SerializeField] private Collider zoneA;

		[Tooltip("Вторая зона, куда разрешено заходить только после зоны A (Trigger B)")]
		[SerializeField] private Collider zoneB;

		[Tooltip("Тег игрока для фильтрации")]
		[SerializeField] private string requiredTag = "Player";

		[Header("Сообщения")]
		[SerializeField] private string successMessage = "Шлюз успешно пройден: вход в Зону B после отметки в Зоне A.";
		[SerializeField] private string violationMessage = "Нарушение регламента: вход в Зону B без предварительной отметки в Зоне A!";

		private bool _hasVisitedZoneA;

		public bool HasVisitedZoneA => _hasVisitedZoneA;

		private void Awake()
		{
			BindZone(zoneA, OnZoneAEntered);
			BindZone(zoneB, OnZoneBEntered);
		}

		private static void BindZone(Collider zone, Action<Collider> handler)
		{
			if (zone == null) return;
			zone.isTrigger = true;
			var relay = zone.GetComponent<ZoneTriggerRelay>();
			if (relay == null) relay = zone.gameObject.AddComponent<ZoneTriggerRelay>();
			relay.Entered += handler;
		}

		private bool IsPlayer(Collider other) =>
			string.IsNullOrEmpty(requiredTag) || other.CompareTag(requiredTag);

		private void OnZoneAEntered(Collider other)
		{
			if (!isSourceEnabled || !IsStepActive || !IsPlayer(other)) return;
			_hasVisitedZoneA = true;
		}

		private void OnZoneBEntered(Collider other)
		{
			if (!isSourceEnabled || !IsPlayer(other)) return;
			if (_hasVisitedZoneA) NotifyAction(true, successMessage);
			else NotifyAction(false, violationMessage);
		}

		protected override void OnStepActivated()
		{
			_hasVisitedZoneA = false;
		}

		public override void ResetState()
		{
			base.ResetState();
			_hasVisitedZoneA = false;
		}
	}
}
