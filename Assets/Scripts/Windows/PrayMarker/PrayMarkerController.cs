using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Windows.PrayMarker
{
	/// <summary>
	/// This is the base behaviour of the pray marker (rotating glow and pulsating text).
	/// </summary>
	[DisallowMultipleComponent]
	public class PrayMarkerController : MonoBehaviour
	{
		private const float ShineRotateDuration = 5f;
		private const float LabelPulseDuration = 0.5f;

		private Tween _tween;

		[SerializeField] private Image _shine;
		[SerializeField] private TextMeshProUGUI _label;

		private void OnEnable()
		{
			Assert.IsTrue(_shine && _label, "Shine and label must have.");

			_tween?.Kill();

			_shine.transform.rotation = Quaternion.identity;
			_label.transform.localScale = Vector3.one * 0.95f;

			_tween = DOTween.Sequence()
				.Append(_shine.transform
					.DOLocalRotate(new Vector3(0f, 0f, -360f), ShineRotateDuration, RotateMode.FastBeyond360)
					.SetEase(Ease.Linear))
				.Join(DOTween.Sequence()
					.Append(_label.transform.DOScale(Vector3.one * 1.15f, LabelPulseDuration * 0.5f)
						.SetEase(Ease.InQuad).SetLoops(4, LoopType.Yoyo).SetDelay(LabelPulseDuration * 2f)))
				.SetLoops(-1);
		}

		private void OnDisable()
		{
			_tween?.Kill();
			_tween = null;
		}

		protected virtual void OnDestroy()
		{
			_tween?.Kill();
			_tween = null;
		}
	}
}