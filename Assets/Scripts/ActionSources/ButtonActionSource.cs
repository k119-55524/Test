using UnityEngine;
using UnityEngine.UI;

namespace ScenarioSystem.ActionSources
{
	/// <summary>
	/// Источник действия: нажатие UI-кнопки.
	/// </summary>
	public class ButtonActionSource : ActionSourceBase
	{
		[Header("Привязка UI")]
		[Tooltip("Кнопка Unity UI. Если не назначена, ищется на этом же объекте.")]
		[SerializeField] private Button targetButton;

		[Tooltip("Считать ли нажатие этой кнопки успешным действием (true) или ошибочным (false, например аварийная кнопка)")]
		[SerializeField] private bool treatClickAsSuccess = true;

		[Tooltip("Сообщение при нажатии кнопки")]
		[SerializeField] private string clickMessage = "Нажата кнопка на пульте";

		private void Awake()
		{
			if (targetButton == null)
			{
				targetButton = GetComponent<Button>();
			}
		}

		private void OnEnable()
		{
			if (targetButton != null)
			{
				targetButton.onClick.AddListener(HandleClick);
			}
		}

		private void OnDisable()
		{
			if (targetButton != null)
			{
				targetButton.onClick.RemoveListener(HandleClick);
			}
		}

		private void HandleClick()
		{
			if (!isSourceEnabled) return;
			NotifyAction(treatClickAsSuccess, clickMessage);
		}

		public override void EnableSource()
		{
			base.EnableSource();
			if (targetButton != null) targetButton.interactable = true;
		}

		public override void DisableSource()
		{
			base.DisableSource();
			if (targetButton != null) targetButton.interactable = false;
		}
	}
}
