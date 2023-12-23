using System.Collections;
using UnityEngine;
using Random = System.Random;

namespace Final
{
	/// <summary>
	/// The god in the sky in final scene controller.
	/// </summary>
	[DisallowMultipleComponent]
	public class GodController : MonoBehaviour
	{
		private readonly Random _rnd = new();

		[SerializeField] private Animator _godAnimator;

		private static readonly int Happy = Animator.StringToHash("Happy");
		private static readonly int Angry = Animator.StringToHash("Angry");

		/// <summary>
		/// Play happy animation.
		/// </summary>
		public void PlayHappy()
		{
			StopAllCoroutines();
			StartCoroutine(PlayAnimationRoutine(Happy));
		}

		/// <summary>
		/// Play angry animation.
		/// </summary>
		public void PlayAngry()
		{
			StopAllCoroutines();
			StartCoroutine(PlayAnimationRoutine(Angry));
		}

		private void OnDestroy()
		{
			StopAllCoroutines();
		}

		private IEnumerator PlayAnimationRoutine(int triggerId)
		{
			yield return new WaitForSeconds(2f + (float)(_rnd.NextDouble() * 5.0));
			_godAnimator.SetTrigger(triggerId);

			for (;;)
			{
				yield return new WaitForSeconds(15f + (float)(_rnd.NextDouble() * 15.0));
				// Repeat after some time.
				_godAnimator.SetTrigger(triggerId);
			}
			// ReSharper disable once IteratorNeverReturns
		}
	}
}