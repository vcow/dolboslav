using UnityEngine;
using UnityEngine.Events;

namespace Game
{
	/// <summary>
	/// The listener of the animation events from the Mecanim for the Dolboslav.
	/// </summary>
	[DisallowMultipleComponent, RequireComponent(typeof(Animator))]
	public class DolboslavAnimationEventController : MonoBehaviour
	{
		// ReSharper disable once InconsistentNaming
		public UnityEvent<string> animationEvent = new();

		/// <summary>
		/// The lighting struck event
		/// </summary>
		public void OnHitEvent()
		{
			animationEvent.Invoke("hit");
		}

		private void OnDestroy()
		{
			animationEvent.RemoveAllListeners();
		}
	}
}