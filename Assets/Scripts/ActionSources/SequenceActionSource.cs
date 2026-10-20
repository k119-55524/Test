using System;
using UnityEngine;

namespace ScenarioSystem.ActionSources
{
	/// <summary>
	/// Источник действия «шлюз»: сначала зона A, затем зона B; вход в B без A — ошибка.
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

		private void Awake()
		{
			BindZone(zoneA, OnZoneAEntered, null);
			BindZone(zoneB, OnZoneBEntered, OnZoneBExited);
		}

		public override bool HoldsInteraction => true;

		private void BindZone(Collider zone, Action<Collider> onEnter, Action<Collider> onExit)
		{
			if (zone == null) return;
			zone.isTrigger = true;
			var relay = zone.GetComponent<ZoneTriggerRelay>();
			if (relay == null) relay = zone.gameObject.AddComponent<ZoneTriggerRelay>();
			relay.Entered += onEnter;
			if (onExit != null) relay.Exited += onExit;
		}

		private void OnZoneBExited(Collider other)
		{
			if (IsPlayer(other)) NotifyInteractionEnded();
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
