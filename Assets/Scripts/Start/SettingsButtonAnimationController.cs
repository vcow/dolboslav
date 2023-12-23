using Windows;
using Base.WindowManager;
using DG.Tweening;
using Start.Signals;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Zenject;

namespace Start
{
	/// <summary>
	/// The gear-like settings button controller.
	/// </summary>
	[DisallowMultipleComponent, RequireComponent(typeof(Button))]
	public sealed class SettingsButtonAnimationController : MonoBehaviour
	{
		private Button _button;
		private Tween _tween;

		private const float AnimationDuration = 1f;

		[SerializeField] private RectTransform _icon;
		[SerializeField] private RectTransform _iconShadow;

		[Inject] private readonly IWindowManager _windowManager;
		[Inject] private readonly SignalBus _signalBus;

		void Start()
		{
			_button = GetComponent<Button>();
			Assert.IsTrue(_button && _icon, "Toggle and icon must have.");

			_button.onClick.AddListener(OnButtonClick);
			_signalBus.Subscribe<SettingsWindowCloseSignal>(OnButtonClick);
		}

		private void OnButtonClick()
		{
			// Rotate back and forth depending on the window state.
			var ang = _windowManager.GetWindow(SettingsWindow.Id) == null ? -360f : 360f;
			_tween?.Kill();
			var seq = DOTween.Sequence()
				.Append(_icon.DOLocalRotate(new Vector3(0f, 0f, ang), AnimationDuration, RotateMode.FastBeyond360)
					.SetEase(Ease.InOutQuad));
			if (_iconShadow)
			{
				seq.Join(_iconShadow
					.DOLocalRotate(new Vector3(0f, 0f, ang), AnimationDuration, RotateMode.FastBeyond360)
					.SetEase(Ease.InOutQuad));
			}

			seq.OnComplete(() => _tween = null);
			_tween = seq;
		}

		private void OnDestroy()
		{
			_tween?.Kill(true);

			_button.onClick.RemoveListener(OnButtonClick);
			_button = null;

			_signalBus.Unsubscribe<SettingsWindowCloseSignal>(OnButtonClick);
		}
	}
}