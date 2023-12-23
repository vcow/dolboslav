using System.Collections.Generic;
using UnityEngine;

namespace Game
{
	/// <summary>
	/// Script to randomize idol pedestals at the start.
	/// </summary>
	[DisallowMultipleComponent]
	public class GameFieldViewController : MonoBehaviour
	{
		[SerializeField] private List<IdolViewController> _idols;

		private void Start()
		{
			var exclude = new HashSet<int>();
			foreach (var idol in _idols)
			{
				exclude.Add(idol.RandomizeView(exclude));
			}
		}
	}
}