using System;

using UnityEngine;
using ScenarioSystem.Core;
using System.Collections.Generic;

namespace ScenarioSystem.Observers.Logging
{
	/// <summary>
	/// Журнал прохождения сценария (ТЗ п. 5). Компонент на объекте ScenarioManager:
	/// повешен — события прохождения пишутся, не повешен — журнал не ведется.
	///
	/// Логика:
	/// - Хранит все события прохождения (ScenarioLogEntry) в порядке поступления.
	/// - Событие OnEntryAdded — для экранного журнала (UI подписывается и дорисовывает строки).
	/// - WriteToConsole — дублировать записи в консоль Unity для отладки.
	/// Анализаторы получают готовый журнал (Entries) и работают отдельно от прохождения,
	/// поэтому завершенное прохождение можно переоценить без перезапуска сцены.
	/// </summary>
	[DisallowMultipleComponent]
	public class ScenarioLogger : MonoBehaviour
	{
		[Tooltip("Дублировать записи журнала в консоль Unity")]
		[SerializeField] private bool writeToConsole = true;

		private readonly List<ScenarioLogEntry> _entries = new();

		/// <summary>Дублировать записи в консоль Unity.</summary>
		public bool WriteToConsole
		{
			get => writeToConsole;
			set => writeToConsole = value;
		}

		/// <summary>Новая запись добавлена в журнал.</summary>
		public event Action<ScenarioLogEntry> OnEntryAdded;

		/// <summary>Все записи текущего (или последнего завершенного) прохождения.</summary>
		public IReadOnlyList<ScenarioLogEntry> Entries => _entries;

		public void Log(ScenarioLogEntry entry)
		{
			if (entry == null) return;
			_entries.Add(entry);
			if (writeToConsole) WriteEntryToConsole(entry);
			OnEntryAdded?.Invoke(entry);
		}

		/// <summary>Очищает журнал перед новым прохождением.</summary>
		public void Clear()
		{
			_entries.Clear();
		}

		private static void WriteEntryToConsole(ScenarioLogEntry entry)
		{
			string text = $"[Scenario:{entry.EventType}] {entry.StepTitle} (ID: {entry.StepId}) -> {entry.ResultStatus} | {entry.Message}";

			switch (entry.EventType)
			{
				case ScenarioEventType.StepFailed:
				case ScenarioEventType.StepSkipped:
				case ScenarioEventType.OutOfOrderViolation:
				case ScenarioEventType.HazardViolation:
				case ScenarioEventType.GroupSkipped:
					Debug.LogWarning(text);
					break;
				default:
					Debug.Log(text);
					break;
			}
		}
	}
}
