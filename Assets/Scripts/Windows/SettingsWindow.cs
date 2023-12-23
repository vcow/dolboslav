using System.Collections;
using Base.Activatable;
using Base.Localization;
using Base.WindowManager.Template;
using DG.Tweening;
using Helpers.TouchHelper;
using Sound;
using Start.Signals;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace Windows
{
	/// <summary>
	/// Settings window controller.
	/// See more about the windows here https://github.com/vcow/lib-window-manager
	/// </summary>
	[RequireComponent(typeof(RawImage))]
	public sealed class SettingsWindow : PopupWindowBase<DialogButtonType>, IPointerDownHandler, IPointerUpHandler
	{
		public const string Id = nameof(SettingsWindow);

		private CanvasGroup _canvasGroup;

		private bool _isStarted;
		private int? _locker;
		private Vector2 _initialWindowPosition;
		private Color _initialBlendColor;

		private const float InTweenDuration = 1f;
		private const float OutTweenDuration = 1f;

		private const float ClickTolerance = 10f;
		private const float AlphaTolerance = 0.7f;

		private Tween _tween;
		private Vector2? _blendButtonDown;

		[SerializeField] private Image _backgroundImage;

		[SerializeField, Header("Sound settings")]
		private Toggle _soundOnToggle;

		[SerializeField] private Toggle _musicOnToggle;

		[SerializeField, Header("Language settings")]
		private Toggle _enToggle;

		[SerializeField] private Toggle _deToggle;
		[SerializeField] private Toggle _ruToggle;

		[Inject] private readonly SignalBus _signalBus;
		[Inject] private readonly ISoundManager _soundManager;
		[Inject] private readonly ILocalizationManager _localizationManager;

		private static readonly int WindowIsClosedHash = Animator.StringToHash("WindowIsClosed");

		protected override string GetWindowId()
		{
			return Id;
		}

		private void Awake()
		{
			Assert.IsTrue(Popup, "Popup must have.");
			_canvasGroup = Popup.GetComponent<CanvasGroup>();
			Assert.IsTrue(_canvasGroup, "Popup CanvasGroup must have.");
		}

		private void Start()
		{
			_isStarted = true;

			var blend = Blend;
			Assert.IsTrue(blend, "Blend RawImage must have.");
			_initialWindowPosition = Vector2.Scale(Popup.sizeDelta + ((RectTransform)Canvas.transform).sizeDelta,
				new Vector2(0f, 0.5f));
			_initialBlendColor = blend.color;

			Popup.anchoredPosition = _initialWindowPosition;
			blend.color = Color.clear;
			_canvasGroup.interactable = false;

			_signalBus.Subscribe<CloseButtonDownSignal>(OnCloseButtonDown);
			_signalBus.Subscribe<CloseButtonUpSignal>(OnCloseButtonUp);

			Assert.IsTrue(_soundOnToggle && _musicOnToggle, "Sound toggle and Music toggle must have.");
			_soundOnToggle.isOn = _soundManager.SoundIsOn;
			_musicOnToggle.isOn = _soundManager.MusicIsOn;

			Assert.IsTrue(_enToggle && _deToggle && _ruToggle, "Language toggles must have.");
			switch (_localizationManager.CurrentLanguage)
			{
				case SystemLanguage.English:
					_enToggle.isOn = true;
					break;
				case SystemLanguage.German:
					_deToggle.isOn = true;
					break;
				case SystemLanguage.Russian:
					_ruToggle.isOn = true;
					break;
				default:
					Debug.LogErrorFormat("Hasn't toggle for the language {0}.", _localizationManager.CurrentLanguage);
					break;
			}

			if (this.IsActiveOrActivated())
			{
				PlayActivate();
			}
		}

		private void OnCloseButtonDown()
		{
			var bugController = Popup.GetComponentInChildren<UiBugController>();
			if (bugController)
			{
				bugController.IsPressed = true;
			}
		}

		private void OnCloseButtonUp()
		{
			var bugController = Popup.GetComponentInChildren<UiBugController>();
			if (bugController)
			{
				bugController.IsPressed = false;
			}
		}

		protected override void OnDestroy()
		{
			_tween?.Kill(true);
			if (_locker.HasValue)
			{
				TouchHelper.Unlock(_locker.Value);
				_locker = null;
			}

			_signalBus.TryUnsubscribe<CloseButtonDownSignal>(OnCloseButtonDown);
			_signalBus.TryUnsubscribe<CloseButtonUpSignal>(OnCloseButtonUp);

			StopAllCoroutines();

			base.OnDestroy();
		}

		protected override void DoSetArgs(object[] args)
		{
		}

		protected override void DoActivate(bool immediately)
		{
			if (this.IsActiveOrActivated())
			{
				return;
			}

			_locker ??= TouchHelper.Lock();
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
				.Join(Popup.DOAnchorPosY(_initialWindowPosition.y, OutTweenDuration).SetEase(Ease.InOutQuad))
				.Join(DOTween.To(() => 1f, v => _soundManager.SuppressMusic(v), 0f, OutTweenDuration)
					.SetEase(Ease.InQuad))
				.OnComplete(() =>
				{
					_tween = null;
					if (_locker.HasValue)
					{
						TouchHelper.Unlock(_locker.Value);
						_locker = null;
					}

					ActivatableState = ActivatableState.Inactive;
				});

			_soundManager.PlaySound("rolling1");
		}

		private void PlayActivate()
		{
			_tween?.Kill();
			_tween = DOTween.Sequence()
				.Append(Blend.DOFade(_initialBlendColor.a, InTweenDuration * 0.5f).SetEase(Ease.Linear))
				.Join(Popup.DOAnchorPosY(0, InTweenDuration).SetEase(Ease.InOutQuad))
				.Join(DOTween.To(() => 0f, v => _soundManager.SuppressMusic(v), 1f, InTweenDuration)
					.SetEase(Ease.OutQuad))
				.OnComplete(() =>
				{
					_tween = null;
					_canvasGroup.interactable = true;
					ActivatableState = ActivatableState.Active;
				});

			_soundManager.PlaySound("rolling1");
		}

		void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
		{
			if (!this.IsActive())
			{
				return;
			}

			if (eventData.pointerCurrentRaycast.gameObject != gameObject && _backgroundImage)
			{
				if (eventData.pointerCurrentRaycast.gameObject == _backgroundImage.gameObject)
				{
					RectTransformUtility.ScreenPointToLocalPointInRectangle(_backgroundImage.rectTransform,
						eventData.pointerCurrentRaycast.screenPosition, null, out var localPoint);
					var r = _backgroundImage.rectTransform.rect;
					if (r.Contains(localPoint))
					{
						var color = _backgroundImage.sprite.texture.GetPixel(Mathf.RoundToInt(localPoint.x - r.x),
							Mathf.RoundToInt(localPoint.y - r.y));
						if (color.a <= AlphaTolerance)
						{
							_blendButtonDown = eventData.pointerCurrentRaycast.screenPosition / Canvas.scaleFactor;
							return;
						}
					}
				}

				_blendButtonDown = null;
			}
			else
			{
				_blendButtonDown = eventData.pointerCurrentRaycast.screenPosition / Canvas.scaleFactor;
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

		public override bool Close(bool immediately = false)
		{
			if (base.Close(immediately))
			{
				_signalBus.TryFire<SettingsWindowCloseSignal>();
				return true;
			}

			return false;
		}

		public void OnClose()
		{
			var animator = Popup.GetComponent<Animator>();
			if (animator)
			{
				animator.SetTrigger(WindowIsClosedHash);
				StartCoroutine(DelayedClose(1f));
			}
			else
			{
				Close();
			}
		}

		private IEnumerator DelayedClose(float delaySec)
		{
			_canvasGroup.interactable = false;

			if (delaySec > 0)
			{
				yield return new WaitForSeconds(delaySec);
			}

			if (!this.IsInactiveOrDeactivated())
			{
				Close();
			}
		}

		public void OnPlaySound(bool value)
		{
			_soundManager.SoundIsOn = value;
		}

		public void OnPlayMusic(bool value)
		{
			_soundManager.MusicIsOn = value;
		}

		public void OnSetLanguage(Toggle toggle)
		{
			if (toggle == _enToggle)
			{
				_localizationManager.SetCurrentLanguage(SystemLanguage.English);
			}
			else if (toggle == _deToggle)
			{
				_localizationManager.SetCurrentLanguage(SystemLanguage.German);
			}
			else if (toggle == _ruToggle)
			{
				_localizationManager.SetCurrentLanguage(SystemLanguage.Russian);
			}
			else
			{
				Debug.LogError("Unexpected language toggle.");
			}
		}
	}
}