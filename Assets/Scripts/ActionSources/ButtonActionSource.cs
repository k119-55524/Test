using System.Collections.Generic;
using ScenarioSystem.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ScenarioSystem.ActionSources
{
	/// <summary>
	/// Источник действия: нажатие UI-кнопки на панели пульта. Игрок подошел к пульту в свою очередь —
	/// панель появляется, управление персонажем отключается; вне очереди — действие вне очереди, панели нет.
	/// Нажата кнопка — панель скрывается: правильная — успех, другая — ошибка.
	/// </summary>
	[RequireComponent(typeof(Collider))]
	public class ButtonActionSource : ActionSourceBase
	{
		[Header("Панель пульта")]
		[Tooltip("Панель с кнопками (Canvas): показывается, когда игрок у пульта")]
		[SerializeField] private GameObject panel;

		[Tooltip("Кнопки вариантов")]
		[SerializeField] private List<Button> buttons = new();

		[Tooltip("Номер правильной кнопки (с 1)")]
		[Min(1)]
		[SerializeField] private int correctButton = 1;

		[Tooltip("Тег игрока")]
		[SerializeField] private string requiredTag = "Player";

		[SerializeField] private string correctMessage = "Нажата правильная кнопка";
		[SerializeField] private string wrongMessage = "Нажата неправильная кнопка";

		private PlayerController _player;

		private void Awake()
		{
			GetComponent<Collider>().isTrigger = true;
			for (int i = 0; i < buttons.Count; i++)
			{
				int index = i;
				if (buttons[i] != null) buttons[i].onClick.AddListener(() => HandleClick(index));
			}
			SetPanelVisible(false);
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!isSourceEnabled || !IsPlayer(other)) return;

			// Вне очереди панель не показывается: сам подход к пульту — действие вне очереди
			if (!IsStepActive)
			{
				NotifyAction(false, "Подход к пульту вне очереди");
				return;
			}

			_player = other.GetComponent<PlayerController>();
			SetPanelVisible(true);
		}

		private void OnTriggerExit(Collider other)
		{
			if (IsPlayer(other)) SetPanelVisible(false);
		}

		public override void DisableSource()
		{
			base.DisableSource();
			if (panel != null && panel.activeSelf) SetPanelVisible(false);
		}

		private void HandleClick(int index)
		{
			SetPanelVisible(false);
			if (!isSourceEnabled) return;

			bool correct = index == correctButton - 1;
			NotifyAction(correct, correct ? correctMessage : wrongMessage);
		}

		private bool IsPlayer(Collider other) =>
			string.IsNullOrEmpty(requiredTag) || other.CompareTag(requiredTag);

		/// <summary>Панель открыта — курсор свободен, персонаж стоит; закрыта — курсор захвачен, управление возвращено.</summary>
		private void SetPanelVisible(bool visible)
		{
			if (panel != null) panel.SetActive(visible);
			Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
			Cursor.visible = visible;
			if (_player != null) _player.enabled = !visible;
		}
	}
}
