using System;
using Base.WindowManager;
using Game.Model;
using UniRx;
using UnityEngine;
using UnityEngine.Assertions;
using Zenject;

namespace Windows.PrayMarker
{
	/// <summary>
	/// Pray marker that appear when Dolboslav is ready to pray. This marker interacts with the game model and
	/// automatically activated/deactivated. GameObject must have some ActivatablePrayMarkerController.
	/// Also can interact with the IWindowManager to hide the marker when TableWindow is opened.
	/// </summary>
	[DisallowMultipleComponent, RequireComponent(typeof(ActivatablePrayMarkerController))]
	public class InteractivePrayMarkerController : MonoBehaviour
	{
		protected readonly CompositeDisposable Handlers = new();
		protected bool IsStarted;

		[Inject] private readonly GameModelDecorator _gameModelDecorator;
		[Inject] private readonly IGameModel _gameModel;
		[Inject] private readonly IWindowManager _windowManager;

		[SerializeField, Header("Settings"), Tooltip("Hide marker when Table window is opened.")]
		private bool _reactTableWindow;

		protected virtual void Start()
		{
			if (_gameModel.IsGameOver(out _))
			{
				var activatableMarker = GetComponent<ActivatablePrayMarkerController>();
				Assert.IsNotNull(activatableMarker, "ActivatablePrayMarkerController must have.");

				activatableMarker.Deactivate(true);
			}
			else
			{
				MakeMarkerBehaviour();
			}

			IsStarted = true;
		}

		protected void MakeMarkerBehaviour()
		{
			var activatableMarker = GetComponent<ActivatablePrayMarkerController>();
			Assert.IsNotNull(activatableMarker, "ActivatablePrayMarkerController must have.");

			// React if user can move.
			IDisposable tableWindowCloseHandler = null;
			_gameModelDecorator.ReadyToMove.Subscribe(canPray =>
			{
				if (canPray)
				{
					if (_reactTableWindow)
					{
						var tableWindow = _windowManager.GetWindow(TableWindow.Id);
						if (tableWindow != null)
						{
							// Table is opened. Show marker only when table was closed.
							ListenForTableWindowClose(tableWindow);
						}
						else
						{
							activatableMarker.Activate(!IsStarted);
						}
					}
					else
					{
						activatableMarker.Activate(!IsStarted);
					}
				}
				else
				{
					activatableMarker.Deactivate(!IsStarted);
					if (tableWindowCloseHandler != null)
					{
						Handlers.Remove(tableWindowCloseHandler);
						tableWindowCloseHandler = null;
					}
				}
			}).AddTo(Handlers);

			if (_reactTableWindow)
			{
				// React if user open the Table (when table is opened marker must not be visible).
				Observable.FromEvent<WindowOpenedHandler, (IWindowManager windowManager, IWindow window)>(
						h => (manager, window) => h((manager, window)),
						h => _windowManager.WindowOpenedEvent += h,
						h => _windowManager.WindowOpenedEvent -= h)
					.Where(tuple => tuple.window.WindowId == TableWindow.Id)
					.Subscribe(tuple =>
					{
						activatableMarker.Deactivate(!IsStarted);
						if (tableWindowCloseHandler == null && _gameModelDecorator.ReadyToMove.Value)
						{
							ListenForTableWindowClose(tuple.window);
						}
					}).AddTo(Handlers);
			}

			// React round over (round is over when the new item added to the History).
			_gameModel.History.ObserveAdd().Subscribe(_ =>
			{
				activatableMarker.Deactivate(!IsStarted);
				Handlers.Dispose();
			}).AddTo(Handlers);

			return;

			void ListenForTableWindowClose(IWindow tableWindow)
			{
				Assert.IsNull(tableWindowCloseHandler, "Try to listen Table window close event twice.");

				tableWindowCloseHandler = Observable
					.FromEvent<CloseWindowHandler, (IWindow sender, object result)>(
						h => (sender, result) => h((sender, result)),
						h => tableWindow.CloseWindowEvent += h,
						h => tableWindow.CloseWindowEvent -= h)
					.Subscribe(_ =>
					{
						// ReSharper disable once AccessToModifiedClosure
						Handlers.Remove(tableWindowCloseHandler);
						tableWindowCloseHandler = null;
						activatableMarker.Activate(!IsStarted);
					}).AddTo(Handlers);
			}
		}

		private void OnDestroy()
		{
			Handlers.Dispose();
		}
	}
}