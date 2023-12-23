using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

/// <summary>
/// Game config. Contains initial game values and base settings.
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Game Config")]
public class GameConfig : ScriptableObjectInstaller<GameConfig>
{
	public override void InstallBindings()
	{
		Container.Bind<GameConfig>().FromInstance(this).AsSingle();
	}

	[SerializeField] private int _maxSteps = 6;

	[SerializeField, Header("User Model presets")]
	private int _additionalGameSteps;

	[SerializeField] private int _startNumGames = 5;
	[SerializeField] private int _startNumBonusGames;
	[SerializeField, Header("Spawn")] private int _maxSpawnByTimeGames = 1;
	[SerializeField] private long _spawnGameDelayTimeSec = 60L * 45L;

	[SerializeField, Header("Bless settings")]
	private List<BlessRecord> _bless;

	/// <summary>
	/// The number of available steps in the game.
	/// </summary>
	public int MaxSteps => _maxSteps;

	/// <summary>
	/// If more than zero, user have one additional step.
	/// </summary>
	public int AdditionalGameSteps => _additionalGameSteps;

	/// <summary>
	/// Initial number of free games.
	/// </summary>
	public int StartNumGames => _startNumGames;

	/// <summary>
	/// Initial number of the bonus games.
	/// </summary>
	public int StartNumBonusGames => _startNumBonusGames;

	/// <summary>
	/// Maximum number of games spawned by time.
	/// </summary>
	public int MaxSpawnByTimeGames => _maxSpawnByTimeGames;

	/// <summary>
	/// Time in seconds after which another game becomes available.
	/// </summary>
	public long SpawnGameDelayTimeSec => _spawnGameDelayTimeSec;

	/// <summary>
	/// Get bless settings for the specified number of step taken.
	/// </summary>
	/// <param name="numSteps">The number of steps taken.</param>
	/// <returns>Returns the number of scores won and the scale factor for the coin in UI to show winnings.</returns>
	public (float scaleFactor, long blessValue) GetBlessSettingsForSteps(int numSteps)
	{
		var scaleFactor = 0f;
		var blessValue = 0L;

		var sortedBless = _bless.OrderBy(record => record._numSteps);
		foreach (var record in sortedBless)
		{
			scaleFactor = record._coinScaleFactor;
			blessValue = record._blessValue;

			if (record._numSteps >= numSteps)
			{
				break;
			}
		}

		return (scaleFactor, blessValue);
	}

	[Serializable]
	public class BlessRecord
	{
		public int _numSteps;
		public float _coinScaleFactor;
		public long _blessValue;
	}
}