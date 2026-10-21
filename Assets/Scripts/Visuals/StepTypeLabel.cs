using UnityEngine;

namespace ScenarioSystem
{
	/// <summary>
	/// Надпись над тестом (TextMesh, текст задается в префабе): в Play всегда повернута к камере.
	/// </summary>
	public class StepTypeLabel : MonoBehaviour
	{
		private Camera _camera;

		// Повернуть к камере
		private void LateUpdate()
		{
			if (_camera == null)
				_camera = Camera.main;

			if (_camera != null)
				transform.rotation = _camera.transform.rotation;
		}
	}
}
