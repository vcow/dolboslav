using System;
using System.Collections.Generic;
using Base.Activatable;
using Base.WindowManager.Template;
using DG.Tweening;
using Game.Model;
using Sound;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace Windows
{
	/// <summary>
	/// This is the window that appear in front of the TableWindow and allows the user to select new
	/// color for a point in the table.
	/// See more about the windows here https://github.com/vcow/lib-window-manager
	/// </summary>
	[RequireComponent(typeof(RawImage))]
	public sealed class TableWindowPointSelector : PopupWindowBase<IdolColor>, IPointerDownHandler, IPointerUpHandler
	{
		public const string Id = nameof(TableWindowPointSelector);

		private const float ActivateDuration = 0.5f;
		private const float DeactivateDuration = 0.5f;

		private bool _isStarted;
		private TableWindowPointViewController _initialPoint;
		private CanvasGroup _canvasGroup;

		private readonly Dictionary<Transform, Vector3> _pointsInitialPosition = new();
		private Vector3 _initialPointPosition;
		private Color _initialBlendColor;
		private Tween _tween;

		private Vector2? _blendButtonDown;
		private const float ClickTolerance = 10f;

		[SerializeField] private List<TableWindowPointViewController> _points;
		[SerializeField] private Image _popupBackground;

		[Inject] private readonly ISoundManager _soundManager;

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
					case TableWindowPointViewController initialPoint:
						_initialPoint = initialPoint;
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

			ActivatableState = ActivatableState.ToInactive;
			_tween?.Kill();

			_canvasGroup.interactable = false;

			var seq = DOTween.Sequence()
				.Append(Blend.DOFade(0, DeactivateDuration).SetEase(Ease.Linear))
				.Join(_popupBackground.transform.DOScale(Vector3.one * 0.01f, DeactivateDuration).SetEase(Ease.InQuad));

			foreach (var point in _points)
			{
				seq.Join(point.transform.DOMove(_initialPointPosition, DeactivateDuration).SetEase(Ease.InQuad));

				if (point.Color != Result)
				{
					point.Deactivate();
				}
			}

			seq.OnComplete(() =>
			{
				_tween = null;
				_initialPoint.Activate(true);
				ActivatableState = ActivatableState.Inactive;
			});

			_soundManager.PlaySound("wzuch1");
		}

		public void OnPointClick(TableWindowPointViewController point)
		{
			Result = point.Color;
			Close();
		}

		private void Start()
		{
			Assert.IsTrue(_initialPoint,
				"This window have to receive initial TableWindowPointViewController point in the args.");
			Result = _initialPoint.Color;

			_isStarted = true;

			Assert.IsTrue(Popup, "Popup wasn't specified.");
			_canvasGroup = Popup.GetComponent<CanvasGroup>();
			Assert.IsTrue(_canvasGroup, "Popup must have CanvasGroup.");
			var blend = Blend;
			Assert.IsTrue(blend, "Blend RawImage must have.");
			_initialBlendColor = blend.color;

			var initialPointTransform = (RectTransform)_initialPoint.transform;
			var corners = new Vector3[4];
			initialPointTransform.GetWorldCorners(corners);
			var initialPointBounds = Corners2Bounds(corners);

			Popup.GetWorldCorners(corners);
			var popupBounds = Corners2Bounds(corners);

			var safeFrameTransform = (RectTransform)Popup.parent;
			safeFrameTransform.GetWorldCorners(corners);
			var safeAreaBounds = Corners2Bounds(corners);

			_initialPointPosition = initialPointBounds.center;
			popupBounds.center = _initialPointPosition;
			if (!safeAreaBounds.Contains(popupBounds.min))
			{
				var delta = safeAreaBounds.min - popupBounds.min;
				popupBounds.center += new Vector3(Mathf.Max(0f, delta.x), Mathf.Max(0f, delta.y), 0f);
			}

			if (safeAreaBounds.Contains(popupBounds.max))
			{
				var delta = safeAreaBounds.max - popupBounds.max;
				popupBounds.center += new Vector3(Mathf.Min(0f, delta.x), Mathf.Min(0f, delta.y), 0f);
			}

			Popup.position = popupBounds.center;

			foreach (var point in _points)
			{
				var t = point.transform;
				_pointsInitialPosition.Add(t, t.position);
			}

			if (this.IsActiveOrActivated())
			{
				PlayActivate();
			}
		}

		protected override void OnDestroy()
		{
			_tween?.Kill();
			_tween = null;

			_pointsInitialPosition.Clear();
			base.OnDestroy();
		}

		private void PlayActivate()
		{
			_tween?.Kill();

			Blend.color = Color.clear;
			var popupBackTransform = _popupBackground.transform;
			popupBackTransform.localScale = Vector3.one * 0.01f;
			_canvasGroup.interactable = false;

			var seq = DOTween.Sequence()
				.Append(Blend.DOFade(_initialBlendColor.a, ActivateDuration).SetEase(Ease.Linear))
				.Join(popupBackTransform.DOScale(Vector3.one, ActivateDuration).SetEase(Ease.OutQuad));

			foreach (var point in _points)
			{
				var t = point.transform;
				var dest = _pointsInitialPosition[t];
				t.position = _initialPointPosition;
				seq.Join(t.DOMove(dest, ActivateDuration).SetEase(Ease.OutBack));

				if (point.Color == _initialPoint.Color)
				{
					point.Activate(true);
					t.SetAsLastSibling();
				}
				else
				{
					point.Activate();
				}
			}

			_initialPoint.Deactivate(true);

			seq.OnComplete(() =>
			{
				_tween = null;
				_canvasGroup.interactable = true;
				ActivatableState = ActivatableState.Active;
			});
		}

		private static Bounds Corners2Bounds(Vector3[] corners)
		{
			Assert.IsTrue(corners.Length == 4);
			return new Bounds
			{
				min = corners[0],
				max = corners[2]
			};
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
	}
}