using Base.Activatable;
using Base.WindowManager.Extensions.ScreenLockerExtension;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;

namespace ScreenLocker
{
	/// <summary>
	/// Screen locker to lock the screen during the game is initialized in preloader scene.
	/// (see https://github.com/vcow/lib-window-manager#how-to-use-screenlockermanager for details)
	/// </summary>
	[RequireComponent(typeof(CanvasGroup))]
	public sealed class GameScreenLocker : ScreenLocker<GameScreenLocker>
	{
		[SerializeField] private RectTransform _icon;

		private bool _isStarted;
		private CanvasGroup _canvasGroup;
		private float _alpha;

		public override LockerType LockerType => LockerType.GameLoader;

		private void Awake()
		{
			_canvasGroup = GetComponent<CanvasGroup>();
			_canvasGroup.interactable = false;
		}

		protected override void Start()
		{
			base.Start();

			_isStarted = true;
			_canvasGroup.alpha = _alpha;
			ValidateState();

			_icon.DOLocalRotate(new Vector3(0f, 0f, -360f), 2f, RotateMode.FastBeyond360)
				.SetEase(Ease.Linear).SetLoops(-1).SetLink(_icon.gameObject);
		}

		public override void Activate(bool immediately = false)
		{
			Assert.IsFalse(this.IsActiveOrActivated());
			ActivatableState = ActivatableState.Active;
			if (!ValidateState())
			{
				_alpha = 1;
			}
		}

		public override void Deactivate(bool immediately = false)
		{
			Assert.IsFalse(this.IsInactiveOrDeactivated());
			ActivatableState = immediately ? ActivatableState.Inactive : ActivatableState.ToInactive;
			if (!ValidateState() && immediately)
			{
				_alpha = 0;
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

		private bool ValidateState()
		{
			if (!_isStarted) return false;

			DOTween.Kill(_canvasGroup);

			switch (ActivatableState)
			{
				case ActivatableState.Active:
					_canvasGroup.alpha = 1;
					break;
				case ActivatableState.Inactive:
					_canvasGroup.alpha = 0;
					break;
				case ActivatableState.ToActive:
					_canvasGroup.DOFade(1, 1).OnComplete(() =>
					{
						_canvasGroup.interactable = true;
						ActivatableState = ActivatableState.Active;
					}).SetLink(_canvasGroup.gameObject);
					break;
				case ActivatableState.ToInactive:
					_canvasGroup.interactable = false;
					_canvasGroup.DOFade(0, 1).OnComplete(() => { ActivatableState = ActivatableState.Inactive; })
						.SetLink(_canvasGroup.gameObject);
					break;
			}

			return true;
		}
	}
}