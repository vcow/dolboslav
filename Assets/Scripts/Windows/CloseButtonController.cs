using Start.Signals;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace Windows
{
	/// <summary>
	/// The component which signals to the additional UI elements (red beetle in the settings window) when user
	/// press and release the button.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class CloseButtonController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
	{
		[Inject] private readonly SignalBus _signalBus;

		public void OnPointerDown(PointerEventData eventData)
		{
			_signalBus.TryFire<CloseButtonDownSignal>();
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			_signalBus.TryFire<CloseButtonUpSignal>();
		}
	}
}