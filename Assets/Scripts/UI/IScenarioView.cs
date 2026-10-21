using System.Collections.Generic;

namespace ScenarioSystem
{
	/// <summary>
	/// Вывод для игрока: текущие задания, сообщения о нарушениях, итог. Как показывать — решает реализация.
	/// </summary>
	public interface IScenarioView
	{
		// Текущие задания (активные шаги)
		void ShowActiveSteps(IReadOnlyCollection<StepBase> steps);

		// Сообщение об ошибке или нарушении с причиной
		void ShowMessage(string text);

		// Итог прохождения или повторной оценки
		void ShowResult(string title, ScenarioReport report);
	}
}
