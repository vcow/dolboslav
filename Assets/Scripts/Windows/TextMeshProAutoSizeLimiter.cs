using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace Windows
{
	/// <summary>
	/// This script switch off TextMeshPro autosizing to prevent jumps when text changes at runtime. 
	/// </summary>
	[DisallowMultipleComponent, RequireComponent(typeof(TextMeshProUGUI))]
	public sealed class TextMeshProAutoSizeLimiter : MonoBehaviour
	{
		private IEnumerator Start()
		{
			yield return null;

			var tmp = GetComponent<TextMeshProUGUI>();
			Assert.IsTrue(tmp, "TextMeshPro must have.");
			tmp.enableAutoSizing = false;
		}
	}
}