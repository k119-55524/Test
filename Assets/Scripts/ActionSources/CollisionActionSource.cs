using UnityEngine;

namespace ScenarioSystem.ActionSources
{
	/// <summary>
	/// Источник действия: столкновение игрока с объектом.
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

		private void Awake()
		{
			GetComponent<Collider>().isTrigger = false;
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

		public override bool HoldsInteraction => true;

		private void OnCollisionExit(Collision collision)
		{
			if (!string.IsNullOrEmpty(requiredTag) && !collision.gameObject.CompareTag(requiredTag)) return;
			NotifyInteractionEnded();
		}
	}
}
