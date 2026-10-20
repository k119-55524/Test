using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScenarioSystem.Manager
{
	/// <summary>
	/// Линия сценария: набор тестов, которые активируются одновременно
	/// и выполняются в любом порядке (ТЗ п. 3.2).
	///
	/// Логика:
	/// Линии идут строго по очереди: следующая активируется, когда закрыты все тесты текущей.
	/// Последовательные шаги = несколько линий по одному тесту.
	/// Пример смешанной активации: [1, 2, 3, 4] → [5] → [6] → [7] → [8] → [9, 10] → [11, 12].
	/// Порядок линий и тестов меняется перетаскиванием в инспекторе.
	/// </summary>
	[Serializable]
	public class ScenarioStage
	{
		[Tooltip("Название линии (для интерфейса и отчетов)")]
		[SerializeField] private string stageName = "Линия";

		[Tooltip("Тесты линии: активируются вместе, выполняются в любом порядке")]
		[SerializeField] private List<StepInstance> steps = new();

		public string StageName => stageName;
		public IReadOnlyList<StepInstance> Steps => steps;
	}

	/// <summary>
	/// Группа сценария: последовательность линий и порог нарушений (ТЗ п. 4).
	///
	/// Логика:
	/// - Экзамен: при достижении порога нарушений незавершенные тесты группы получают Skipped,
	///   начинается следующая группа.
	/// - Обучение: границы групп и порог не учитываются — все линии идут подряд как одна общая группа.
	/// </summary>
	[Serializable]
	public class ScenarioGroup
	{
		[Tooltip("Название группы (например 'Группа 1: Допуск и подготовка')")]
		[SerializeField] private string groupName = "Группа";

		[Tooltip("Порог нарушений в группе (Экзамен). По умолчанию 2")]
		[Min(1)]
		[SerializeField] private int violationThreshold = 2;

		[Tooltip("Линии группы, выполняются строго по очереди")]
		[SerializeField] private List<ScenarioStage> stages = new();

		public string GroupName => groupName;
		public int ViolationThreshold => violationThreshold;
		public IReadOnlyList<ScenarioStage> Stages => stages;
	}
}
