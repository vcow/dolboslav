using System.Collections;
using Base.WindowManager.Extensions.ScreenLockerExtension;
using DG.Tweening;
using Game.Model;
using Game.Signals;
using Helpers.TouchHelper;
using Sound;
using UniRx;
using UnityEngine;
using UnityEngine.Assertions;
using Zenject;

namespace Game
{
	/// <summary>
	/// Animated Dolboslav character in Game scene controller.
	/// </summary>
	[DisallowMultipleComponent, RequireComponent(typeof(Collider2D))]
	public class DolboslavInGameSceneController : MonoBehaviour
	{
		private readonly CompositeDisposable _handlers = new();
		private Coroutine _idleCoroutine;

		private Vector3 _p1, _p2, _p3, _p4, _p5, _p4Ls, _p5Ls, _blessTarget;
		private float? _readyTimestamp;

		private static readonly Vector3 Scale1 = Vector3.one * 0.5f;
		private static readonly Vector3 Scale2 = Vector3.one * 0.6f;

		[Inject] private readonly GameModelDecorator _gameModelDecorator;
		[Inject] private readonly IGameModel _gameModel;
		[Inject] private readonly SignalBus _signalBus;
		[Inject] private readonly ISoundManager _soundManager;
		[Inject] private readonly GameConfig _gameConfig;
		[Inject] private readonly IScreenLockerManager _screenLocker;

		[SerializeField] private Animator _animator;
		[SerializeField] private FlashController _flash;
		[SerializeField] private Animator _blessAnimator;

		private static readonly int PrayPrepare = Animator.StringToHash("PrayPrepare");
		private static readonly int Pray = Animator.StringToHash("Pray");
		private static readonly int PrayIdle1 = Animator.StringToHash("PrayIdle1");
		private static readonly int PrayIdle2 = Animator.StringToHash("PrayIdle2");
		private static readonly int PrayGoRight = Animator.StringToHash("PrayGoRight");
		private static readonly int PrayGoLeft = Animator.StringToHash("PrayGoLeft");
		private static readonly int Reset = Animator.StringToHash("Reset");
		private static readonly int WonderRight = Animator.StringToHash("WonderRight");
		private static readonly int Joy = Animator.StringToHash("Joy");
		private static readonly int Fail = Animator.StringToHash("Fail");
		private static readonly int Comming = Animator.StringToHash("Comming");

		private IEnumerator Start()
		{
			Assert.IsTrue(_animator, "Dolboslav's Animator must have.");

			// Skip one frame for the camera controller to work.
			yield return null;

			PreparePrayBehaviour();

			// React if user can move.
			_gameModelDecorator.ReadyToMove.Subscribe(canPray =>
			{
				_animator.SetBool(PrayPrepare, canPray);

				if (canPray)
				{
					if (_idleCoroutine != null)
					{
						StopCoroutine(_idleCoroutine);
						_idleCoroutine = null;
					}
					else
					{
						_idleCoroutine = StartCoroutine(IdleRoutine());
					}

					_readyTimestamp = Time.time;
				}
				else
				{
					_idleCoroutine = StartCoroutine(IdleRoutine());
					_readyTimestamp = null;
				}
			}).AddTo(_handlers);

			// React round is over.
			_gameModel.History.ObserveAdd().Subscribe(_ =>
			{
				// The round is over when the new item added to the History. Stop react input here.
				_handlers.Clear();

				// Play final pray animation.
				StartCoroutine(PlayPrayAnimation());
			}).AddTo(_handlers);

			// Go to initial state of the pray action.
			_animator.SetTrigger(Pray);

			// Hide bless
			_blessAnimator.gameObject.SetActive(false);
		}

		// Prepare Dolboslav's complex pray behaviour. He will have to go of the right edge of the screen
		// then dance from right to left screen edge and then jump from the bottom to see result.
		private void PreparePrayBehaviour()
		{
			// Try to calculate edges of the visible area.
			Rect sceneScreenRect;
			var cam = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
			if (cam)
			{
				var bottomLeft = cam.ViewportToWorldPoint(Vector3.zero);
				var topRight = cam.ViewportToWorldPoint(Vector3.one);
				sceneScreenRect = new Rect
				{
					xMin = bottomLeft.x,
					yMin = bottomLeft.y,
					xMax = topRight.x,
					yMax = topRight.y
				};
			}
			else
			{
				Debug.LogError("Can't find camera.");
				sceneScreenRect = new Rect
				{
					xMin = -2.8125f,
					yMin = -5f,
					xMax = 2.8125f,
					yMax = 5f
				};
			}

			var cldr = GetComponent<Collider2D>();
			Assert.IsTrue(cldr, "Collider must have.");

			var t = transform;
			var initialPosition = t.position;

			// Destination point for go away motion
			var bounds = cldr.bounds;
			_p1 = initialPosition + Vector3.right * (sceneScreenRect.xMax - bounds.min.x);

			var initialScale = t.localScale;
			var dolboslavSize = new Vector2(bounds.size.x / initialScale.x, bounds.size.y / initialScale.y);

			// Start point for second dancing motion
			var sz = dolboslavSize * Scale1;
			_p2 = new Vector3(_p1.x + sz.x, sceneScreenRect.yMin - sz.y * 0.3f, _p1.z);

			// Destination left point for dancing
			_p3 = new Vector3(sceneScreenRect.xMin - sz.x, _p2.y, _p2.z);

			// Final appear start position
			sz = dolboslavSize * Scale2;
			_p4 = new Vector3(sceneScreenRect.xMin + sz.x * 0.5f, sceneScreenRect.yMin - sz.y, _p3.z);

			// Final appear end position
			_p5 = new Vector3(_p4.x, sceneScreenRect.yMin - sz.y * 0.3f, _p4.z);

			// Final appear start position (last step)
			sz = dolboslavSize * Scale2;
			_p4Ls = new Vector3(sceneScreenRect.center.x, sceneScreenRect.yMin - sz.y, _p3.z);

			// Final appear end position (last step)
			_p5Ls = new Vector3(_p4Ls.x, sceneScreenRect.yMin - sz.y * 0.3f, _p4.z);

			// Bless final target point
			_blessTarget = new Vector3(sceneScreenRect.center.x,
				sceneScreenRect.center.y + sceneScreenRect.height * 0.2f, _blessAnimator.transform.position.y);
		}

		// Scenario of the pray behaviour.
		private IEnumerator PlayPrayAnimation()
		{
			_screenLocker.Lock(LockerType.BusyWait, null);

			var speed = 1.3f;
			var t = transform;

			// Play tambourine and go beyond the right edge of the screen
			var duration = Mathf.Abs(_p1.x - t.position.x) / speed;
			var tween = t.DOMove(_p1, duration).SetEase(Ease.InQuad).SetLink(gameObject);
			_animator.SetTrigger(PrayGoRight);
			_soundManager.PlaySound("buben");
			_soundManager.PlayMusic("intro", 1f);

			while (tween.active)
			{
				yield return null;
			}

			_animator.gameObject.SetActive(false);
			yield return new WaitForSeconds(0.5f);

			// Move Dolboslav to second position
			t.position = _p2;
			t.localScale = Scale1;
			_animator.gameObject.SetActive(true);
			_animator.SetTrigger(Pray);
			_animator.SetTrigger(PrayGoLeft);
			_animator.SetBool(PrayPrepare, true);

			// Go to right edge of the screen
			speed = 2;
			duration = Mathf.Abs(_p3.x - _p2.x) / speed;
			tween = t.DOMove(_p3, duration).SetEase(Ease.Linear).SetLink(gameObject);
			_soundManager.PlaySound("buben", 2f);

			while (tween.active)
			{
				yield return null;
			}

			_animator.gameObject.SetActive(false);
			yield return new WaitForSeconds(1.5f);

			// Move to the final appear position
			var isGameOver = _gameModel.IsGameOver(out var isWin);
			var p4 = isGameOver ? _p4Ls : _p4;
			var p5 = isGameOver ? _p5Ls : _p5;

			t.position = p4;
			t.localScale = Scale2;
			_animator.gameObject.SetActive(true);
			_animator.SetTrigger(Reset);

			// Final appear
			tween = t.DOMove(p5, 1f).SetEase(Ease.OutBack).SetLink(gameObject);
			_soundManager.PlaySound("wzuch1", 0.25f);

			while (tween.active)
			{
				yield return null;
			}

			_soundManager.PlayMusic("main_title");

			if (isGameOver)
			{
				// Game is over
				yield return new WaitForSeconds(0.5f);

				if (isWin)
				{
					// Win animation
					_animator.SetTrigger(Joy);

					var numSteps = Mathf.Max(_gameModel.History.Count, 1);
					var blessSettings = _gameConfig.GetBlessSettingsForSteps(numSteps);
					_blessAnimator.transform.localScale *= blessSettings.scaleFactor;

					_blessAnimator.gameObject.SetActive(true);
					_blessAnimator.SetTrigger(Comming);

					_blessAnimator.transform.DOMove(_blessTarget, 5f).SetEase(Ease.OutQuad)
						.SetLink(_blessAnimator.gameObject);
					_soundManager.PlaySound("hallelyja", 0.5f);
				}
				else
				{
					// Fail animation
					var animationEventController = _animator.GetComponent<DolboslavAnimationEventController>();
					Assert.IsTrue(animationEventController, "Dolboslav must have DolboslavAnimationEventController");

					animationEventController.animationEvent.AddListener(evt =>
					{
						switch (evt)
						{
							case "hit":
								_soundManager.PlaySound("flash1");
								_flash.Hit();
								break;
						}
					});

					_animator.SetTrigger(Fail);
				}

				yield return new WaitForSeconds(5f);

				// Send signal to finish game
				_signalBus.TryFire<FinishStepSignal>();
			}
			else
			{
				_animator.SetTrigger(WonderRight);
				yield return new WaitForSeconds(1.5f);

				// Send signal to finish step
				_signalBus.TryFire<FinishStepSignal>();
			}

			_screenLocker.Unlock(null, LockerType.BusyWait);
		}

		private void OnDestroy()
		{
			StopAllCoroutines();
			_handlers.Dispose();
		}

		// Make Dolboslav's idle behaviour.
		private IEnumerator IdleRoutine()
		{
			for (;;)
			{
				_animator.SetTrigger(PrayIdle1);
				var t = 15f + Random.Range(5f, 25f);
				yield return new WaitForSeconds(t);
				_animator.SetTrigger(PrayIdle2);
				t = Random.Range(0.5f, 3f);
				yield return new WaitForSeconds(t);
			}
			// ReSharper disable once IteratorNeverReturns
		}

		private void OnMouseUpAsButton()
		{
			if (TouchHelper.IsLocked)
			{
				return;
			}

			if (_readyTimestamp.HasValue && Time.time - _readyTimestamp.Value >= 1f)
			{
				// Send a signal if the move is ready and at least 1 second has passed
				// for Dolboslav's prepare animation has finished
				_signalBus.TryFire<PraySignal>();
			}
		}
	}
}