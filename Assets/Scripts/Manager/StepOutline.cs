using System.Collections.Generic;
using UnityEngine;

namespace ScenarioSystem.Manager
{
	/// <summary>
	/// Добавляет материал контура к рендерерам шага, пока шаг активен.
	/// </summary>
	public class StepOutline
	{
		private readonly MeshRenderer[] _renderers;
		private readonly Material _material;
		private bool _visible;

		public StepOutline(GameObject root, Material material)
		{
			_renderers = root.GetComponentsInChildren<MeshRenderer>(true);
			_material = material;

			if (_material == null)
			{
				Debug.LogWarning("[StepOutline] Не задан материал контура (Outline Material) — контур не будет показан.", root);
			}
		}

		/// <summary>Показывает или скрывает контур.</summary>
		public void SetVisible(bool visible)
		{
			if (_material == null || _visible == visible) return;
			_visible = visible;

			foreach (var renderer in _renderers)
			{
				if (renderer == null) continue;

				var materials = new List<Material>(renderer.sharedMaterials);
				materials.Remove(_material);
				if (visible) materials.Add(_material);
				renderer.sharedMaterials = materials.ToArray();
			}
		}
	}
}
