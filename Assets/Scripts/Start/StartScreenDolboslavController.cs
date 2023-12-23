using UnityEngine;
using UnityEngine.Assertions;

namespace Start
{
	/// <summary>
	/// In start scene Dolboslav's controller.
	/// </summary>
	[DisallowMultipleComponent, RequireComponent(typeof(Animator))]
	public sealed class StartScreenDolboslavController : MonoBehaviour
	{
		private static readonly int Dance = Animator.StringToHash("Dance");

		private void Start()
		{
			// He's just dancing.
			var animator = GetComponent<Animator>();
			Assert.IsTrue(animator, "Animator must have.");
			animator.SetTrigger(Dance);
		}
	}
}