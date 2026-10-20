using System.Collections.Generic;

namespace ScenarioSystem.Core
{
	/// <summary>
	/// Запись журнала: время, шаг, тип события, статус, номер попытки, сообщение, доп. данные.
	/// </summary>
	public class ScenarioLogEntry
	{
		/// <summary>Ключ ExtraData: режим прохождения (пишется в событие ScenarioStarted).</summary>
		public const string KeyMode = "mode";

		/// <summary>
		/// Внутриигровое время фиксации события (от старта сценария в секундах).
		/// </summary>
		public float Timestamp { get; }

		/// <summary>
		/// Идентификатор шага (step_01…), к которому относится событие.
		/// Для событий сценария и этапа — пустой.
		/// </summary>
		public string StepId { get; }

		/// <summary>
		/// Человекочитаемое название шага (для вывода в интерфейс и лог).
		/// </summary>
		public string StepTitle { get; }

		/// <summary>
		/// Категория/тип события (активация, завершение, ошибка, нарушение).
		/// </summary>
		public ScenarioEventType EventType { get; }

		/// <summary>
		/// Результирующий статус шага (Completed, Failed, Skipped, etc.).
		/// </summary>
		public StepStatus ResultStatus { get; }

		/// <summary>
		/// Номер попытки на шаге (с 1); 0 — попыток не было.
		/// </summary>
		public int AttemptIndex { get; }

		/// <summary>
		/// Текстовое пояснение / причина ошибки / подсказка.
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Расширяемые метаданные (ключ-значение) для кастомных анализаторов или отладки.
		/// </summary>
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
