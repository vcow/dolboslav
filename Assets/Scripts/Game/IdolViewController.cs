using System;
using System.Collections.Generic;
using System.Linq;
using Windows;
using Base.WindowManager;
using DG.Tweening;
using Game.Model;
using Game.Signals;
using Helpers.TouchHelper;
using Sound;
using UniRx;
using UnityEngine;
using UnityEngine.Assertions;
using Zenject;

namespace Game
{
	/// <summary>
	/// Controller of the idol on the scene.
	/// </summary>
	[RequireComponent(typeof(Collider2D))]
	public class IdolViewController : RandomViewBase
	{
		private const float AppearDuration = 0.45f;
		private const float DisappearDuration = 0.3f;

		[SerializeField, Header("Pedestal")] private List<Transform> _pedestalViews;
		[SerializeField, Header("Idol")] private Transform _idolContainer;
		[SerializeField] private int _idolNumber;
		[SerializeField] private IdolColor _initialState = IdolColor.Undefined;
		[SerializeField] private List<IdolRecord> _idolViews;

		[Inject] private readonly SignalBus _signalBus;
		[Inject] private readonly IGameModel _gameModel;
		[Inject] private readonly ISoundManager _soundManager;
		[Inject] private readonly IWindowManager _windowManager;

		private readonly CompositeDisposable _handlers = new();
		private IdolColor? _currentState;

		protected override IReadOnlyList<GameObject> Views => _pedestalViews.Select(p => p.gameObject).ToArray();

		private void Start()
		{
			SetState(_initialState, true);

			Assert.IsTrue(_idolNumber is >= 1 and <= 4, "Idol number must be in range from 1 to 4.");
			switch (_idolNumber)
			{
				case 1:
					_gameModel.CurrentStep.Idol1Color.Subscribe(OnIdolChanged).AddTo(_handlers);
					break;
				case 2:
					_gameModel.CurrentStep.Idol2Color.Subscribe(OnIdolChanged).AddTo(_handlers);
					break;
				case 3:
					_gameModel.CurrentStep.Idol3Color.Subscribe(OnIdolChanged).AddTo(_handlers);
					break;
				case 4:
					_gameModel.CurrentStep.Idol4Color.Subscribe(OnIdolChanged).AddTo(_handlers);
					break;
				default:
					throw new NotSupportedException();
			}

			_gameModel.History.ObserveAdd().Subscribe(_ =>
			{
				// Round is over when the new item added to the History. Stop react model here.
				_handlers.Clear();
			}).AddTo(_handlers);
		}

		private void OnIdolChanged(IdolColor value)
		{
			if (_currentState == value)
			{
				return;
			}

			Assert.IsTrue(_idolContainer, "Idol container must have.");
			DOTween.Kill(_idolContainer);

			// Remove old view
			_idolContainer.DOScale(Vector3.one * 0.1f, DisappearDuration).SetEase(Ease.InQuad)
				.SetLink(_idolContainer.gameObject).OnComplete(() =>
				{
					// Set new view
					SetState(value, false);
				});

			if (!_windowManager.GetWindows().Any())
			{
				_soundManager.PlaySound("wzuch1");
			}
		}

		private void SetState(IdolColor color, bool immediate)
		{
			if (_currentState == color)
			{
				return;
			}

			_currentState = color;
			foreach (var record in _idolViews)
			{
				record._view.gameObject.SetActive(record._color == _currentState);
			}

			DOTween.Kill(_idolContainer);
			if (immediate)
			{
				_idolContainer.localScale = Vector3.one;
			}
			else
			{
				if (color != IdolColor.Undefined && !_windowManager.GetWindows().Any())
				{
					_soundManager.PlaySound("click5", 0.2f);
				}

				_idolContainer.DOScale(Vector3.one, AppearDuration).SetEase(Ease.OutBack)
					.SetLink(_idolContainer.gameObject);
			}
		}

		private void OnMouseUpAsButton()
		{
			if (TouchHelper.IsLocked)
			{
				return;
			}

			var cldr = GetComponent<Collider2D>();
			_soundManager.PlaySound("click6");

			// Open the idol color selection window.
			var wnd = _windowManager.ShowWindow(IdolSelectWindow.Id,
				new object[] { _currentState ?? IdolColor.Undefined, cldr.bounds.center });

			IDisposable handler = null;
			// Introduce the CloseWindowEvent as Observable and subscribe it.
			handler = Observable.FromEvent<CloseWindowHandler, (IWindow sender, object result)>(
					h => (sender, result) => h((sender, result)),
					h => wnd.CloseWindowEvent += h, h => wnd.CloseWindowEvent -= h)
				.Subscribe(result =>
				{
					// ReSharper disable once AccessToModifiedClosure
					_handlers.Remove(handler);

					// Apply new selected color.
					_signalBus.TryFire(new SelectIdolColorSignal(_idolNumber, (IdolColor)result.result));
				}).AddTo(_handlers);
		}

		private void OnDestroy()
		{
			_handlers.Dispose();
		}

		[Serializable]
		public class IdolRecord
		{
			public IdolColor _color;
			public Transform _view;
		}
	}
}