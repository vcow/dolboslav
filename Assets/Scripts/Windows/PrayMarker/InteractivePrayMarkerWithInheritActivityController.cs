using Base.Activatable;
using UniRx;
using UnityEngine.Assertions;

namespace Windows.PrayMarker
{
	/// <summary>
	/// Pray marker for the markers which are located on other IActivatable UI items (i.e. windows) and must
	/// inherit their activity - activated/deactivated with their parents.
	/// </summary>
	public sealed class InteractivePrayMarkerWithInheritActivityController : InteractivePrayMarkerController
	{
		protected override void Start()
		{
			var parent = transform.parent;
			var parentActivatable = parent ? parent.GetComponentInParent<IActivatable>() : null;
			Assert.IsNotNull(parentActivatable, "this gameObject must be located in the IActivatable parent.");
			
			var activatableMarker = GetComponent<ActivatablePrayMarkerController>();
			Assert.IsNotNull(activatableMarker, "ActivatablePrayMarkerController must have.");

			activatableMarker.Deactivate(true);

			// Make reactive property from the activatable state of the parent and listen them.
			Observable.FromEvent<ActivatableStateChangedHandler,
					(IActivatable _activatableState, ActivatableState state)>(
					h => (activatable, state) => h((activatable, state)),
					h => parentActivatable.ActivatableStateChangedEvent += h,
					h => parentActivatable.ActivatableStateChangedEvent -= h)
				.Select(tuple => tuple.state)
				.ToReadOnlyReactiveProperty(parentActivatable.ActivatableState)
				.Subscribe(state =>
				{
					switch (state)
					{
						case ActivatableState.Active:
							if (!IsStarted)
							{
								IsStarted = true;
								MakeMarkerBehaviour();
							}

							break;
						case ActivatableState.ToInactive:
						case ActivatableState.Inactive:
							if (IsStarted && activatableMarker.IsActive())
							{
								activatableMarker.Deactivate();
							}

							break;
					}
				}).AddTo(Handlers);
		}
	}
}