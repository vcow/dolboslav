using System.Collections.Generic;
using System.Linq;
using Base.Activatable;
using Game.Model;
using UniRx;
using UnityEngine;
using UnityEngine.Events;

namespace Windows
{
	/// <summary>
	/// Controller for the one record of the table in the TableWindow. Can activate and deactivate
	/// all their points to interact with user.
	/// </summary>
	public sealed class TableWindowRecordViewController : TableRecordViewController, IActivatable
	{
		private readonly CompositeDisposable _handlers = new();

		private bool _playPulse;
		private ActivatableState _activatableState;

		// ReSharper disable once InconsistentNaming
		[Header("Events")] public UnityEvent<TableWindowPointViewController> pointClickEvent = new();

		protected override void PulseRecord(bool play)
		{
			_playPulse = play;
			if (!play || this.IsActive())
			{
				base.PulseRecord(play);
			}
		}

		public void Activate(bool immediately = false)
		{
			if (this.IsActiveOrActivated())
			{
				return;
			}

			_handlers.Clear();

			var pointStateListeners = new List<IReadOnlyReactiveProperty<bool>>();
			foreach (var point in ToPointsEnumerable())
			{
				if (point.Color != IdolColor.Undefined || point is not TableWindowPointViewController windowPoint)
				{
					continue;
				}

				windowPoint.Activate(immediately);

				if (!immediately && windowPoint.ActivatableState != ActivatableState.Active)
				{
					// Convert point to the observable that send true when the point is activated.
					var stateListener = Observable
						.FromEvent<ActivatableStateChangedHandler, (IActivatable sender, ActivatableState result)>(
							h => (sender, result) => h((sender, result)),
							h => windowPoint.ActivatableStateChangedEvent += h,
							h => windowPoint.ActivatableStateChangedEvent -= h)
						.Select(tuple => tuple.result == ActivatableState.Active)
						.ToReadOnlyReactiveProperty(false)
						.AddTo(_handlers);
					pointStateListeners.Add(stateListener);
				}
			}

			if (pointStateListeners.Any())
			{
				// Mark record as active only if all points have become active.
				ActivatableState = ActivatableState.ToActive;
				pointStateListeners.CombineLatestValuesAreAllTrue().First(b => b)
					.Subscribe(_ =>
					{
						_handlers.Clear();
						ActivatableState = ActivatableState.Active;
					}).AddTo(_handlers);
			}
			else
			{
				ActivatableState = ActivatableState.Active;
			}
		}

		public void Deactivate(bool immediately = false)
		{
			if (this.IsInactiveOrDeactivated())
			{
				return;
			}

			_handlers.Clear();

			var pointStateListeners = new List<IReadOnlyReactiveProperty<bool>>();
			foreach (var point in ToPointsEnumerable())
			{
				if (point.Color != IdolColor.Undefined || point is not TableWindowPointViewController windowPoint)
				{
					continue;
				}

				windowPoint.Deactivate(immediately);

				if (!immediately && windowPoint.ActivatableState != ActivatableState.Inactive)
				{
					// Convert point to the observable that send true when the point is deactivated.
					var stateListener = Observable
						.FromEvent<ActivatableStateChangedHandler, (IActivatable, ActivatableState)>(
							h => (sender, result) => h((sender, result)),
							h => windowPoint.ActivatableStateChangedEvent += h,
							h => windowPoint.ActivatableStateChangedEvent -= h)
						.Select(tuple => tuple.Item2 == ActivatableState.Inactive)
						.ToReadOnlyReactiveProperty(false)
						.AddTo(_handlers);
					pointStateListeners.Add(stateListener);
				}
			}

			if (pointStateListeners.Any())
			{
				// Mark record as inactive only if all points have become inactive.
				ActivatableState = ActivatableState.ToInactive;
				pointStateListeners.CombineLatestValuesAreAllTrue().First(b => b)
					.Subscribe(_ =>
					{
						_handlers.Clear();
						ActivatableState = ActivatableState.Inactive;
					}).AddTo(_handlers);
			}
			else
			{
				ActivatableState = ActivatableState.Inactive;
			}
		}

		protected override void OnDestroy()
		{
			pointClickEvent.RemoveAllListeners();
			_handlers.Dispose();

			base.OnDestroy();
		}

		public ActivatableState ActivatableState
		{
			get => _activatableState;
			private set
			{
				if (value == _activatableState)
				{
					return;
				}

				_activatableState = value;
				ActivatableStateChangedEvent?.Invoke(this, _activatableState);

				if (_playPulse && _activatableState == ActivatableState.Active)
				{
					base.PulseRecord(true);
				}
			}
		}

		public event ActivatableStateChangedHandler ActivatableStateChangedEvent;

		public void OnPointClick(TableWindowPointViewController point)
		{
			pointClickEvent.Invoke(point);
		}
	}
}