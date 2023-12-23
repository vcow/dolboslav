using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Assertions;
using Random = System.Random;

namespace Game
{
	/// <summary>
	/// The lighting controller.
	/// </summary>
	[DisallowMultipleComponent, RequireComponent(typeof(LookAtConstraint))]
	public sealed class FlashController : MonoBehaviour
	{
		// Flags for randomize the lighting.
		[Flags]
		private enum FlashEvolution
		{
			FlipHorizontal = 0x01,
			FlipVertical = 0x02
		}

		private const float FadeDuration = 0.3f;

		private static readonly Random Rnd = new();

		[SerializeField] private SpriteRenderer _flashSprite;
		[SerializeField] private ParticleSystem _splash;

		private void Start()
		{
			Assert.IsTrue(_flashSprite && _splash, "Flash and splash must have.");
			_flashSprite.gameObject.SetActive(false);
		}

		/// <summary>
		/// Lighting struck. 
		/// </summary>
		public void Hit()
		{
			var flashGameObject = _flashSprite.gameObject;
			flashGameObject.SetActive(true);

			// The lighting image can be flipped vertically and horizontally to randomize their view.
			var flags = (FlashEvolution)Rnd.Next(5);
			_flashSprite.flipX = (flags & FlashEvolution.FlipHorizontal) != 0;
			_flashSprite.flipY = (flags & FlashEvolution.FlipVertical) != 0;

			_flashSprite.color = Color.white;
			_flashSprite.DOFade(0, FadeDuration).SetEase(Ease.InQuad).SetLink(flashGameObject);

			_splash.Play();
		}
	}
}