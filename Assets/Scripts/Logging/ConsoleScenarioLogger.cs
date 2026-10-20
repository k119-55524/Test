using UnityEngine;
using ScenarioSystem.Core;
using System.Collections.Generic;

namespace ScenarioSystem.Logging
{
	/// <summary>
	/// Логгер: хранит журнал в памяти и выводит записи в консоль Unity.
	/// </summary>
	[DisallowMultipleComponent]
	public class ConsoleScenarioLogger : MonoBehaviour, IScenarioLogger
	{
		[Tooltip("Дублировать записи журнала в консоль Unity")]
		[SerializeField] private bool writeToConsole = true;

		/// <summary>Метка в начале каждой записи консоли — отделяет журнал сценария от прочих сообщений.</summary>
		private const string ConsolePrefix = "[TestLogger] ";

		private readonly List<ScenarioLogEntry> _entries = new();

		public IReadOnlyList<ScenarioLogEntry> Entries => _entries;

		public void Log(ScenarioLogEntry entry)
		{
			if (entry == null) return;
			_entries.Add(entry);
			if (writeToConsole) WriteEntryToConsole(entry);
		}

		public void Clear()
		{
			_entries.Clear();
		}

		/// <summary>
		/// Выводит запись в консоль короткой строкой; активация шага и завершение этапа не выводятся.
		/// </summary>
		private void WriteEntryToConsole(ScenarioLogEntry entry)
		{
			const string indent = "    ";
			string text;
			bool warning = false;

			switch (entry.EventType)
			{
				case ScenarioEventType.StepActivated:
				case ScenarioEventType.GroupCompleted:
					return;
				case ScenarioEventType.StepCompleted:
					text = $"{indent}Выполнено, попыток: {entry.AttemptIndex} — {entry.StepTitle}";
					break;
				case ScenarioEventType.StepFailed:
					string result = entry.ResultStatus == StepStatus.Failed ? "Провалено" : "Ошибка";
					text = $"{indent}{result}, попытка: {entry.AttemptIndex} — {entry.StepTitle}: {entry.Message}";
					warning = true;
					break;
				case ScenarioEventType.StepSkipped:
					text = $"{indent}Пропущено — {entry.StepTitle}: {entry.Message}";
					warning = true;
					break;
				case ScenarioEventType.OutOfOrderViolation:
					text = $"{indent}Нарушение порядка: {entry.Message}";
					warning = true;
					break;
				case ScenarioEventType.HazardViolation:
					text = $"{indent}Нарушение ТБ — {entry.StepTitle}: {entry.Message}";
					warning = true;
					break;
				case ScenarioEventType.GroupSkipped:
					text = entry.Message;
					warning = true;
					break;
				default:
					text = entry.Message;
					break;
			}

			// Многострочная запись (например, состав этапа) выводится построчно, каждая строка с меткой
			foreach (string line in text.Split('\n'))
			{
				if (warning) Debug.LogWarning(ConsolePrefix + line);
				else Debug.Log(ConsolePrefix + line);
			}
		}
	}
}
