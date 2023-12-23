using System;
using Base.WindowManager.Extensions.ScreenLockerExtension;
using Zenject;

namespace ScreenLocker
{
	/// <summary>
	/// The extension interface for IScreenLockerManager. Allows to show screen locker in the specified
	/// Zenject container. See https://github.com/vcow/lib-window-manager#how-to-use-screenlockermanager
	/// for details about the base IScreenLockerManager interface.
	/// </summary>
	public interface IScreenLockerManagerExt : IScreenLockerManager
	{
		void Lock(DiContainer container, LockerType type, Action completeCallback);
	}
}