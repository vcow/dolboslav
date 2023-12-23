using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace Sound
{
	/// <summary>
	/// This component reproduces the "click" sound when user pointer up and/or down on the UI element.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class UiClicker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
	{
		[SerializeField] private string _mouseDownSound;
		[SerializeField] private string _mouseUpSound;

		[Inject] private readonly ISoundManager _soundManager;

		public void OnPointerDown(PointerEventData eventData)
		{
			if (!string.IsNullOrEmpty(_mouseDownSound))
			{
				_soundManager.PlaySound(_mouseDownSound);
			}
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			if (!string.IsNullOrEmpty(_mouseUpSound))
			{
				_soundManager.PlaySound(_mouseUpSound);
			}
		}
	}
}