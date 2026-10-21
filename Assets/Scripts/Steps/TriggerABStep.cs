using UnityEngine;

namespace ScenarioSystem
{
	/// <summary>
	/// Тест «Trigger A → B»: игрок входит в зону A, затем в B — шаг выполнен; в B без A — ошибка.
	/// </summary>
	public class TriggerABStep : StepBase
	{
		[Header("Trigger A → B")]
		[Tooltip("Зона A (объект с триггером и ZoneTriggerRelay) — посетить первой")]
		[SerializeField] private ZoneTriggerRelay zoneA;

		[Tooltip("Зона B (объект с триггером и ZoneTriggerRelay) — входить только после зоны A")]
		[SerializeField] private ZoneTriggerRelay zoneB;

		[Header("Сообщения журнала")]
		[Tooltip("Вход в B после A — шаг выполнен")]
		[SerializeField] private string correctOrderMessage = "Вход в зону B после зоны A";

		[Tooltip("Вход в B без A — ошибка")]
		[SerializeField] private string wrongOrderMessage = "Вход в зону B без зоны A";

		private bool _visitedZoneA;

		// Ошибка — красный, пока игрок в зоне B
		protected override bool HoldsInteraction => true;

		protected override void Awake()
		{
			base.Awake();

			zoneA.Entered += OnZoneAEntered;
			zoneB.Entered += OnZoneBEntered;
			zoneB.Exited += OnZoneBExited;
		}

		// Новое прохождение — зона A не посещена
		protected override void OnReset() => _visitedZoneA = false;

		// Игрок вошел в зону A (учитывается только в очередь шага)
		private void OnZoneAEntered(Collider other)
		{
			if (IsActive && IsPlayer(other))
				_visitedZoneA = true;
		}

		// Игрок вошел в зону B
		private void OnZoneBEntered(Collider other)
		{
			if (!IsPlayer(other))
				return;

			if (_visitedZoneA)
				ReportAction(true, correctOrderMessage);
			else
				ReportAction(false, wrongOrderMessage);
		}

		// Игрок вышел из зоны B
		private void OnZoneBExited(Collider other)
		{
			if (IsPlayer(other))
				EndInteraction();
		}
	}
}
