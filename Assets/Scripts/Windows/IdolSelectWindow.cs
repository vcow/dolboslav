using System;
using System.Collections.Generic;
using System.Linq;
using Base.Activatable;
using Base.WindowManager.Template;
using DG.Tweening;
using Game.Model;
using Helpers.TouchHelper;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Windows
{
	/// <summary>
	/// The window where user can select the idol in the scene.
	/// See more about the windows here https://github.com/vcow/lib-window-manager
	/// </summary>
	[DisallowMultipleComponent, RequireComponent(typeof(RawImage))]
	public sealed class IdolSelectWindow : PopupWindowBase<IdolColor?>
	{
		public const string Id = nameof(IdolSelectWindow);

		private const float AppearDuration = 0.65f;
		private const float DisappearDuration = 0.5f;

		private IdolColor? _targetColor;
		private Vector3? _initialPosition;

		private int? _lockKey;
		private Tween _tween;
		private CanvasGroup _popupCanvasGroup;
		private float _blendInitialAlpha;

		[SerializeField] private List<IdolListItemViewController> _items;

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
					case IdolColor targetColor:
						_targetColor = targetColor;
						break;
					case Vector3 initialPosition:
						_initialPosition = initialPosition;
						break;
					default:
						throw new NotSupportedException();
				}
			}
		}

		private void Awake()
		{
			_popupCanvasGroup = Popup.GetComponent<CanvasGroup>();
			Assert.IsTrue(_popupCanvasGroup, "Popup must have CanvasGroup to lock UI on transitions.");

			var blend = Blend;
			Assert.IsTrue(blend, "Blend RawImage must have.");
			_blendInitialAlpha = blend.color.a;
		}

		private void Start()
		{
			if (!_lockKey.HasValue)
			{
				_lockKey = TouchHelper.Lock();
			}

			if (_targetColor is null or IdolColor.Undefined)
			{
				return;
			}

			var item = _items.FirstOrDefault(i => i.Color == _targetColor);
			if (item)
			{
				item.Color = IdolColor.Undefined;
			}

			if (this.IsInactive())
			{
				_popupCanvasGroup.interactable = false;
			}
		}

		protected override void OnDestroy()
		{
			_tween?.Kill();
			_tween = null;

			base.OnDestroy();

			if (_lockKey.HasValue)
			{
				TouchHelper.Unlock(_lockKey.Value);
				_lockKey = null;
			}
		}

		protected override void DoActivate(bool immediately)
		{
			if (!_lockKey.HasValue)
			{
				_lockKey = TouchHelper.Lock();
			}

			LayoutRebuilder.ForceRebuildLayoutImmediate(Popup);

			Vector2? initialScreenPoint;
			if (_initialPosition.HasValue)
			{
				var cameraObject = GameObject.FindGameObjectWithTag("MainCamera");
				var cam = cameraObject ? cameraObject.GetComponent<Camera>() : null;
				Assert.IsTrue(cam, "Can't find camera.");
				initialScreenPoint = RectTransformUtility.WorldToScreenPoint(cam, _initialPosition.Value);
			}
			else
			{
				initialScreenPoint = null;
			}

			ActivatableState = ActivatableState.ToActive;

			_popupCanvasGroup.alpha = 0.1f;
			_popupCanvasGroup.interactable = false;
			Blend.color = Color.clear;

			_tween?.Kill();
			var seq = DOTween.Sequence()
				.Append(_popupCanvasGroup.DOFade(1, AppearDuration).SetEase(Ease.OutQuad))
				.Join(Blend.DOFade(_blendInitialAlpha, AppearDuration).SetEase(Ease.Linear));
			_tween = seq;
			foreach (var item in _items)
			{
				var rt = (RectTransform)item.transform;
				rt.localScale = Vector3.one * 0.01f;
				var itemTween = DOTween.Sequence()
					.Append(rt.DOScale(Vector3.one, AppearDuration).SetEase(Ease.OutQuad));
				if (initialScreenPoint.HasValue)
				{
					rt.position = initialScreenPoint.Value;
					itemTween.Join(rt.DOAnchorPos(Vector2.zero, AppearDuration).SetEase(Ease.OutQuad));
				}

				seq.Join(itemTween);
			}

			seq.OnComplete(() =>
			{
				_tween = null;
				_popupCanvasGroup.interactable = true;
				ActivatableState = ActivatableState.Active;
			});
		}

		protected override void DoDeactivate(bool immediately)
		{
			ActivatableState = ActivatableState.ToInactive;
			_popupCanvasGroup.interactable = false;

			_tween?.Kill();
			var seq = DOTween.Sequence()
				.Append(_popupCanvasGroup.DOFade(0, DisappearDuration).SetEase(Ease.InQuad))
				.Join(Blend.DOFade(0, DisappearDuration).SetEase(Ease.Linear));
			_tween = seq;
			foreach (var item in _items)
			{
				var rt = (RectTransform)item.transform;
				var targetScale = Vector3.one * (Result.HasValue && Result.Value == item.Color ? 3f : 0.01f);
				seq.Join(rt.DOScale(targetScale, DisappearDuration).SetEase(Ease.OutQuad));
			}

			seq.OnComplete(() =>
			{
				_tween = null;
				ActivatableState = ActivatableState.Inactive;
			});
		}

		public void OnSelectItem(IdolListItemViewController item)
		{
			Result = item.Color;
			Close();
		}
	}
}