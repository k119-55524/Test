using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScenarioSystem.Manager
{
	/// <summary>
	/// Этап сценария: название, порог нарушений (экзамен) и тесты, выполняемые в любом порядке.
	/// </summary>
	[Serializable]
	public class ScenarioGroup
	{
		[Tooltip("Название группы по смыслу (например «Допуск к работе»). Используется в журнале, сообщениях и интерфейсе")]
		[SerializeField] private string groupName = "Группа";

		[Tooltip("Порог нарушений в этапе (Экзамен). По умолчанию 2")]
		[Min(1)]
		[SerializeField] private int violationThreshold = 2;

		[Tooltip("Тесты этапа — все активны сразу, выполняются в любом порядке. Перетащите объекты тестов со сцены")]
		[SerializeField] private List<GameObject> tests = new();

		public string GroupName => groupName;
		public int ViolationThreshold => violationThreshold;
		public IReadOnlyList<GameObject> Tests => tests;

		/// <summary>Порог меньше 1 (новый элемент списка) заменяется значением по умолчанию — 2.</summary>
		public void ApplyDefaults()
		{
			if (violationThreshold < 1) violationThreshold = 2;
		}
	}
}
