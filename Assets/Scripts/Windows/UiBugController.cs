using UnityEngine;

namespace Windows
{
	/// <summary>
	/// Controller for the animated red beetle in the settings window.
	/// </summary>
	[DisallowMultipleComponent, RequireComponent(typeof(Animator)), ExecuteInEditMode]
	public sealed class UiBugController : MonoBehaviour
	{
		[SerializeField] private bool _run;
		[SerializeField] private bool _closeEyes;
		[SerializeField] private bool _pressed;
		[SerializeField, Header("Body parts")] private GameObject _eyelids;
		[SerializeField] private GameObject _pressedBody;

		private Animator _animator;
		private static readonly int RunHash = Animator.StringToHash("Run");

		private bool? _isRun;
		private bool? _eyesIsClosed;
		private bool? _isPressed;

		public bool IsPressed
		{
			get => _pressed;
			set => _pressed = value;
		}

		private void Update()
		{
			if (!_animator)
			{
				_animator = GetComponent<Animator>();
				if (!_animator)
				{
					Debug.LogError("Animator must have.");
					return;
				}
			}

			if (_run != _isRun)
			{
				_animator.SetBool(RunHash, _run);
				_isRun = _run;
			}

			if (_pressedBody && _pressed != _isPressed)
			{
				_pressedBody.SetActive(_pressed);
				_isPressed = _pressed;
			}

			if (_eyelids && _closeEyes != _eyesIsClosed)
			{
				_eyelids.SetActive(_closeEyes);
				_eyesIsClosed = _closeEyes;
			}
		}
	}
}