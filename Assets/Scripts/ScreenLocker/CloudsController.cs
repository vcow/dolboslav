using UnityEngine;
using UnityEngine.Events;

namespace ScreenLocker
{
	/// <summary>
	/// Controller for the cloud in the final scene screen locker (ScreenLocker.FinalScreenLocker).
	/// Listens the animation events from the Mecanim.
	/// </summary>
	[DisallowMultipleComponent, RequireComponent(typeof(Animator))]
	public sealed class CloudsController : MonoBehaviour
	{
		// ReSharper disable once InconsistentNaming
		public UnityEvent<string> animationEvent = new();

		private static readonly int CloseImmediateKey = Animator.StringToHash("CloseImmediate");
		private static readonly int CloseKey = Animator.StringToHash("Close");
		private static readonly int OpenImmediateKey = Animator.StringToHash("OpenImmediate");
		private static readonly int OpenKey = Animator.StringToHash("Open");

		/// <summary>
		/// Cloud closed event handler.
		/// </summary>
		public void OnClosed()
		{
			animationEvent.Invoke("closed");
		}

		/// <summary>
		/// Cloud opened event handler.
		/// </summary>
		public void OnOpened()
		{
			animationEvent.Invoke("opened");
		}

		/// <summary>
		/// Start close animation.
		/// </summary>
		/// <param name="immediate">Close immediate.</param>
		public void Close(bool immediate)
		{
			GetComponent<Animator>().SetTrigger(immediate ? CloseImmediateKey : CloseKey);
		}

		/// <summary>
		/// Start open animation.
		/// </summary>
		/// <param name="immediate">Open immediate.</param>
		public void Open(bool immediate)
		{
			GetComponent<Animator>().SetTrigger(immediate ? OpenImmediateKey : OpenKey);
		}

		private void OnDestroy()
		{
			animationEvent.RemoveAllListeners();
		}
	}
}