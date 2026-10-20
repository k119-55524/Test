using System.Collections.Generic;

namespace ScenarioSystem.Core
{
	/// <summary>
	/// Минимальный расширяемый контракт события для структурированного журнала (ТЗ п. 5).
	///
	/// Логика архитектуры:
	/// 1. Неизменяемость (Immutable): запись фиксирует свершившийся факт во времени.
	/// 2. Изоляция: содержит только «сырые данные» без привязки к конкретному алгоритму оценки.
	/// 3. Расширяемость: словарь ExtraData позволяет добавлять любые новые параметры
	///    (координаты, силу удара, тайминги) без поломки существующих логгеров и анализаторов.
	/// </summary>
	public class ScenarioLogEntry
	{
		/// <summary>Ключ ExtraData: режим прохождения (пишется в событие ScenarioStarted).</summary>
		public const string KeyMode = "mode";

		/// <summary>Ключ ExtraData: количество шагов в конфигурации режима (событие ScenarioStarted).</summary>
		public const string KeyTotalSteps = "totalSteps";

		/// <summary>
		/// Внутриигровое время фиксации события (от старта сценария в секундах).
		/// </summary>
		public float Timestamp { get; }

		/// <summary>
		/// Уникальный идентификатор шага/теста, к которому относится действие.
		/// Если событие глобальное (например старт группы), может быть пустым.
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
		/// Номер попытки на активном шаге (начиная с 1); 0 — попыток еще не было
		/// (активация шага, действие вне очереди, пропуск).
		/// Критично для расчета баллов в режиме Обучения (10 за 1-ю, 5 за 2-ю, 0 за 3-ю+).
		/// </summary>
		public int AttemptIndex { get; }

		/// <summary>
		/// Номер группы/станции, в рамках которой произошло действие (для Экзамена).
		/// </summary>
		public int GroupIndex { get; }

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
			int attemptIndex = 1,
			int groupIndex = 0,
			string message = "",
			Dictionary<string, string> extraData = null)
		{
			Timestamp = timestamp;
			StepId = stepId ?? string.Empty;
			StepTitle = stepTitle ?? string.Empty;
			EventType = eventType;
			ResultStatus = resultStatus;
			AttemptIndex = attemptIndex;
			GroupIndex = groupIndex;
			Message = message ?? string.Empty;
			ExtraData = extraData ?? new Dictionary<string, string>();
		}

		public override string ToString()
		{
			return $"[{Timestamp:F2}s] [{EventType}] Step='{StepId}' ({StepTitle}) Status={ResultStatus} Attempt={AttemptIndex} Msg='{Message}'";
		}
	}
}
