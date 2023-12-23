using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Game.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Windows
{
	/// <summary>
	/// Point view controller for the TableWindow record.
	/// </summary>
	[DisallowMultipleComponent]
	public class TablePointViewController : MonoBehaviour
	{
		private const float PulseIncreaseAspect = 1.1f;
		private const float PulseDuration = 0.25f;

		private IdolColor? _color;
		private Vector3? _initialScale;
		private Tween _pulseTween;

		[SerializeField] private Image _icon;
		[SerializeField] private IdolColor _defaultColor = IdolColor.Undefined;
		[SerializeField] private List<IconColorRecord> _colorMap;

		protected Image Icon => _icon;

		/// <summary>
		/// Pulse the point to attract attention.
		/// </summary>
		public virtual void Pulse(bool play = true)
		{
			if (!_initialScale.HasValue)
			{
				return;
			}

			if (play && _pulseTween == null)
			{
				var t = transform;
				t.localScale = _initialScale.Value;
				_pulseTween = t.DOScale(_initialScale.Value * PulseIncreaseAspect, PulseDuration)
					.SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad).OnComplete(() => _pulseTween = null);
			}
			else if (!play && _pulseTween != null)
			{
				_pulseTween.Kill(true);
				transform.localScale = _initialScale.Value;
			}
		}

		/// <summary>
		/// Set and get current idol color.
		/// </summary>
		public IdolColor Color
		{
			get => _color ?? _defaultColor;
			set
			{
				if (value == _color)
				{
					return;
				}

				_color = value;
				var sprite = _colorMap.FirstOrDefault(record => record._color == _color)?._sprite;
				if (sprite)
				{
					_icon.sprite = sprite;
					_icon.gameObject.SetActive(true);
				}
				else
				{
					_icon.gameObject.SetActive(false);
				}
			}
		}

		private void Start()
		{
			_initialScale = transform.localScale;

			if (!_color.HasValue)
			{
				Color = _defaultColor;
			}
		}

		private void OnDestroy()
		{
			_pulseTween?.Kill(true);
		}

		[Serializable]
		public class IconColorRecord
		{
			public IdolColor _color;
			public Sprite _sprite;
		}
	}
}