using UnityEngine;

namespace ScenarioSystem
{
	/// <summary>
	/// Тест «Object State»: игрок входит в зону станка. Станок выключен — шаг выполнен;
	/// работает — ошибка шага (в его очередь) или нарушение ТБ (в любой момент).
	/// </summary>
	[RequireComponent(typeof(Collider))]
	public class ObjectStateStep : StepBase
	{
		[Header("Object State")]
		[Tooltip("Оборудование работает на старте сценария")]
		[SerializeField] private bool isHazardActive;

		[Tooltip("Индикатор работы оборудования (лампа)")]
		[SerializeField] private GameObject dangerIndicator;

		[Header("Сообщения журнала")]
		[Tooltip("Вход при выключенном оборудовании — шаг выполнен")]
		[SerializeField] private string safeEntryMessage = "Осмотр выключенного оборудования";

		[Tooltip("Вход при работающем оборудовании — ошибка шага или нарушение ТБ")]
		[SerializeField] private string hazardEntryMessage = "Вход в зону работающего оборудования";

		private bool _initialHazardActive;

		// Ошибка — красный, пока игрок в зоне
		protected override bool HoldsInteraction => true;

		protected override void Awake()
		{
			base.Awake();

			_initialHazardActive = isHazardActive;
			UpdateIndicator();
		}

		// Включить или выключить станок (из UnityEvent рубильника)
		public void SetHazardActive(bool active)
		{
			isHazardActive = active;
			UpdateIndicator();
		}

		// Новое прохождение — исходное состояние станка
		protected override void OnReset()
		{
			isHazardActive = _initialHazardActive;
			UpdateIndicator();
		}

		// Игрок вошел в зону
		private void OnTriggerEnter(Collider other)
		{
			if (!IsPlayer(other))
				return;

			if (isHazardActive)
			{
				if (IsActive)
					ReportAction(false, hazardEntryMessage);
				else
					ReportSafetyViolation(hazardEntryMessage);
			}
			else if (IsActive)
			{
				ReportAction(true, safeEntryMessage);
			}
		}

		// Игрок вышел из зоны
		private void OnTriggerExit(Collider other)
		{
			if (IsPlayer(other))
				EndInteraction();
		}

		// Лампа горит, пока станок работает
		private void UpdateIndicator()
		{
			if (dangerIndicator != null)
				dangerIndicator.SetActive(isHazardActive);
		}
	}
}
