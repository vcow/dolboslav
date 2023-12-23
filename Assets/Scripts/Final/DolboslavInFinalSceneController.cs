using System.Collections;
using DG.Tweening;
using Game.Model;
using UnityEngine;
using UnityEngine.Assertions;
using Zenject;

namespace Final
{
	/// <summary>
	/// Animated Dolboslav character in Game scene controller.
	/// </summary>
	[DisallowMultipleComponent, RequireComponent(typeof(Collider2D))]
	public class DolboslavInFinalSceneController : MonoBehaviour
	{
		private Vector3 _p1, _p2;

		[Inject] private readonly IGameModel _gameModel;
		[Inject] private readonly GameConfig _gameConfig;

		[SerializeField] private Animator _animator;
		[SerializeField] private Animator _blessAnimator;

		private static readonly int FinalWin = Animator.StringToHash("FinalWin");
		private static readonly int FinalFail = Animator.StringToHash("FinalFail");
		private static readonly int Coin = Animator.StringToHash("Coin");

		private IEnumerator Start()
		{
			Assert.IsTrue(_animator && _blessAnimator, "Dolboslav's  and bless Animators must have.");
			_animator.gameObject.SetActive(false);

			// Skip one frame for the camera controller to work.
			yield return null;

			PreparePrayBehaviour();

			// Set Dolboslav in the initial position and play appropriate animation.
			transform.position = _p1;

			yield return new WaitForSeconds(5f);

			_animator.gameObject.SetActive(true);

			if (_gameModel.IsGameOver(out var isWin) && isWin)
			{
				_animator.SetTrigger(FinalWin);

				var numSteps = Mathf.Max(_gameModel.History.Count, 1);
				var blessSettings = _gameConfig.GetBlessSettingsForSteps(numSteps);
				_blessAnimator.transform.localScale *= blessSettings.scaleFactor;
				_blessAnimator.SetTrigger(Coin);
			}
			else
			{
				_animator.SetTrigger(FinalFail);
				_blessAnimator.gameObject.SetActive(false);
			}

			transform.DOMove(_p2, 2.5f).SetEase(Ease.Linear).SetLink(gameObject);
		}

		// Prepare Dolboslav's appear behaviour. He will have to jump from the bottom and just play fail
		// or win animation.
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

			var bounds = cldr.bounds;
			var initialPosition = transform.position;
			// Start appear point
			_p1 = new Vector3(initialPosition.x, sceneScreenRect.yMin - bounds.size.y, initialPosition.z);

			// Destination appear point
			_p2 = _p1 + Vector3.up * bounds.size.y * 0.5f;
		}
	}
}