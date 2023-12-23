using System.Collections.Generic;

namespace Game.Model
{
	public static class StepRecordExtensions
	{
		/// <summary>
		/// Convert IStepRecord to the colors sequence.
		/// </summary>
		/// <param name="step">Internal step.</param>
		/// <returns>The sequence of idol colors.</returns>
		public static IEnumerable<IdolColor> ToColors(this IStepRecord step)
		{
			yield return step.Idol1Color.Value;
			yield return step.Idol2Color.Value;
			yield return step.Idol3Color.Value;
			yield return step.Idol4Color.Value;
		}

		/// <summary>
		/// Check if the step is complete and ready to move.
		/// </summary>
		/// <param name="step">Checkable step.</param>
		/// <returns>Returns true if the step hasn't repeatable colors and empty values.</returns>
		public static bool CheckStep(this IStepRecord step)
		{
			var result = 0;
			foreach (var color in step.ToColors())
			{
				if (color == IdolColor.Undefined || (result & (int)color) != 0)
				{
					return false;
				}

				result |= (int)color;
			}

			return true;
		}
	}
}