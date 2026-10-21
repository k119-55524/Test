using UnityEngine;

namespace ScenarioSystem
{
	/// <summary>
	/// Тест «UI Button»: игрок подходит к пульту и нажимает одну из кнопок на экране;
	/// правильная кнопка — шаг выполнен, другая — ошибка.
	/// </summary>
	[RequireComponent(typeof(Collider))]
	public class ButtonStep : StepBase
	{
		[Header("Button")]
		[Tooltip("Панель с кнопками (Canvas): показывается, когда игрок у пульта")]
		[SerializeField] private GameObject panel;

		[Tooltip("Номер правильной кнопки — тот, что передает ее On Click в Choose")]
		[Min(1)]
		[SerializeField] private int correctButton = 1;

		private PlayerController _player;

		// Перезапуск или стоп — панель скрывается
		protected override void OnReset()
		{
			if (panel.activeSelf)
				SetPanelVisible(false);
		}

		// Игрок у пульта: в очередь — панель, вне очереди — нарушение
		private void OnTriggerEnter(Collider other)
		{
			if (!IsPlayer(other))
				return;

			if (!IsActive)
			{
				ReportAction(false, "Подход к пульту вне очереди");
				return;
			}

			_player = other.GetComponent<PlayerController>();
			SetPanelVisible(true);
		}

		// Нажата кнопка с номером number (из ее On Click)
		public void Choose(int number)
		{
			SetPanelVisible(false);
			bool correct = number == correctButton;
			ReportAction(correct, correct ? "Нажата правильная кнопка" : "Нажата неправильная кнопка");
		}

		// Показ панели: курсор свободен, персонаж стоит; скрытие — наоборот
		private void SetPanelVisible(bool visible)
		{
			panel.SetActive(visible);
			Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
			Cursor.visible = visible;

			if (_player != null)
				_player.enabled = !visible;
		}
	}
}