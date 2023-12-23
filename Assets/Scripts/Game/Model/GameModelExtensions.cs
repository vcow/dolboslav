using System.Linq;

namespace Game.Model
{
	public static class GameModelExtensions
	{
		/// <summary>
		/// Check if the Game is over.
		/// </summary>
		/// <param name="gameModel">Game model.</param>
		/// <param name="isWin">Result win/loose. Matters if the Game is over.</param>
		/// <returns>Returns true if the Game is over.</returns>
		public static bool IsGameOver(this IGameModel gameModel, out bool isWin)
		{
			isWin = gameModel.History.Any() && gameModel.History.Last().Equals(gameModel.Target);
			return gameModel.History.Count >= gameModel.MaxSteps || isWin;
		}
	}
}