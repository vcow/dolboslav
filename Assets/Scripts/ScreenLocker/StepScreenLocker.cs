using System.Collections;
using System.Linq;
using Base.Activatable;
using Base.WindowManager.Extensions.ScreenLockerExtension;
using DG.Tweening;
using Game.Model;
using Sound;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using Zenject;

namespace ScreenLocker
{
	/// <summary>
	/// Screen locker to lock the screen between the steps.
	/// (see https://github.com/vcow/lib-window-manager#how-to-use-screenlockermanager for details)
	/// </summary>
	[RequireComponent(typeof(Canvas))]
	public class StepScreenLocker : ScreenLocker<StepScreenLocker>
	{
		private const float ActivateDuration = 0.75f;
		private const float DeactivateDuration = 0.75f;

		private bool _isStarted;
		private Vector2 _initialViewPosition, _startViewPosition, _endViewPosition;
		private float? _activateTimestamp;

		[SerializeField] private RectTransform _lockerView;
		[SerializeField] private TextMeshProUGUI _guessedCount;
		[SerializeField] private TextMeshProUGUI _correctCount;
		[SerializeField, Header("Settings")] private float _lockerDelayTimeSec = 3f;
		[SerializeField, Header("Views")] private GameObject _stepView;
		[SerializeField] private GameObject _firstStepView;

		[SerializeField, Header("First step icons")]
		private Transform _topLeftIcon;

		[SerializeField] private Transform _bottomLeftIcon;
		[SerializeField] private Transform _topRightIcon;
		[SerializeField] private Transform _bottomRightIcon;

		[InjectOptional] private readonly IGameModel _gameModel;
		[Inject] private readonly ISoundManager _soundManager;

		public override LockerType LockerType => LockerType.SceneLoader;

		public override void Activate(bool immediately = false)
		{
			StopAllCoroutines();

			Assert.IsFalse(this.IsActiveOrActivated());
			ActivatableState = immediately ? ActivatableState.Active : ActivatableState.ToActive;
			ValidateState();
		}

		public override void Deactivate(bool immediately = false)
		{
			Assert.IsFalse(this.IsInactiveOrDeactivated());
			ActivatableState = immediately ? ActivatableState.Inactive : ActivatableState.ToInactive;

			Assert.IsTrue(_activateTimestamp.HasValue);

			StopAllCoroutines();

			var dt = Time.time - _activateTimestamp.Value;
			if (dt >= _lockerDelayTimeSec)
			{
				ValidateState();
			}
			else
			{
				StartCoroutine(DelayedValidateRoutine(_lockerDelayTimeSec - dt));
			}
		}

		public override bool Force()
		{
			switch (ActivatableState)
			{
				case ActivatableState.ToActive:
					ActivatableState = ActivatableState.Active;
					ValidateState();
					break;
				case ActivatableState.ToInactive:
					ActivatableState = ActivatableState.Inactive;
					ValidateState();
					break;
				default:
					return false;
			}

			return true;
		}

		protected override void Start()
		{
			Assert.IsTrue(_guessedCount && _correctCount, "Guessed and correct text items must have.");
			Assert.IsTrue(_stepView && _firstStepView, "Views must have.");
			if (_gameModel != null && _gameModel.History.Any())
			{
				_stepView.SetActive(true);
				_firstStepView.SetActive(false);

				var result = _gameModel.History.Last().Result;
				if (result.HasValue)
				{
					var (guess, correct) = result.Value;
					_guessedCount.text = (guess + correct).ToString();
					_correctCount.text = correct.ToString();
				}
				else
				{
					Debug.LogError("Last step must be finished.");
					_guessedCount.text = "-1";
					_correctCount.text = "-1";
				}
			}
			else
			{
				Assert.IsTrue(_topLeftIcon && _bottomLeftIcon && _topRightIcon && _bottomRightIcon,
					"Corner icons must have");
				_stepView.SetActive(false);
				_firstStepView.SetActive(true);
			}

			var canvas = GetComponent<Canvas>();
			var canvasSize = ((RectTransform)canvas.transform).sizeDelta;
			var lockerSize = _lockerView.sizeDelta;
			_initialViewPosition = _lockerView.anchoredPosition;
			_startViewPosition = new Vector2(
				_initialViewPosition.x,
				lockerSize.y * 0.5f + canvasSize.y * 0.5f);
			_endViewPosition = new Vector2(
				_initialViewPosition.x,
				-lockerSize.y * 0.5f - canvasSize.y * 0.5f);
			_lockerView.anchoredPosition = _startViewPosition;

			base.Start();

			_isStarted = true;
			ValidateState();
		}

		protected override void OnDestroy()
		{
			StopAllCoroutines();
			base.OnDestroy();
		}

		private IEnumerator DelayedValidateRoutine(float delayTime)
		{
			yield return new WaitForSeconds(delayTime);
			ValidateState();
		}

		private void ValidateState()
		{
			if (!_isStarted) return;

			DOTween.Kill(_lockerView);

			switch (ActivatableState)
			{
				case ActivatableState.Active:
					_activateTimestamp = Time.time;
					_lockerView.anchoredPosition = _initialViewPosition;
					break;
				case ActivatableState.Inactive:
					_activateTimestamp = null;
					_lockerView.anchoredPosition = _endViewPosition;
					break;
				case ActivatableState.ToActive:
					_lockerView.DOAnchorPos(_initialViewPosition, ActivateDuration).SetEase(Ease.OutQuad)
						.SetLink(_lockerView.gameObject).OnComplete(() =>
						{
							ActivatableState = ActivatableState.Active;
							_activateTimestamp = Time.time;
						});
					if (_topLeftIcon && _bottomLeftIcon && _topRightIcon && _bottomRightIcon)
					{
						DOTween.Sequence()
							.Append(_topLeftIcon
								.DOLocalRotate(Vector3.forward * 360f, ActivateDuration, RotateMode.FastBeyond360)
								.SetEase(Ease.OutQuad))
							.Join(_bottomLeftIcon
								.DOLocalRotate(Vector3.forward * 360f, ActivateDuration, RotateMode.FastBeyond360)
								.SetEase(Ease.OutQuad))
							.Join(_topRightIcon
								.DOLocalRotate(Vector3.back * 360f, ActivateDuration, RotateMode.FastBeyond360)
								.SetEase(Ease.OutQuad))
							.Join(_bottomRightIcon
								.DOLocalRotate(Vector3.back * 360f, ActivateDuration, RotateMode.FastBeyond360)
								.SetEase(Ease.OutQuad))
							.SetLink(gameObject);
					}

					_soundManager.PlaySound("rolling1");
					break;
				case ActivatableState.ToInactive:
					_activateTimestamp = null;
					_lockerView.DOAnchorPos(_endViewPosition, DeactivateDuration).SetEase(Ease.InQuad)
						.SetLink(_lockerView.gameObject).OnComplete(() =>
						{
							ActivatableState = ActivatableState.Inactive;
						});
					if (_topLeftIcon && _bottomLeftIcon && _topRightIcon && _bottomRightIcon)
					{
						DOTween.Sequence()
							.Append(_topLeftIcon
								.DOLocalRotate(Vector3.forward * 360f, ActivateDuration, RotateMode.FastBeyond360)
								.SetEase(Ease.InQuad))
							.Join(_bottomLeftIcon
								.DOLocalRotate(Vector3.forward * 360f, ActivateDuration, RotateMode.FastBeyond360)
								.SetEase(Ease.InQuad))
							.Join(_topRightIcon
								.DOLocalRotate(Vector3.back * 360f, ActivateDuration, RotateMode.FastBeyond360)
								.SetEase(Ease.InQuad))
							.Join(_bottomRightIcon
								.DOLocalRotate(Vector3.back * 360f, ActivateDuration, RotateMode.FastBeyond360)
								.SetEase(Ease.InQuad))
							.SetLink(gameObject);
					}

					_soundManager.PlaySound("rolling1");
					break;
			}
		}
	}
}