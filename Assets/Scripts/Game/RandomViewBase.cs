using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Random = System.Random;

namespace Game
{
	/// <summary>
	/// The base class for scene item view that have multiple representations from which final view randomly selected.
	/// </summary>
	[DisallowMultipleComponent]
	public abstract class RandomViewBase : MonoBehaviour
	{
		protected static readonly Random Rnd = new();

		/// <summary>
		/// The list of views.
		/// </summary>
		protected abstract IReadOnlyList<GameObject> Views { get; }

		/// <summary>
		/// Chose random view representation from the Views list.
		/// </summary>
		/// <param name="exclude">List of the views indices to ignore during selection.</param>
		/// <returns>The index of the chosen view.</returns>
		public int RandomizeView(IEnumerable<int> exclude)
		{
			Assert.IsTrue(Views is { Count: > 0 }, "Has no views to randomize.");
			var ex = exclude?.ToArray();
			int index;
			if (ex == null || ex.Length == 0)
			{
				index = Views.Count > 1 ? Rnd.Next(Views.Count) : 0;
			}
			else
			{
				var validIndexes = Enumerable.Range(0, Views.Count).Where(i => !ex.Contains(i)).ToArray();
				if (validIndexes.Length == 0)
				{
					return RandomizeView(null);
				}

				index = validIndexes[validIndexes.Length > 1 ? Rnd.Next(validIndexes.Length) : 0];
			}

			for (var i = 0; i < Views.Count; ++i)
			{
				var stump = Views[i];
				var isVisible = i == index;
				stump.SetActive(isVisible);
				PostProcessView(stump, isVisible);
			}

			return index;
		}

		/// <summary>
		/// Override in the inherited classes to postprocess item's view.
		/// </summary>
		/// <param name="view">The view to postprocess.</param>
		/// <param name="isVisible">View's visibility state.</param>
		protected virtual void PostProcessView(GameObject view, bool isVisible)
		{
		}
	}
}