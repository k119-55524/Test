using UnityEngine;

namespace ScenarioSystem
{
	/// <summary>
	/// Цвета оформления шагов: не пройден, пройден, ошибка.
	/// </summary>
	public static class StepColors
	{
		public static readonly Color NotPassed = new Color(0.45f, 0.65f, 0.95f);
		public static readonly Color Passed = new Color(0.3f, 0.75f, 0.35f);
		public static readonly Color Error = new Color(0.9f, 0.2f, 0.2f);

		// Длительность красной вспышки (клик, событие), с
		public const float ErrorFlashSeconds = 1.5f;
	}
}
