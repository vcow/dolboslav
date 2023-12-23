using Base.Activatable;

namespace Windows.PrayMarker
{
	/// <summary>
	/// Marker that activated/deactivated without any animated effects.
	/// </summary>
	public sealed class SimpleActivatablePrayMarkerController : ActivatablePrayMarkerController
	{
		public override void Activate(bool immediately = false)
		{
			gameObject.SetActive(true);
			ActivatableState = ActivatableState.Active;
		}

		public override void Deactivate(bool immediately = false)
		{
			gameObject.SetActive(false);
			ActivatableState = ActivatableState.Inactive;
		}
	}
}