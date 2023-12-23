using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sound;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using Zenject;

namespace Game
{
	/// <summary>
	/// Controller for the mushroom on the game scene.
	/// </summary>
	[SelectionBase, RequireComponent(typeof(Collider2D))]
	public sealed class MushroomViewController : RandomViewBase
	{
		private const float AppearDuration = 0.5f;
		private static readonly string[] Sounds = { "chpok1", "chpok2" };

		[SerializeField, Header("View")] private List<Transform> _views;
		[SerializeField] private Transform _viewContainer;
		[SerializeField] private Transform _shadow;

		// ReSharper disable InconsistentNaming
		[Header("Events")] public UnityEvent<Collider2D> triggerEnterEvent = new();
		public UnityEvent<Collider2D> triggerExitEvent = new();
		// ReSharper restore InconsistentNaming

		[Inject] private readonly ISoundManager _soundManager;

		private bool? _isVisible;
		private Vector3 _initialViewScale = Vector3.one;
		private Vector3 _initialShadowScale = Vector3.one;
		private Tween _tween;

		protected override IReadOnlyList<GameObject> Views => _views.Select(v => v.gameObject).ToArray();

		private void Awake()
		{
			Assert.IsTrue(_viewContainer && _shadow, "View container and the shadow must have.");
			_initialViewScale = _viewContainer.localScale;
			_initialShadowScale = _shadow.localScale;
		}

		private void Start()
		{
			if (_isVisible.HasValue)
			{
				_viewContainer.gameObject.SetActive(_isVisible.Value);
				_shadow.gameObject.SetActive(_isVisible.Value);
			}
			else
			{
				_isVisible = gameObject.activeSelf;
			}
		}

		/// <summary>
		/// Show the mushroom.
		/// </summary>
		/// <param name="delayTimeSec">Delay time before the mushroom appears. Has no effect
		/// if immediate flag is true.</param>
		/// <param name="immediate">Show the mushroom immediate, without animation and delay.</param>
		public void Show(float delayTimeSec, bool immediate = false)
		{
			if (_isVisible.HasValue && _isVisible.Value)
			{
				return;
			}

			_isVisible = true;
			_viewContainer.gameObject.SetActive(true);
			_shadow.gameObject.SetActive(true);

			_tween?.Kill();
			if (immediate)
			{
				_tween = null;
				_viewContainer.localScale = _initialViewScale;
				_shadow.localScale = _initialShadowScale;
			}
			else
			{
				_viewContainer.localScale = Vector3.one * 0.01f;
				_shadow.localScale = Vector3.one * 0.01f;
				_tween = DOTween.Sequence()
					.Append(_viewContainer.DOScale(_initialViewScale, AppearDuration).SetEase(Ease.OutBack))
					.Join(_shadow.DOScale(_initialShadowScale, AppearDuration).SetEase(Ease.Linear))
					.OnComplete(() => _tween = null);

				var sound = Sounds[Rnd.Next(Sounds.Length)];
				if (delayTimeSec > 0)
				{
					_tween.SetDelay(delayTimeSec);
					_soundManager.PlaySound(sound, delayTimeSec);
				}
				else
				{
					_soundManager.PlaySound(sound);
				}
			}
		}

		private void OnDestroy()
		{
			_tween?.Kill(true);

			triggerEnterEvent.RemoveAllListeners();
			triggerExitEvent.RemoveAllListeners();
		}

		/// <summary>
		/// Hide the mushroom.
		/// </summary>
		public void Hide()
		{
			if (_isVisible.HasValue && !_isVisible.Value || !gameObject.activeSelf)
			{
				return;
			}

			_isVisible = false;

			_tween?.Kill();
			_tween = null;

			_viewContainer.localScale = _initialViewScale;
			_shadow.localScale = _initialShadowScale;

			_viewContainer.gameObject.SetActive(false);
			_shadow.gameObject.SetActive(false);
		}

		private void OnTriggerEnter2D(Collider2D other)
		{
			triggerEnterEvent.Invoke(other);
		}

		private void OnTriggerExit2D(Collider2D other)
		{
			triggerExitEvent.Invoke(other);
		}
	}
}