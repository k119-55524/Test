using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ScenarioSystem
{
	/// <summary>
	/// Экранный интерфейс сценария (uGUI): текущие задания, сообщения о нарушениях,
	/// в конце — итог и журнал прохождения с прокруткой.
	/// </summary>
	public class ScenarioHud : MonoBehaviour, IScenarioView
	{
		private const float MessageSeconds = 3f;

		[Tooltip("Менеджер сценария: журнал прохождения")]
		[SerializeField] private ScenarioManager manager;

		[Tooltip("Управление персонажем: выключается, пока открыта панель итога")]
		[SerializeField] private PlayerController player;

		[Header("Задания")]
		[Tooltip("Панель заданий: скрыта, когда заданий нет")]
		[SerializeField] private GameObject tasksPanel;

		[Tooltip("Текст списка текущих заданий")]
		[SerializeField] private Text tasksText;

		[Header("Сообщение")]
		[Tooltip("Плашка сообщения о нарушении")]
		[SerializeField] private GameObject messagePanel;

		[Tooltip("Текст сообщения")]
		[SerializeField] private Text messageText;

		[Header("Итог")]
		[Tooltip("Панель итога: появляется после прохождения")]
		[SerializeField] private GameObject resultPanel;

		[Tooltip("Заголовок панели итога")]
		[SerializeField] private Text resultTitle;

		[Tooltip("Текст итога и журнала (внутри прокрутки)")]
		[SerializeField] private Text resultText;

		[Tooltip("Прокрутка текста итога")]
		[SerializeField] private ScrollRect resultScroll;

		private Coroutine _hideMessage;

		// Текущие задания: название и как выполнить
		public void ShowActiveSteps(IReadOnlyCollection<StepBase> steps)
		{
			tasksPanel.SetActive(steps.Count > 0);
			if (steps.Count == 0)
				return;

			// Новое прохождение — панель итога закрывается
			if (resultPanel.activeSelf)
			{
				resultPanel.SetActive(false);
				SetPlayerControl(true);
			}

			var text = new StringBuilder("Задания:");
			foreach (var step in steps)
				text.Append($"\n• {step.StepTitle} — {step.Description}");

			tasksText.text = text.ToString();
		}

		// Сообщение на несколько секунд; новое, пока старое на экране, дописывается снизу
		public void ShowMessage(string text)
		{
			messageText.text = messagePanel.activeSelf ? $"{messageText.text}\n{text}" : text;
			messagePanel.SetActive(true);

			if (_hideMessage != null)
				StopCoroutine(_hideMessage);
			_hideMessage = StartCoroutine(HideMessage());
		}

		// Итог сверху, журнал снизу; курсор свободен, персонаж стоит
		public void ShowResult(string title, ScenarioReport report)
		{
			var text = new StringBuilder();
			text.Append($"Оценка: {report.TotalScore} / {report.MaxScore} ({report.AnalyzerName})\n");
			text.Append($"Выполнено: {report.CompletedStepsCount}");
			if (report.FailedStepsCount > 0)
				text.Append($", провалено: {report.FailedStepsCount}");
			if (report.SkippedStepsCount > 0)
				text.Append($", пропущено: {report.SkippedStepsCount}");
			text.Append($"\nНарушений: {report.TotalViolations}, время: {report.DurationSeconds:F1} с\n\n");
			foreach (string line in report.Details)
				text.Append(line).Append('\n');

			text.Append("\n————————————————————————————————\nЖурнал прохождения\n");
			foreach (var entry in manager.Entries)
			{
				string step = string.IsNullOrEmpty(entry.StepId) ? entry.StepTitle : $"{entry.StepId} {entry.StepTitle}";
				string attempt = entry.AttemptIndex > 0 ? $", попытка {entry.AttemptIndex}" : "";
				text.Append($"{step} — {entry.EventType} ({entry.ResultStatus}{attempt}): {entry.Message}\n");
			}

			resultTitle.text = title;
			resultText.text = text.ToString();
			resultScroll.verticalNormalizedPosition = 1f;
			resultPanel.SetActive(true);
			SetPlayerControl(false);
		}

		// Курсор и управление персонажем: панель открыта — курсор свободен, персонаж стоит
		private void SetPlayerControl(bool active)
		{
			Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !active;
			if (player != null)
				player.enabled = active;
		}

		private IEnumerator HideMessage()
		{
			yield return new WaitForSeconds(MessageSeconds);
			messagePanel.SetActive(false);
			_hideMessage = null;
		}
	}
}
