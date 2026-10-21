using System.Collections.Generic;

namespace ScenarioSystem
{
	/// <summary>
	/// Запись журнала: время, шаг, тип события, статус, номер попытки, сообщение, доп. данные.
	/// </summary>
	public class ScenarioLogEntry
	{
		// Ключ ExtraData: режим прохождения (в записи ScenarioStarted)
		public const string KeyMode = "mode";

		// Время от старта сценария, с
		public float Timestamp { get; }

		// Id шага (step_01…); у событий сценария и этапа — пусто
		public string StepId { get; }

		// Название шага (или этапа)
		public string StepTitle { get; }

		// Тип события
		public ScenarioEventType EventType { get; }

		// Статус шага после события
		public StepStatus ResultStatus { get; }

		// Номер попытки (с 1); 0 — попыток не было
		public int AttemptIndex { get; }

		// Пояснение: причина, подсказка
		public string Message { get; }

		// Доп. данные (ключ-значение) для анализаторов
		public IReadOnlyDictionary<string, string> ExtraData { get; }

		public ScenarioLogEntry(
			float timestamp,
			string stepId,
			string stepTitle,
			ScenarioEventType eventType,
			StepStatus resultStatus,
			int attemptIndex = 0,
			string message = "",
			Dictionary<string, string> extraData = null)
		{
			Timestamp = timestamp;
			StepId = stepId ?? string.Empty;
			StepTitle = stepTitle ?? string.Empty;
			EventType = eventType;
			ResultStatus = resultStatus;
			AttemptIndex = attemptIndex;
			Message = message ?? string.Empty;
			ExtraData = extraData ?? new Dictionary<string, string>();
		}
	}
}
