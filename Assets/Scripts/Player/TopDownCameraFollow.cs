using UnityEngine;

namespace ScenarioSystem.Player
{
	/// <summary>
	/// Камера от третьего лица: держится за спиной цели со смещением offset
	/// (в локальных осях цели, поворачивается вместе с ней) и смотрит на точку над ее пивотом.
	/// </summary>
	public class TopDownCameraFollow : MonoBehaviour
	{
		[SerializeField] private Transform target;
		[SerializeField] private Vector3 offset = new Vector3(0f, 6f, -6f);

		private void LateUpdate()
		{
			if (target == null)
				return;

			transform.position = target.position + target.rotation * offset;
			transform.LookAt(target.position + Vector3.up * 1f);
		}
	}
}