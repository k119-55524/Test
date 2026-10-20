namespace ScenarioSystem.Core
{
	/// <summary>
	/// Режим прохождения сценария.
	/// Определяет правила активации, политику ошибок и алгоритм оценивания:
	/// - Training: режим обучения (неограниченные попытки, подсказки, подсчет попыток: 10/5/0 баллов).
	/// - Exam: режим экзамена (деление на группы, лимит нарушений, статус Failed/Skipped, штрафы).
	/// </summary>
	public enum ScenarioMode
	{
		Training = 0,
		Exam = 1
	}

	/// <summary>
	/// Статус выполнения отдельного шага/теста.
	/// Прямо соответствует требованиям ТЗ (Completed, Failed, Skipped) и состояниям жизненного цикла.
	/// </summary>
	public enum StepStatus
	{
		Pending = 0,    // Ожидает своей очереди (не активен)
		Active = 1,     // Активен в данный момент, принимает действия игрока
		Completed = 2,  // Успешно завершен
		Failed = 3,     // Завален ошибкой (в режиме экзамена)
		Skipped = 4     // Пропущен из-за превышения порога ошибок в группе (в режиме экзамена)
	}

	/// <summary>
	/// Реакция системы на действие игрока, относящееся к шагу вне его очереди:
	/// - RejectAsViolation (по умолчанию в ТЗ): фиксирует действие как нарушение и ошибку регламента.
	/// - IgnoreUntilActive: шаг полностью пассивен и игнорирует взаимодействие до наступления своей очереди.
	/// </summary>
	public enum OutOfOrderPolicy
	{
		RejectAsViolation = 0,
		IgnoreUntilActive = 1
	}

	/// <summary>
	/// Индивидуальное правило шага для действий вне очереди.
	/// UseScenarioDefault — взять общую политику из ScenarioManager.
	/// </summary>
	public enum OutOfOrderOverride
	{
		UseScenarioDefault = 0,
		RejectAsViolation = 1,
		IgnoreUntilActive = 2
	}

	/// <summary>
	/// Тип зарегистрированного в журнале события.
	/// Позволяет логгерам и анализаторам фильтровать и категоризировать факты прохождения.
	/// </summary>
	public enum ScenarioEventType
	{
		ScenarioStarted,        // Старт прохождения (ExtraData: режим, количество шагов)
		StepActivated,          // Шаг вошел в очередь
		StepCompleted,          // Шаг выполнен
		StepFailed,             // Ошибка на активном шаге (Обучение: повтор; Экзамен: Failed)
		StepSkipped,            // Шаг пропущен из-за порога нарушений группы (Экзамен)
		OutOfOrderViolation,    // Действие на шаге вне его очереди
		HazardViolation,        // Вход в опасную зону при включенной опасности (нарушение ТБ)
		GroupCompleted,         // Экзаменационная группа завершена
		GroupSkipped,           // Группа закрыта досрочно по порогу нарушений
		ScenarioFinished        // Прохождение завершено
	}
}
