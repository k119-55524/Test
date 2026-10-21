using UnityEngine;
using System.Collections;
using ScenarioSystem.Core;
using ScenarioSystem.Manager;

namespace ScenarioSystem.Visuals
{
	/// <summary>
	/// Красит площадку по статусу шага (не пройден / пройден), при ошибке — красным.
	/// </summary>
	public class StepStatusColor : MonoBehaviour
	{
		private const string BaseColorProperty = "_BaseColor";
		private const string ColorProperty = "_Color";

		[Tooltip("Шаг, статус которого показывается. Если пусто — ищется на этом объекте или выше")]
		[SerializeField] private StepInstance step;

		[Tooltip("Рендерер, который красится. Если пусто — на этом объекте")]
		[SerializeField] private Renderer targetRenderer;

		[SerializeField] private Color notPassedColor = new Color(0.45f, 0.65f, 0.95f);
		[SerializeField] private Color passedColor = new Color(0.3f, 0.75f, 0.35f);

		[Tooltip("Цвет вспышки при ошибке на шаге")]
		[SerializeField] private Color errorColor = new Color(0.9f, 0.2f, 0.2f);

		[Tooltip("Длительность вспышки при ошибке для мгновенных действий (клик, событие), секунды")]
		[SerializeField] private float errorFlashSeconds = 1.5f;

		private MaterialPropertyBlock _block;
		private Coroutine _flash;
		private bool _holdingError;

		private void Awake()
		{
			if (step == null) step = GetComponentInParent<StepInstance>();
			if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
			_block = new MaterialPropertyBlock();
		}

		private void OnEnable()
		{
			if (step != null)
			{
				step.OnStatusChanged += Apply;
				step.OnError += ShowError;
				step.OnInteractionEnded += EndHeldError;
			}
			Apply(step != null ? step.CurrentStatus : StepStatus.Pending);
		}

		private void OnDisable()
		{
			if (step != null)
			{
				step.OnStatusChanged -= Apply;
				step.OnError -= ShowError;
				step.OnInteractionEnded -= EndHeldError;
			}
			_flash = null;
			_holdingError = false;
		}

		private void ShowError()
		{
			if (!isActiveAndEnabled) return;
			if (_flash != null)
			{
				StopCoroutine(_flash);
				_flash = null;
			}

			if (step != null && step.HoldsInteraction)
			{
				_holdingError = true;
				SetColor(errorColor);
			}
			else
			{
				_flash = StartCoroutine(FlashRoutine());
			}
		}

		private void EndHeldError()
		{
			if (!_holdingError) return;
			_holdingError = false;
			Apply(step != null ? step.CurrentStatus : StepStatus.Pending);
		}

		private IEnumerator FlashRoutine()
		{
			SetColor(errorColor);
			yield return new WaitForSeconds(errorFlashSeconds);
			_flash = null;
			Apply(step != null ? step.CurrentStatus : StepStatus.Pending);
		}

		private void Apply(StepStatus status)
		{
			// Пока показывается ошибка, цвет статуса применится по ее окончании
			if (_flash != null || _holdingError) return;
			SetColor(status == StepStatus.Completed ? passedColor : notPassedColor);
		}

		private void SetColor(Color color)
		{
			if (targetRenderer == null) return;

			targetRenderer.GetPropertyBlock(_block);
			_block.SetColor(BaseColorProperty, color);
			_block.SetColor(ColorProperty, color);
			targetRenderer.SetPropertyBlock(_block);
		}
	}
}
