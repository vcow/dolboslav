namespace User.Model
{
	/// <summary>
	/// User model interface.
	/// </summary>
	public interface IUserModel
	{
		/// <summary>
		/// Flag indicates that the user has "Favor of the gods" (a additional step in the game).
		/// </summary>
		bool HasAdditionalGameStep { get; }

		/// <summary>
		/// Current user's score.
		/// </summary>
		long Score { get; }

		/// <summary>
		/// Get the number of remaining games.
		/// </summary>
		/// <param name="gameConfig">The game settings as a GameConfig instance.</param>
		/// <param name="nextGameSpawnDelayTimeSec">Returns time in seconds after which a new game will appear.</param>
		/// <returns>Number of available games.</returns>
		int GetNumGames(GameConfig gameConfig, out float? nextGameSpawnDelayTimeSec);
	}
}