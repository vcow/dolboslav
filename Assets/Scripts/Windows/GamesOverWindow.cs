using System;
using System.Collections;
using Base.Activatable;
using Base.Localization;
using Base.WindowManager.Template;
using DG.Tweening;
using Sound;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using User;
using Zenject;

namespace Windows
{
	/// <summary>
	/// Controller for the window that informs that there are no more games.
	/// See more about the windows here https://github.com/vcow/lib-window-manager
	/// </summary>
	public class GamesOverWindow : PopupWindowBase<bool>, IPointerDownHandler, IPointerUpHandler
	{
		public const string Id = nameof(GamesOverWindow);

		private readonly CompositeDisposable _handlers = new();
		private CanvasGroup _canvasGroup;

		private bool _isStarted;
		private Tween _tween;
		private Vector2 _initialWindowPosition, _startPosition, _finishPosition;
		private Color _initialBlendColor;
		private LocalString _timerString;

		private const float InTweenDuration = 1f;
		private const float OutTweenDuration = 1f;

		private const float ClickTolerance = 10f;

		private Vector2? _blendButtonDown;

		private float _delayTimeSec;

		[Inject] private readonly ISoundManager _soundManager;
		[Inject] private readonly ILocalizationManager _localizationManager;
		[Inject] private readonly UserModelController _userModelController;
		[Inject] private readonly GameConfig _gameConfig;

		[SerializeField] private TextMeshProUGUI _timerText;
		[SerializeField] private string _timerPhraseKey;

		private void Awake()
		{
			Assert.IsTrue(Popup, "Popup must have.");
			_canvasGroup = Popup.GetComponent<CanvasGroup>();
			Assert.IsTrue(_canvasGroup, "Popup CanvasGroup must have.");
		}

		private void Start()
		{
			_isStarted = true;

			_initialWindowPosition = Popup.anchoredPosition;

			var blend = Blend;
			Assert.IsTrue(blend, "Blend RawImage must have.");
			_initialBlendColor = blend.color;

			var canvasTransformSize = ((RectTransform)Canvas.transform).sizeDelta;
			var popupSize = Popup.sizeDelta;
			_startPosition = new Vector2(_initialWindowPosition.x,
				(popupSize.y + canvasTransformSize.y - _initialWindowPosition.y) * 0.5f);
			_finishPosition = new Vector2(_initialWindowPosition.x,
				(-_initialWindowPosition.y - popupSize.y - canvasTransformSize.y) * 0.5f);

			Popup.anchoredPosition = _startPosition;
			blend.color = Color.clear;
			_canvasGroup.interactable = false;

			if (this.IsActiveOrActivated())
			{
				PlayActivate();
			}

			Assert.IsFalse(string.IsNullOrEmpty(_timerPhraseKey), "The key for timer string must have.");
			_timerString = new LocalString(_localizationManager, _timerPhraseKey);
			_handlers.Add(_timerString);

			StartCoroutine(TimerTextRoutine(_delayTimeSec));
		}

		protected override void OnDestroy()
		{
			StopAllCoroutines();

			_handlers.Dispose();
			_tween?.Kill(true);

			base.OnDestroy();
		}

		private IEnumerator TimerTextRoutine(float delayTime)
		{
			Assert.IsNotNull(_timerString, "Timer string must have.");
			UpdateTimerText();

			while (delayTime > 0f)
			{
				yield return new WaitForSeconds(1);
				delayTime -= 1f;
				UpdateTimerText();
			}

			Result = true;

			if (this.IsActive())
			{
				Close();
			}

			yield break;

			void UpdateTimerText()
			{
				var m = Mathf.FloorToInt(delayTime / 60f);
				var s = Mathf.CeilToInt(delayTime - m * 60f);
				_timerString.FormatArgs = new object[] { m, s };
				_timerText.text = _timerString.ToString();
			}
		}

		protected override string GetWindowId()
		{
			return Id;
		}

		protected override void DoSetArgs(object[] args)
		{
			foreach (var arg in args)
			{
				switch (arg)
				{
					case float delayTimeSec:
						_delayTimeSec = delayTimeSec;
						break;
					default:
						throw new NotSupportedException();
				}
			}
		}

		protected override void DoActivate(bool immediately)
		{
			if (this.IsActiveOrActivated())
			{
				return;
			}

			ActivatableState = ActivatableState.ToActive;

			if (_isStarted)
			{
				PlayActivate();
			}
		}

		protected override void DoDeactivate(bool immediately)
		{
			if (this.IsInactiveOrDeactivated() || !_isStarted)
			{
				return;
			}

			_canvasGroup.interactable = false;
			ActivatableState = ActivatableState.ToInactive;

			_tween?.Kill(true);
			_tween = DOTween.Sequence()
				.Append(Blend.DOFade(0, OutTweenDuration * 0.5f).SetEase(Ease.Linear))
				.Join(Popup.DOAnchorPos(_finishPosition, OutTweenDuration).SetEase(Ease.OutQuad))
				.Join(DOTween.To(() => 1f, v => _soundManager.SuppressMusic(v), 0f, OutTweenDuration)
					.SetEase(Ease.InQuad))
				.OnComplete(() =>
				{
					_tween = null;
					ActivatableState = ActivatableState.Inactive;
				});

			_soundManager.PlaySound("wzuch1", 0.01f);
		}

		void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
		{
			if (!this.IsActive())
			{
				return;
			}

			if (eventData.pointerCurrentRaycast.gameObject == gameObject)
			{
				_blendButtonDown = eventData.pointerCurrentRaycast.screenPosition / Canvas.scaleFactor;
			}
			else
			{
				_blendButtonDown = null;
			}
		}

		void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
		{
			if (!_blendButtonDown.HasValue)
			{
				return;
			}

			var screenPosition = eventData.pointerCurrentRaycast.screenPosition / Canvas.scaleFactor;
			if ((screenPosition - _blendButtonDown.Value).sqrMagnitude <= ClickTolerance * ClickTolerance)
			{
				Close();
			}

			_blendButtonDown = null;
		}

		private void PlayActivate()
		{
			_tween?.Kill();
			_tween = DOTween.Sequence()
				.Append(Blend.DOFade(_initialBlendColor.a, InTweenDuration * 0.5f).SetEase(Ease.Linear))
				.Join(Popup.DOAnchorPos(_initialWindowPosition, InTweenDuration).SetEase(Ease.OutBack))
				.Join(DOTween.To(() => 0f, v => _soundManager.SuppressMusic(v), 1f, InTweenDuration)
					.SetEase(Ease.OutQuad))
				.OnComplete(() =>
				{
					_tween = null;
					_canvasGroup.interactable = true;
					ActivatableState = ActivatableState.Active;

					if (Result)
					{
						// New game has spawned when the window is activated.
						Close();
					}
				});

			_soundManager.PlaySound("wzuch1", 0.25f);
		}

		public void OnClose()
		{
			Close();
		}

		public void OnGoToShop()
		{
			// TODO: Go to Shop
			_userModelController.AddNumBonusGames(1);
			Result = true;
			Close();
		}

		public void OnViewAd()
		{
			// TODO: View AD
			_userModelController.AddNumBonusGames(1);
			Result = true;
			Close();
		}
	}
}