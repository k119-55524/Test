using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ScenarioSystem
{
	/// <summary>
	/// Базовый класс шага: тексты, статус, попытки, контур и цвет; режим прохождения работает только с ним.
	/// Наследник ловит свое событие Unity и сообщает о действии игрока.
	/// </summary>
	public abstract class StepBase : MonoBehaviour
	{
		private const string PlayerTag = "Player";

		[Header("Описание шага")]
		[Tooltip("Название задания")]
		[SerializeField] private string stepTitle = "Задание шага";

		[Tooltip("Как выполнить шаг")]
		[TextArea(2, 4)]
		[SerializeField] private string description = "Выполните требуемое действие.";

		[Tooltip("Подсказка при ошибке (обучение)")]
		[TextArea(2, 4)]
		[SerializeField] private string hintOnFailure = "Обратите внимание на последовательность и технику безопасности.";

		[Header("Действия вне очереди")]
		[Tooltip("UseScenarioDefault — как в ScenarioManager. RejectAsViolation — действие до очереди считается нарушением. IgnoreUntilActive — до очереди действия не принимаются.")]
		[SerializeField] private OutOfOrderOverride outOfOrderOverride = OutOfOrderOverride.UseScenarioDefault;

		[Header("Оформление")]
		[Tooltip("Материал контура активного шага (общий M_Outline)")]
		[SerializeField] private Material outlineMaterial;

		[Tooltip("Объекты, окрашиваемые по статусу шага: не пройден / пройден / ошибка")]
		[SerializeField] private Renderer[] statusRenderers;

		// Данные шага для режима и журнала
		public string StepId { get; private set; } = string.Empty;
		public string StepTitle => stepTitle;
		public string Description => description;
		public string HintOnFailure => hintOnFailure;
		public bool IsActive => CurrentStatus == StepStatus.Active;
		public StepStatus CurrentStatus { get; private set; } = StepStatus.Pending;
		public int AttemptCount { get; private set; }

		// true — ошибка горит, пока игрок в зоне или касается; false — вспышка
		protected virtual bool HoldsInteraction => false;

		private ScenarioModeBase _mode;
		private MeshRenderer[] _outlineRenderers;
		private MaterialPropertyBlock _colorBlock;
		private Coroutine _errorFlash;
		private bool _holdingError;

		protected virtual void Awake()
		{
			// Контур — только на рендерерах с обычным мешем (без текста)
			_outlineRenderers = System.Array.FindAll(GetComponentsInChildren<MeshRenderer>(true),
				renderer => renderer.GetComponent<MeshFilter>() != null);
			_colorBlock = new MaterialPropertyBlock();
			PaintStatus();
		}

		// Правило вне очереди: свое или как в менеджере
		public OutOfOrderPolicy ResolveOutOfOrderPolicy(OutOfOrderPolicy scenarioDefault) => outOfOrderOverride switch
		{
			OutOfOrderOverride.RejectAsViolation => OutOfOrderPolicy.RejectAsViolation,
			OutOfOrderOverride.IgnoreUntilActive => OutOfOrderPolicy.IgnoreUntilActive,
			_ => scenarioDefault
		};

		// Связь с режимом прохождения и id шага (step_01…)
		public void Bind(ScenarioModeBase mode, string stepId)
		{
			_mode = mode;
			StepId = stepId;
		}

		// Сброс перед прохождением и при его остановке
		public void ResetStep()
		{
			AttemptCount = 0;
			StopError();
			SetStatus(StepStatus.Pending);
			SetOutline(false);
			OnReset();
		}

		// Шаг вошел в очередь; контур — только в обучении
		public void ActivateStep(bool showOutline)
		{
			SetStatus(StepStatus.Active);
			SetOutline(showOutline);
		}

		// Шаг закрыт: Completed, Failed или Skipped
		public void Finish(StepStatus status)
		{
			SetStatus(status);
			SetOutline(false);
		}

		// Новая попытка, возвращает ее номер
		public int RegisterAttempt() => ++AttemptCount;

		// Ошибка или нарушение — красный
		public void NotifyError()
		{
			if (!isActiveAndEnabled)
				return;

			StopError();

			if (HoldsInteraction)
			{
				_holdingError = true;
				Paint(StepColors.Error);
			}
			else
				_errorFlash = StartCoroutine(ErrorFlash());
		}

		// Для наследника: сброс своего состояния
		protected virtual void OnReset() { }

		// Объект — игрок
		protected static bool IsPlayer(Component other) => other.CompareTag(PlayerTag);

		// Сообщить режиму о действии: успех или ошибка
		protected void ReportAction(bool isSuccess, string message)
		{
			if (_mode != null)
				_mode.ReportStepAction(this, isSuccess, message);
		}

		// Сообщить режиму о нарушении ТБ
		protected void ReportSafetyViolation(string message)
		{
			if (_mode != null)
				_mode.ReportSafetyViolation(this, message);
		}

		// Игрок вышел или отошел — красный гаснет
		protected void EndInteraction()
		{
			if (!_holdingError)
				return;

			_holdingError = false;
			PaintStatus();
		}

		// Новый статус и его цвет
		private void SetStatus(StepStatus status)
		{
			CurrentStatus = status;
			PaintStatus();
		}

		// Красная вспышка
		private IEnumerator ErrorFlash()
		{
			Paint(StepColors.Error);

			yield return new WaitForSeconds(StepColors.ErrorFlashSeconds);

			_errorFlash = null;
			PaintStatus();
		}

		// Снять показ ошибки
		private void StopError()
		{
			if (_errorFlash != null)
				StopCoroutine(_errorFlash);

			_errorFlash = null;
			_holdingError = false;
		}

		// Цвет по статусу (если сейчас не горит ошибка)
		private void PaintStatus()
		{
			if (_errorFlash != null || _holdingError)
				return;

			Paint(CurrentStatus == StepStatus.Completed ? StepColors.Passed : StepColors.NotPassed);
		}

		// Покрасить Status Renderers
		private void Paint(Color color)
		{
			foreach (var target in statusRenderers)
			{
				if (target == null)
					continue;

				target.GetPropertyBlock(_colorBlock);
				_colorBlock.SetColor("_BaseColor", color);
				_colorBlock.SetColor("_Color", color);
				target.SetPropertyBlock(_colorBlock);
			}
		}

		// Добавить или убрать материал контура
		private void SetOutline(bool visible)
		{
			if (outlineMaterial == null)
				return;

			foreach (var target in _outlineRenderers)
			{
				var materials = new List<Material>(target.sharedMaterials);
				materials.Remove(outlineMaterial);

				if (visible)
					materials.Add(outlineMaterial);

				target.sharedMaterials = materials.ToArray();
			}
		}
	}
}