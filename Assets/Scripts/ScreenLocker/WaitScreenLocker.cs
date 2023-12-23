using Base.Activatable;
using Base.WindowManager.Extensions.ScreenLockerExtension;
using UnityEngine;
using UnityEngine.UI;

namespace ScreenLocker
{
	/// <summary>
	/// Screen locker to lock the screen during some action is being performed.
	/// (see https://github.com/vcow/lib-window-manager#how-to-use-screenlockermanager for details)
	/// </summary>
	[DisallowMultipleComponent, RequireComponent(typeof(RawImage))]
	public class WaitScreenLocker : ScreenLocker<WaitScreenLocker>
	{
		public override LockerType LockerType => LockerType.BusyWait;

		public override void Activate(bool immediately = false)
		{
			GetComponent<RawImage>().enabled = true;
			ActivatableState = ActivatableState.Active;
		}

		public override void Deactivate(bool immediately = false)
		{
			GetComponent<RawImage>().enabled = false;
			ActivatableState = ActivatableState.Inactive;
		}

		public override bool Force()
		{
			return true;
		}
	}
}