using System.Collections.Generic;
using System.IO;
using System.Linq;
using Base.WindowManager.Extensions.ScreenLockerExtension;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace ScreenLocker
{
	/// <summary>
	/// Scriptable object of the settings of screen locker system. See
	/// https://github.com/vcow/lib-window-manager#how-to-use-screenlockermanager for details about the screen locker
	/// system. For proper operation this scriptable object must be added to the ProjectContext Scriptable Object
	/// Installers list.
	/// </summary>
	[CreateAssetMenu(fileName = "ScreenLockerSettings", menuName = "Screen Locker Settings")]
	public class ScreenLockerSettings : ScriptableObjectInstaller<ScreenLockerSettings>
	{
		[SerializeField] private List<ScreenLockerBase> _screenLockers = new();

		[SerializeField, Header("Additional Screen lockers"), Tooltip("This is screen locker between the steps.")]
		private ScreenLockerBase _stepScreenLocker;

		[SerializeField, Tooltip("This is screen locker between the last step and final scene.")]
		private ScreenLockerBase _finalScreenLocker;

		public override void InstallBindings()
		{
			Container.Bind<ScreenLockerSettings>().FromInstance(this).AsSingle();
		}

		/// <summary>
		/// The list of common screen lockers.
		/// </summary>
		public IReadOnlyList<ScreenLockerBase> ScreenLockers => _screenLockers;

		/// <summary>
		/// Screen locker to lock the screen between the steps.
		/// </summary>
		public ScreenLockerBase StepScreenLocker => _stepScreenLocker;

		/// <summary>
		/// Screen locker to lock the screen between the last step and final scene.
		/// </summary>
		public ScreenLockerBase FinalScreenLocker => _finalScreenLocker;

		/// <summary>
		/// Get common screen locker by type.
		/// </summary>
		/// <param name="lockerType">Type of the required screen locker.</param>
		/// <returns>Required common screen locker or null.</returns>
		public ScreenLockerBase GetScreenLocker(LockerType lockerType)
		{
			return _screenLockers.FirstOrDefault(locker => locker.LockerType == lockerType);
		}

#if UNITY_EDITOR
		[MenuItem("Tools/Game Settings/Screen Locker Settings")]
		private static void FindAndSelectWindowManager()
		{
			var instance = Resources.FindObjectsOfTypeAll<ScreenLockerSettings>().FirstOrDefault();
			if (!instance)
			{
				LoadAllPrefabs();
				instance = Resources.FindObjectsOfTypeAll<ScreenLockerSettings>().FirstOrDefault();
			}

			if (instance)
			{
				Selection.activeObject = instance;
				return;
			}

			Debug.LogError("Can't find prefab of ScreenLockerSettings.");
		}

		private static void LoadAllPrefabs()
		{
			Directory.GetDirectories(Application.dataPath, @"Resources", SearchOption.AllDirectories)
				.Select(s => Directory.GetFiles(s, @"*.prefab", SearchOption.TopDirectoryOnly))
				.SelectMany(strings => strings.Select(Path.GetFileNameWithoutExtension))
				.Distinct().ToList().ForEach(s => Resources.LoadAll(s));
		}
#endif
	}
}