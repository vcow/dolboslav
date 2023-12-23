using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Windows
{
	/// <summary>
	/// Additional component for the toggle, switches off the toggle background when toggle is on
	/// to show only the checkmark view.
	/// </summary>
	[DisallowMultipleComponent, RequireComponent(typeof(Toggle))]
	public sealed class SettingsToggleController : MonoBehaviour
	{
		private Toggle _toggle;
		private Image _background;

		private void Start()
		{
			_toggle = GetComponent<Toggle>();
			Assert.IsTrue(_toggle);

			var backgroundTransform = _toggle.transform.Find("Background");
			_background = backgroundTransform ? backgroundTransform.GetComponent<Image>() : null;

			if (_background)
			{
				_toggle.onValueChanged.AddListener(OnValueChanged);
				_background.enabled = !_toggle.isOn;
			}
		}

		private void OnDestroy()
		{
			if (_background)
			{
				_toggle.onValueChanged.RemoveListener(OnValueChanged);
			}
		}

		private void OnValueChanged(bool value)
		{
			_background.enabled = !value;
		}
	}
}