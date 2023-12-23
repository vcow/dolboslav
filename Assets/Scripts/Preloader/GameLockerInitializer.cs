using Base.Assignments;
using Base.WindowManager.Extensions.ScreenLockerExtension;
using UnityEngine.Assertions;

namespace Preloader
{
	/// <summary>
	/// This is assignment (see https://github.com/vcow/lib-logicality for details) which turns on
	/// the start Game screen locker in the initial queue in PreloaderInstaller.
	/// </summary>
	public class GameLockerInitializer : IAssignment
	{
		private readonly IScreenLockerManager _screenLockerManager;
		private bool _completed;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="screenLockerManager">Screen locker manager.</param>
		public GameLockerInitializer(IScreenLockerManager screenLockerManager)
		{
			_screenLockerManager = screenLockerManager;
		}

		public void Start()
		{
			_screenLockerManager.Lock(LockerType.GameLoader, () => Completed = true);
		}

		public bool Completed
		{
			get => _completed;
			private set
			{
				if (value == _completed)
				{
					return;
				}

				_completed = value;
				Assert.IsTrue(_completed);
				CompleteEvent?.Invoke(this);
			}
		}

		public event AssignmentCompleteHandler CompleteEvent;
	}
}