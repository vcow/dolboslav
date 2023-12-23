using System;
using System.Collections.Generic;
using Game.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Windows
{
	/// <summary>
	/// Controller for the idol in the IdolSelectWindow. Allows to setup idol color.
	/// </summary>
	[DisallowMultipleComponent, RequireComponent(typeof(Button))]
	public sealed class IdolListItemViewController : MonoBehaviour
	{
		private IdolColor? _color;

		[SerializeField] private IdolColor _initialColor = IdolColor.Undefined;
		[SerializeField] private List<ViewRecord> _views;

		private void Start()
		{
			if (!_color.HasValue)
			{
				Color = _initialColor;
			}
		}

		/// <summary>
		/// Set and get current idol color.
		/// </summary>
		public IdolColor Color
		{
			get => _color ?? _initialColor;
			set
			{
				if (_color == value)
				{
					return;
				}

				_color = value;

				foreach (var record in _views)
				{
					record._view.gameObject.SetActive(record._color == _color);
				}
			}
		}

		[Serializable]
		public class ViewRecord
		{
			public IdolColor _color;
			public Image _view;
		}
	}
}