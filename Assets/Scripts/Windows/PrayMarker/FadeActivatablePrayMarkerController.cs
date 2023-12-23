using System;
using DG.Tweening;
using UnityEngine;
using Base.Activatable;
using UnityEngine.Assertions;

namespace Windows.PrayMarker
{
	/// <summary>
	/// Marker that activated/deactivated with a fade effect. GameObject must have CanvasGroup component.
	/// </summary>
	[RequireComponent(typeof(CanvasGroup))]
	public sealed class FadeActivatablePrayMarkerController : ActivatablePrayMarkerController
	{
		private const float AppearDuration = 0.5f;
		private const float DisappearDuration = 0.5f;

		private readonly Lazy<CanvasGroup> _canvasGroup;

		public FadeActivatablePrayMarkerController()
		{
			_canvasGroup = new Lazy<CanvasGroup>(() => GetComponent<CanvasGroup>());
		}

		public override void Activate(bool immediately = false)
		{
			var canvasGroup = _canvasGroup.Value;
			Assert.IsNotNull(canvasGroup, "CanvasGroup must have.");

			if (immediately)
			{
				canvasGroup.interactable = true;
				canvasGroup.alpha = 1f;

				gameObject.SetActive(true);
				ActivatableState = ActivatableState.Active;
			}
			else
			{
				DOTween.Kill(canvasGroup);
				ActivatableState = ActivatableState.ToActive;

				canvasGroup.interactable = false;
				canvasGroup.alpha = 0f;
				gameObject.SetActive(true);
				canvasGroup.DOFade(1, AppearDuration).SetEase(Ease.OutQuad).SetLink(gameObject)
					.OnComplete(() =>
					{
						canvasGroup.interactable = true;
						ActivatableState = ActivatableState.Active;
					});
			}
		}

		public override void Deactivate(bool immediately = false)
		{
			var canvasGroup = _canvasGroup.Value;
			Assert.IsNotNull(canvasGroup, "CanvasGroup must have.");

			if (immediately)
			{
				canvasGroup.interactable = false;
				canvasGroup.alpha = 0f;

				gameObject.SetActive(false);
				ActivatableState = ActivatableState.Inactive;
			}
			else
			{
				DOTween.Kill(canvasGroup);
				ActivatableState = ActivatableState.ToInactive;

				canvasGroup.interactable = false;
				canvasGroup.DOFade(0, DisappearDuration).SetEase(Ease.OutQuad).SetLink(gameObject)
					.OnComplete(() =>
					{
						gameObject.SetActive(false);
						ActivatableState = ActivatableState.Inactive;
					});
			}
		}
	}
}