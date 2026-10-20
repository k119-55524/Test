using ScenarioSystem.Manager;
using UnityEngine;

namespace ScenarioSystem.Visuals
{
	/// <summary>
	/// Надпись над тестом: показывает тип источника действия шага (Trigger, Collision, Button…)
	/// и всегда повернута к камере. Вешается на объект с TextMesh внутри теста.
	/// </summary>
	[RequireComponent(typeof(TextMesh))]
	public class StepTypeLabel : MonoBehaviour
	{
		[Tooltip("Шаг, тип которого показывается. Если пусто — ищется выше по иерархии")]
		[SerializeField] private StepInstance step;

		private Camera _camera;

		private void Awake()
		{
			if (step == null) step = GetComponentInParent<StepInstance>();

			var text = GetComponent<TextMesh>();
			if (text.font == null)
			{
				text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
				GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
			}
			if (step != null) text.text = step.SourceType;
		}

		private void LateUpdate()
		{
			if (_camera == null) _camera = Camera.main;
			if (_camera != null) transform.rotation = _camera.transform.rotation;
		}
	}
}
