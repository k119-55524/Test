namespace ScenarioSystem
{
	/// <summary>
	/// Режим прохождения: обучение или экзамен.
	/// </summary>
	public enum ScenarioMode
	{
		Training = 0,
		Exam = 1
	}

	/// <summary>
	/// Статус шага.
	/// </summary>
	public enum StepStatus
	{
		Pending = 0,    // Ожидает своей очереди (не активен)
		Active = 1,     // Активен в данный момент, принимает действия игрока
		Completed = 2,  // Успешно завершен
		Failed = 3,     // Завален ошибкой (в режиме экзамена)
		Skipped = 4     // Пропущен из-за порога нарушений этапа (в режиме экзамена)
	}

	/// <summary>
	/// Реакция на действие на шаге до его очереди.
	/// </summary>
	public enum OutOfOrderPolicy
	{
		RejectAsViolation = 0,
		IgnoreUntilActive = 1
	}

	/// <summary>
	/// Правило шага для действий вне очереди: как в менеджере или свое.
	/// </summary>
	public enum OutOfOrderOverride
	{
		UseScenarioDefault = 0,
		RejectAsViolation = 1,
		IgnoreUntilActive = 2
	}

	/// <summary>
	/// Тип записи журнала.
	/// </summary>
	public enum ScenarioEventType
	{
		ScenarioStarted,        // Старт прохождения (ExtraData: режим)
		StepActivated,          // Шаг вошел в очередь
		StepCompleted,          // Шаг выполнен
		StepFailed,             // Ошибка на активном шаге (Обучение: повтор; Экзамен: Failed)
		StepSkipped,            // Шаг пропущен из-за порога нарушений этапа (Экзамен)
		OutOfOrderViolation,    // Действие на шаге вне его очереди
		HazardViolation,        // Вход в опасную зону при включенной опасности (нарушение ТБ)
		GroupStarted,           // Начата группа
		GroupCompleted,         // Группа завершена
		GroupSkipped,           // Группа закрыта досрочно по порогу нарушений (Экзамен)
		StageStarted,           // Начат этап активации; в сообщении — его состав
		StageCompleted,         // Этап активации завершен
		ScenarioFinished        // Прохождение завершено
	}
}
