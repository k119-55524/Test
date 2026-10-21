using UnityEngine;
using UnityEngine.InputSystem;

namespace ScenarioSystem
{
	/// <summary>
	/// Управление персонажем: мышь — поворот, W/S — вперед/назад, A/D — шаг вбок.
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(CapsuleCollider))]
	public class PlayerController : MonoBehaviour
	{
		[SerializeField] private float moveSpeed = 6f;
		[SerializeField] private float mouseSensitivity = 0.2f;

		// Сколько кадров пропускать движение мыши после включения или возврата фокуса
		private const int MouseSettleFrames = 3;

		private Rigidbody _rb;
		private float _yaw;
		private int _skipMouseFrames;

		private void Awake()
		{
			_rb = GetComponent<Rigidbody>();
			_yaw = transform.eulerAngles.y;
		}

		// Включен — первые кадры мыши пропускаются
		private void OnEnable()
		{
			_skipMouseFrames = MouseSettleFrames;
		}

		// Выключен — персонаж останавливается
		private void OnDisable()
		{
			_rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
		}

		// Курсор захватывается
		private void Start()
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}

		// Мышь — поворот
		private void Update()
		{
			var mouse = Mouse.current;
			if (mouse == null) return;

			if (_skipMouseFrames > 0)
			{
				_skipMouseFrames--;
				return;
			}

			_yaw += mouse.delta.ReadValue().x * mouseSensitivity;
		}

		// Возврат фокуса — первые кадры мыши пропускаются
		private void OnApplicationFocus(bool hasFocus)
		{
			if (hasFocus)
				_skipMouseFrames = MouseSettleFrames;
		}

		// Поворот и движение по WASD
		private void FixedUpdate()
		{
			_rb.MoveRotation(Quaternion.Euler(0f, _yaw, 0f));

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