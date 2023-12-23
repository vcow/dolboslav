using Base.WindowManager;
using Zenject;

namespace WindowManager
{
	/// <summary>
	/// The extension interface for IWindowManager. Allows to show window in the specified Zenject container.
	/// See https://github.com/vcow/lib-window-manager for details about the base IWindowManager interface.
	/// </summary>
	public interface IWindowManagerExt : IWindowManager
	{
		IWindow ShowWindow(DiContainer container, string windowId, object[] args = null, bool? isUnique = null,
			bool? overlap = null, string windowGroup = null);
	}
}