using System;
using System.Collections;
using DG.Tweening;
using Sound;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using User.Model;
using Zenject;

namespace Final
{
	/// <summary>
	/// User's score view controller.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class ScoreCounterController : MonoBehaviour
	{
		private const int MaxNumPulses = 15;
		private const float ScoreChangeAnimationDuration = 3f;

		private Tween _tween;

		[Inject] private readonly IUserModel _userModel;
		[Inject] private readonly ISoundManager _soundManager;
		[InjectOptional] private readonly long _deltaScore;

		[SerializeField] private TextMeshProUGUI _label;

		private void Start()
		{
			Assert.IsTrue(_label, "Label must have.");
			_label.text = (_userModel.Score - _deltaScore).ToString();

			if (_deltaScore > 0)
			{
				// Delta score exists when user win.
				StartCoroutine(PlayIncreaseScoreRoutine());
			}
		}

		private void OnDestroy()
		{
			_tween?.Kill(true);
			StopAllCoroutines();
		}

		private IEnumerator PlayIncreaseScoreRoutine()
		{
			yield return new WaitForSeconds(4f);

			long pulseStep;
			int numSteps;
			if (_deltaScore > MaxNumPulses)
			{
				pulseStep = _deltaScore / MaxNumPulses;
				numSteps = _deltaScore % MaxNumPulses > 0L ? MaxNumPulses + 1 : MaxNumPulses;
			}
			else
			{
				pulseStep = 1;
				numSteps = (int)_deltaScore;
			}

			long initialScore;
			var currentScore = initialScore = _userModel.Score - _deltaScore;

			// Pulse and increase score numSteps times.
			_tween?.Kill();
			_tween = DOTween.To(() => 0, i =>
				{
					var newScore = Math.Min(_userModel.Score, initialScore + pulseStep * i);
					if (newScore != currentScore)
					{
						currentScore = newScore;
						_label.text = currentScore.ToString();
						Pulse();
					}
				}, numSteps, ScoreChangeAnimationDuration)
				.SetEase(Ease.OutQuad).OnComplete(() =>
				{
					_tween = null;
					_label.text = _userModel.Score.ToString();
				});
		}

		private void Pulse()
		{
			const float pulseScale = 1.1f;
			var t = _label.transform;
			DOTween.Kill(t);
			t.localScale = Vector3.one * pulseScale;
			t.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutQuad).SetLink(_label.gameObject);
			_soundManager.PlaySound("click7");
		}
	}
}