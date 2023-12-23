using Base.Assignments;
using Base.Assignments.Initable;
using Base.WindowManager.Extensions.ScreenLockerExtension;
using UnityEngine;
using User;
using Zenject;

namespace Preloader
{
	/// <summary>
	/// Controller of the preloader scene.
	/// </summary>
	[DisallowMultipleComponent]
	public class PreloaderInstaller : MonoInstaller<PreloaderInstaller>
	{
		[Inject] private readonly IScreenLockerManager _screenLockerManager;
		[Inject] private readonly ZenjectSceneLoader _sceneLoader;
		[Inject] private readonly UserModelController _userModelController;

		public override void InstallBindings()
		{
		}

		public override void Start()
		{
			// Create and execute the initial queue - the set of actions which must
			// be performed before starting the game.
			var initialQueue = new AssignmentQueue();
			initialQueue.Add(new GameLockerInitializer(_screenLockerManager));
			initialQueue.Add(new AssignmentInit(_userModelController));
			// TODO: Add other initializations here

			initialQueue.CompleteEvent += OnInitialComplete;
			initialQueue.Start();
		}

		private void OnInitialComplete(IAssignment assignment)
		{
			_sceneLoader.LoadSceneAsync("StartScene");
		}
	}
}