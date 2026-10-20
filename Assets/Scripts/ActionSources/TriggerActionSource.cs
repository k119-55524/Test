using UnityEngine;

namespace ScenarioSystem.ActionSources
{
	/// <summary>
	/// Источник действия от триггера Unity (OnTriggerEnter).
	/// Срабатывает, когда игрок (или указанный тег/коллайдер) пересекает границу зоны.
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

		private Collider _collider;

		private void Awake()
		{
			_collider = GetComponent<Collider>();
			if (_collider != null)
			{
				_collider.isTrigger = true;
			}
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
	}
}
