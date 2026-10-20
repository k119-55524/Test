using UnityEngine;

namespace ScenarioSystem.ActionSources
{
	/// <summary>
	/// Источник действия: вход игрока в зону-триггер.
	/// </summary>
	[RequireComponent(typeof(Collider))]
	public class TriggerActionSource : ActionSourceBase
	{
		[Header("Настройки триггера")]
		[Tooltip("Фильтр по тегу объекта (по умолчанию 'Player'). Если пусто — принимает любой коллайдер.")]
		[SerializeField] private string requiredTag = "Player";

		[Tooltip("Считать ли вход в эту зону успехом")]
		[SerializeField] private bool treatEnterAsSuccess = true;

		[Tooltip("Текст при срабатывании")]
		[SerializeField] private string triggerMessage = "Вход в контрольную зону";

		private void Awake()
		{
			GetComponent<Collider>().isTrigger = true;
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!isSourceEnabled) return;

			if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
			{
				return;
			}

			NotifyAction(treatEnterAsSuccess, triggerMessage);
		}

		public override bool HoldsInteraction => true;

		private void OnTriggerExit(Collider other)
		{
			if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;
			NotifyInteractionEnded();
		}
	}
}
