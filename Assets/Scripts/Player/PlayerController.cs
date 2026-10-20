using UnityEngine;
using UnityEngine.InputSystem;

namespace ScenarioSystem.Player
{
	/// <summary>
	/// Управление персонажем от третьего лица.
	/// - Движение мыши по горизонтали поворачивает персонажа влево/вправо.
	/// - W/S или стрелки вверх/вниз — движение вперед/назад относительно взгляда персонажа.
	/// - A/D или стрелки влево/вправо — шаг вбок (стрейф) без поворота.
	/// Тег «Player», по которому фильтруют источники действий, задан на объекте в сцене.
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(CapsuleCollider))]
	public class PlayerController : MonoBehaviour
	{
		[SerializeField] private float moveSpeed = 6f;
		[SerializeField] private float mouseSensitivity = 0.2f;

		private Rigidbody _rb;
		private float _yaw;

		private void Awake()
		{
			_rb = GetComponent<Rigidbody>();
			_yaw = transform.eulerAngles.y;
		}

		private void Update()
		{
			var mouse = Mouse.current;
			if (mouse != null)
			{
				_yaw += mouse.delta.ReadValue().x * mouseSensitivity;
			}
		}

		private void FixedUpdate()
		{
			transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

			float h = 0f;
			float v = 0f;
			var kb = Keyboard.current;
			if (kb != null)
			{
				if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
				if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
				if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
				if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
			}

			Vector3 moveDir = (transform.forward * v + transform.right * h).normalized;
			Vector3 velocity = moveDir * moveSpeed;
			velocity.y = _rb.linearVelocity.y;
			_rb.linearVelocity = velocity;
		}
	}
}