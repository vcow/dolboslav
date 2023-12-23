using Base.Activatable;

namespace Windows.PrayMarker
{
	/// <summary>
	/// Base class for the markers that can be activated/deactivated.
	/// See https://github.com/vcow/lib-logicality for details about IActivatable.
	/// </summary>
	public abstract class ActivatablePrayMarkerController : PrayMarkerController, IActivatable
	{
		private ActivatableState? _activatableState;

		// IActivatable

		public abstract void Activate(bool immediately = false);

		public abstract void Deactivate(bool immediately = false);

		public ActivatableState ActivatableState
		{
			get => _activatableState ?? ActivatableState.Inactive;
			protected set
			{
				if (value == _activatableState)
				{
					return;
				}

				_activatableState = value;
				ActivatableStateChangedEvent?.Invoke(this, value);
			}
		}

		public event ActivatableStateChangedHandler ActivatableStateChangedEvent;

		// \IActivatable
	}
}