using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SaveData;
using UnityEngine;
using UnityEngine.Assertions;

namespace User.Model
{
	/// <summary>
	/// UserModel implementation. Can be persist to and restore from JSON. Must be modified only
	/// with User.UserModelController.
	/// </summary>
	public class UserModel : PersistentData<UserModel>, IUserModel
	{
		private readonly SemaphoreSlim _saveDataSemaphore;

		/// <summary>
		/// Timestamp of last start of the game in seconds.
		/// </summary>
		[JsonProperty("lastGameTimestamp")] public long LastGameTimestamp;

		/// <summary>
		/// Remaining free games.
		/// </summary>
		[JsonProperty("numGames")] public int NumGames;

		/// <summary>
		/// Number of the available additional game steps. If more than zero user has one additional step in the game.
		/// </summary>
		[JsonProperty("additionalGameSteps")] public int AdditionalGameSteps;

		/// <summary>
		/// User's score.
		/// </summary>
		[JsonProperty("score")] public long Score;

		/// <summary>
		/// Additional games that do not affect LastGameTimestamp.
		/// </summary>
		[JsonProperty("numBonusGames")] public int NumBonusGames;

		protected override string Key => nameof(UserModel);

		// IUserModel

		bool IUserModel.HasAdditionalGameStep => AdditionalGameSteps > 0;

		long IUserModel.Score => Score;

		int IUserModel.GetNumGames(GameConfig gameConfig, out float? nextGameSpawnDelayTimeSec)
		{
			return GetNumGames(gameConfig, true, out nextGameSpawnDelayTimeSec);
		}

		// \IUserModel

		public UserModel()
		{
		}

		[JsonConstructor]
		public UserModel(int additionalGameSteps, long score, int numGames, int numBonusGames, long lastGameTimestamp)
		{
			_saveDataSemaphore = new SemaphoreSlim(1, 1);

			AdditionalGameSteps = additionalGameSteps;
			Score = score;
			NumGames = numGames;
			NumBonusGames = numBonusGames;
			LastGameTimestamp = lastGameTimestamp;
		}

		public int GetNumGames(GameConfig gameConfig, bool persist, out float? nextGameSpawnDelayTimeSec)
		{
			if (NumGames >= gameConfig.MaxSpawnByTimeGames)
			{
				// Already have max number of games that can be spawned by time.
				nextGameSpawnDelayTimeSec = null;
				return NumGames + NumBonusGames;
			}

			var now = DateTimeOffset.Now.ToUnixTimeSeconds();
			var deltaTime = now - LastGameTimestamp;
			var newGamesNum = deltaTime / gameConfig.SpawnGameDelayTimeSec;
			if (newGamesNum <= 0L)
			{
				// New game hasn't spawned yet.
				nextGameSpawnDelayTimeSec = (float)(gameConfig.SpawnGameDelayTimeSec - deltaTime);
				return NumGames + NumBonusGames;
			}

			LastGameTimestamp += newGamesNum * gameConfig.SpawnGameDelayTimeSec;
			var spawnedGamesNum = newGamesNum > int.MaxValue ? int.MaxValue : (int)newGamesNum;
			if (NumGames + spawnedGamesNum >= gameConfig.MaxSpawnByTimeGames)
			{
				NumGames = gameConfig.MaxSpawnByTimeGames;
				nextGameSpawnDelayTimeSec = null;
			}
			else
			{
				NumGames += spawnedGamesNum;
				deltaTime = now - LastGameTimestamp;
				Assert.IsTrue(deltaTime > 0L && deltaTime < gameConfig.SpawnGameDelayTimeSec, "Wrong delta time.");
				nextGameSpawnDelayTimeSec = (float)(gameConfig.SpawnGameDelayTimeSec - deltaTime);
			}

			if (persist)
			{
				// Save new values of number or the games and last game timestamp.
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
				Save();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			}

			return NumGames + NumBonusGames;
		}

		public override async Task<bool> Save()
		{
			await _saveDataSemaphore.WaitAsync();
			try
			{
				if (!await base.Save())
				{
					Debug.LogError("Can't save UserModel.");
					return false;
				}
			}
			finally
			{
				_saveDataSemaphore.Release();
			}

			return true;
		}
	}
}