using System;
using System.Linq;
using Base.Activatable;
using Base.WindowManager;
using Base.WindowManager.Template;
using DG.Tweening;
using Game.Model;
using Game.Signals;
using Helpers.TouchHelper;
using Sound;
using UniRx;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using User.Model;
using Zenject;

namespace Windows
{
	/// <summary>
	/// Interactive table window that lets user select idols directly in the table.
	/// See more about the windows here https://github.com/vcow/lib-window-manager
	/// </summary>
	[DisallowMultipleComponent, RequireComponent(typeof(RawImage))]
	public sealed class TableWindow : PopupWindowBase<DialogButtonType>
	{
		public const string Id = nameof(TableWindow);

		private readonly CompositeDisposable _handlers = new();

		private bool _isStarted;
		private int? _locker;
		private Vector3 _initialPosition, _initialScale, _startPosition, _startScale;
		private CanvasGroup _canvasGroup;
		private CanvasGroup _recordsCanvasGroup;
		private Tween _tween;
		private Color _initialBlendColor;
		private TableViewController _tableView;

		private const float InTweenDuration = 0.75f;
		private const float OutTweenDuration = 0.5f;

		// The view of additional table line for "Favor of the gods" option.
		[SerializeField] private GameObject _additionalBlock;

		[SerializeField, Header("Close button")]
		private Image _closeButtonView;

		[SerializeField] private Image _closeButtonShadowView;

		[SerializeField, Header("Table records")]
		private RectTransform _recordsContainer;

		[SerializeField] private TableWindowRecordViewController _recordPrefab;

		[Inject] private readonly IGameModel _gameModel;
		[Inject] private readonly IUserModel _userModel;
		[Inject] private readonly ISoundManager _soundManager;
		[Inject] private readonly DiContainer _container;
		[Inject] private readonly IWindowManager _windowManager;
		[Inject] private readonly SignalBus _signalBus;

		protected override string GetWindowId()
		{
			return Id;
		}

		private void Awake()
		{
			_canvasGroup = Popup.GetComponent<CanvasGroup>();
			Assert.IsTrue(_canvasGroup, "Popup CanvasGroup must have.");
		}

		private void Start()
		{
			_isStarted = true;

			SpawnRecords();

			_tableView = FindFirstObjectByType<TableViewController>();
			if (!_tableView)
			{
				Debug.LogError("Can't find TableView in the Scene.");
				return;
			}

			_additionalBlock.SetActive(_userModel.HasAdditionalGameStep);
			_initialBlendColor = Blend.color;

			var tableViewCanvas = _tableView.GetComponentInParent<Canvas>();
			Assert.IsTrue(tableViewCanvas, "Canvas must have.");
			var tableViewSize = ((RectTransform)_tableView.transform).sizeDelta * tableViewCanvas.scaleFactor;

			Assert.IsTrue(Popup, "Popup wasn't specified.");
			var popupSize = Popup.sizeDelta * Canvas.scaleFactor;
			var k = tableViewSize / popupSize;

			var cam = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
			if (cam)
			{
				// Calculate initial position for the table. That must be the same position as the table in the scene.
				var viewportStartPoint = cam.WorldToViewportPoint(_tableView.transform.position);
				_startPosition = Vector2.Scale(((RectTransform)Canvas.transform).sizeDelta, viewportStartPoint) *
				                 Canvas.scaleFactor;
			}
			else
			{
				Debug.LogError("Can't find Main Camera.");
			}

			_startScale = new Vector3(k.x, k.y, 1f);

			_initialPosition = Popup.position;
			_initialScale = Popup.localScale;
			Popup.position = _startPosition;
			Popup.localScale = _startScale;

			Assert.IsTrue(_closeButtonView && _closeButtonShadowView, "Close button view must have.");
			_closeButtonShadowView.gameObject.SetActive(false);
			_closeButtonView.gameObject.SetActive(false);

			if (this.IsActiveOrActivated())
			{
				PlayActivate();
			}
		}

		protected override void OnDestroy()
		{
			_handlers.Dispose();
			_tween?.Kill(true);

			if (_locker.HasValue)
			{
				TouchHelper.Unlock(_locker.Value);
				_locker = null;
			}

			base.OnDestroy();
		}

		private void SpawnRecords()
		{
			Assert.IsTrue(_recordsContainer && _recordPrefab, "The container and prefab for records must have.");
			var steps = _gameModel.History.Append(_gameModel.CurrentStep).ToArray();
			var activateImmediate = ActivatableState is ActivatableState.Active or ActivatableState.Inactive;
			foreach (var step in steps)
			{
				var record = _container.InstantiatePrefabForComponent<TableWindowRecordViewController>(_recordPrefab,
					_recordsContainer, new object[] { step });
				if (step == _gameModel.CurrentStep)
				{
					record.pointClickEvent.AddListener(OnPointClick);
				}

				if (this.IsActiveOrActivated())
				{
					record.Activate(activateImmediate);
				}
				else
				{
					record.Deactivate(activateImmediate);
				}
			}

			_recordsCanvasGroup = _recordsContainer.GetComponent<CanvasGroup>();
			Assert.IsTrue(_recordsCanvasGroup, "Records container must have CanvasGroup.");
		}

		private void OnPointClick(TableWindowPointViewController point)
		{
			var wnd = _windowManager.ShowWindow(TableWindowPointSelector.Id, new object[] { point });

			IDisposable handler = null;
			// Introduce the CloseWindowEvent as Observable and subscribe it.
			handler = Observable.FromEvent<Base.WindowManager.CloseWindowHandler, (IWindow sender, object result)>(
					h => (sender, result) => h((sender, result)),
					h => wnd.CloseWindowEvent += h, h => wnd.CloseWindowEvent -= h)
				.Subscribe(result =>
				{
					// ReSharper disable once AccessToModifiedClosure
					_handlers.Remove(handler);

					// Apply new selected color.
					_signalBus.TryFire(new SelectIdolColorSignal(point.IdolNumber, (IdolColor)result.result));
					_recordsCanvasGroup.DOFade(1f, 0.5f).SetEase(Ease.OutQuad).SetLink(_recordsContainer.gameObject);
				}).AddTo(_handlers);

			_soundManager.PlaySound("click6");
			_recordsCanvasGroup.DOFade(0.44f, 0.5f).SetEase(Ease.OutQuad).SetLink(_recordsContainer.gameObject);
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
				.Append(Blend.DOFade(0, OutTweenDuration).SetEase(Ease.Linear))
				.Join(Popup.DOMove(_startPosition, OutTweenDuration).SetEase(Ease.InQuad))
				.Join(Popup.DOScale(_startScale, OutTweenDuration).SetEase(Ease.InQuad))
				.Join(_closeButtonView.DOFade(0, OutTweenDuration * 0.75f).SetEase(Ease.Linear))
				.Join(_closeButtonShadowView.DOFade(0, OutTweenDuration * 0.75f).SetEase(Ease.Linear))
				.OnComplete(() =>
				{
					_tween = null;
					if (_locker.HasValue)
					{
						TouchHelper.Unlock(_locker.Value);
						_locker = null;
					}

					_closeButtonShadowView.gameObject.SetActive(false);
					_closeButtonView.gameObject.SetActive(false);

					if (_tableView)
					{
						_tableView.gameObject.SetActive(true);
					}

					ActivatableState = ActivatableState.Inactive;
				});

			_soundManager.PlaySound("wzuch1", 0.1f);

			foreach (Transform child in _recordsContainer)
			{
				var record = child.GetComponent<TableWindowRecordViewController>();
				if (!record)
				{
					continue;
				}

				record.Deactivate(immediately);
			}
		}

		private void PlayActivate()
		{
			_closeButtonShadowView.color = new Color(1f, 1f, 1f, 0f);
			_closeButtonView.transform.localScale = Vector3.one * 0.01f;
			Blend.color = Color.clear;

			_tween?.Kill();
			_tween = DOTween.Sequence()
				.Append(Blend.DOFade(_initialBlendColor.a, InTweenDuration).SetEase(Ease.Linear))
				.Join(Popup.DOMove(_initialPosition, InTweenDuration).SetEase(Ease.OutQuad))
				.Join(Popup.DOScale(_initialScale, InTweenDuration).SetEase(Ease.OutQuad))
				.OnComplete(() =>
				{
					_closeButtonShadowView.gameObject.SetActive(true);
					_closeButtonView.gameObject.SetActive(true);

					_tween = DOTween.Sequence()
						.Append(_closeButtonShadowView.DOFade(1, InTweenDuration * 0.5f).SetEase(Ease.OutQuad))
						.Join(_closeButtonView.transform.DOScale(Vector3.one, InTweenDuration * 0.5f)
							.SetEase(Ease.OutBack))
						.OnComplete(() => _tween = null);
					_soundManager.PlaySound("chpok1", 0.08f);

					_canvasGroup.interactable = true;
					ActivatableState = ActivatableState.Active;
				});

			_soundManager.PlaySound("wzuch1");

			foreach (Transform child in _recordsContainer)
			{
				var record = child.GetComponent<TableWindowRecordViewController>();
				if (!record)
				{
					continue;
				}

				record.Activate();
			}

			if (_tableView)
			{
				_tableView.gameObject.SetActive(false);
			}
		}

		/// <summary>
		/// Pray button handler.
		/// </summary>
		public void OnPray()
		{
			Close();

			foreach (Transform child in _recordsContainer)
			{
				var record = child.GetComponent<TableWindowRecordViewController>();
				if (!record)
				{
					continue;
				}

				record.Dispose();
			}

			_signalBus.TryFire<PraySignal>();
		}

		/// <summary>
		/// Close button handler.
		/// </summary>
		public void OnClose()
		{
			Close();
		}
	}
}