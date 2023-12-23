using System;
using System.Collections;
using Base.Activatable;
using Base.WindowManager.Extensions.ScreenLockerExtension;
using UnityEngine;
using UnityEngine.Assertions;

namespace ScreenLocker
{
	/// <summary>
	/// Screen locker to lock the screen between the game and final scenes.
	/// (see https://github.com/vcow/lib-window-manager#how-to-use-screenlockermanager for details)
	/// </summary>
	public class FinalScreenLocker : ScreenLocker<FinalScreenLocker>
	{
		private bool _isStarted;
		private float? _activateTimestamp;

		[SerializeField] private CloudsController _clouds;
		[SerializeField, Header("Settings")] private float _lockerDelayTimeSec = 2f;

		public override void Activate(bool immediately = false)
		{
			StopAllCoroutines();

			ActivatableState = immediately ? ActivatableState.Active : ActivatableState.ToActive;
			ValidateState();
		}

		public override void Deactivate(bool immediately = false)
		{
			ActivatableState = immediately ? ActivatableState.Inactive : ActivatableState.ToInactive;

			Assert.IsTrue(_activateTimestamp.HasValue);

			StopAllCoroutines();

			var dt = Time.time - _activateTimestamp.Value;
			if (dt >= _lockerDelayTimeSec)
			{
				ValidateState();
			}
			else
			{
				StartCoroutine(DelayedValidateRoutine(_lockerDelayTimeSec - dt));
			}
		}

		private IEnumerator DelayedValidateRoutine(float delayTime)
		{
			yield return new WaitForSeconds(delayTime);
			ValidateState();
		}

		public override bool Force()
		{
			switch (ActivatableState)
			{
				case ActivatableState.ToActive:
					ActivatableState = ActivatableState.Active;
					ValidateState();
					break;
				case ActivatableState.ToInactive:
					ActivatableState = ActivatableState.Inactive;
					ValidateState();
					break;
				default:
					return false;
			}

			return true;
		}

		public override LockerType LockerType => LockerType.SceneLoader;

		protected override void Start()
		{
			_isStarted = true;

			Assert.IsTrue(_clouds, "CloudsController must have.");
			_clouds.animationEvent.AddListener(OnAnimationEvent);

			ValidateState();

			base.Start();
		}

		protected override void OnDestroy()
		{
			StopAllCoroutines();
			base.OnDestroy();
		}

		private void OnAnimationEvent(string eventName)
		{
			switch (eventName)
			{
				case "opened":
					_activateTimestamp = null;
					ActivatableState = ActivatableState.Inactive;
					break;
				case "closed":
					_activateTimestamp = Time.time;
					ActivatableState = ActivatableState.Active;
					break;
				default:
					throw new NotSupportedException($"The animation event {eventName} isn't supported.");
			}
		}

		private void ValidateState()
		{
			if (!_isStarted)
			{
				return;
			}

			switch (ActivatableState)
			{
				case ActivatableState.Active:
					_activateTimestamp = Time.time;
					_clouds.Close(true);
					break;
				case ActivatableState.ToActive:
					_clouds.Close(false);
					break;
				case ActivatableState.Inactive:
					_activateTimestamp = null;
					_clouds.Open(true);
					break;
				case ActivatableState.ToInactive:
					_clouds.Open(false);
					break;
			}
		}
	}
}