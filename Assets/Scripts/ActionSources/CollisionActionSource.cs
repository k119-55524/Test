using UnityEngine;

namespace ScenarioSystem.ActionSources
{
	/// <summary>
	/// Источник действия от физической коллизии Unity (OnCollisionEnter).
	/// Срабатывает при физическом ударе или соприкосновении твердых тел (Rigidbody / Collider).
	/// </summary>
	[RequireComponent(typeof(Collider))]
	public class CollisionActionSource : ActionSourceBase
	{
		[Header("Настройки коллизии")]
		[Tooltip("Фильтр по тегу объекта (по умолчанию 'Player').")]
		[SerializeField] private string requiredTag = "Player";

		[Tooltip("Считать ли столкновение успехом (например толчок ящика) или нарушением (столкновение со стойкой)")]
		[SerializeField] private bool treatCollisionAsSuccess = true;

		[Tooltip("Текст при срабатывании")]
		[SerializeField] private string collisionMessage = "Физическое столкновение с объектом";

		private Collider _collider;

		private void Awake()
		{
			_collider = GetComponent<Collider>();
			if (_collider != null)
			{
				_collider.isTrigger = false;
			}
		}

		private void OnCollisionEnter(Collision collision)
		{
			if (!isSourceEnabled) return;

			if (!string.IsNullOrEmpty(requiredTag) && !collision.gameObject.CompareTag(requiredTag))
			{
				return;
			}

			NotifyAction(treatCollisionAsSuccess, collisionMessage);
		}
	}
}
