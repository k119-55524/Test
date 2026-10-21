using UnityEngine;
using System.Collections.Generic;

namespace ScenarioSystem
{
	/// <summary>
	/// Логгер: хранит журнал в памяти и выводит записи в консоль Unity.
	/// </summary>
	[DisallowMultipleComponent]
	public class ConsoleScenarioLogger : MonoBehaviour, IScenarioLogger
	{
		[Tooltip("Дублировать записи журнала в консоль Unity")]
		[SerializeField] private bool writeToConsole = true;

		// Метка строк журнала в консоли
		private const string ConsolePrefix = "[TestLogger] ";

		private readonly List<ScenarioLogEntry> _entries = new();

		public IReadOnlyList<ScenarioLogEntry> Entries => _entries;

		// Добавить запись и вывести в консоль
		public void Log(ScenarioLogEntry entry)
		{
			if (entry == null) return;
			_entries.Add(entry);
			if (writeToConsole) WriteEntryToConsole(entry);
		}

		// Очистить журнал
		public void Clear()
		{
			_entries.Clear();
		}

		// Запись в консоль короткой строкой (активация шага, конец этапа и группы не выводятся)
		private void WriteEntryToConsole(ScenarioLogEntry entry)
		{
			const string indent = "    ";
			string text;
			bool warning = false;

			switch (entry.EventType)
			{
				case ScenarioEventType.StepActivated:
				case ScenarioEventType.StageCompleted:
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

			// Многострочная запись (состав этапа) — построчно, каждая строка с меткой
			foreach (string line in text.Split('\n'))
			{
				if (warning) Debug.LogWarning(ConsolePrefix + line);
				else Debug.Log(ConsolePrefix + line);
			}
		}
	}
}
