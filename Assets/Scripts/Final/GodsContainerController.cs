using System;
using System.Collections.Generic;
using System.Linq;
using Game.Model;
using UnityEngine;
using Zenject;

namespace Final
{
	/// <summary>
	/// Common container of gods in the sky controller.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class GodsContainerController : MonoBehaviour
	{
		[Inject] private readonly IGameModel _gameModel;
		[Inject] private readonly DiContainer _container;

		[SerializeField, Header("God places")] private Transform _place1;
		[SerializeField] private Transform _place2;
		[SerializeField] private Transform _place3;
		[SerializeField] private Transform _place4;

		[SerializeField, Header("Prefabs")] private List<GodPrefabRecord> _gods;

		private void Start()
		{
			var isGameOver = _gameModel.IsGameOver(out var isWin);
			if (!isGameOver)
			{
				Debug.LogError("The gods are visible when Game isn't over.");
			}

			var places = new (Transform container, IdolColor color)[]
			{
				(_place1, _gameModel.Target.Idol1Color.Value),
				(_place2, _gameModel.Target.Idol2Color.Value),
				(_place3, _gameModel.Target.Idol3Color.Value),
				(_place4, _gameModel.Target.Idol4Color.Value)
			};

			// Instantiate the gods and start their animation.
			foreach (var place in places)
			{
				foreach (Transform child in place.container)
				{
					Destroy(child.gameObject);
				}

				var prefab = _gods.FirstOrDefault(record => record._color == place.color)?._godPrefab;
				if (!prefab)
				{
					Debug.LogErrorFormat("Can't find prefab for the {0} god.", place.color);
					continue;
				}

				var god = _container.InstantiatePrefabForComponent<GodController>(prefab);
				god.transform.SetParent(place.container, false);

				if (isWin)
				{
					god.PlayHappy();
				}
				else
				{
					god.PlayAngry();
				}
			}
		}

		/// <summary>
		/// The record of the god prefab for the specific color.
		/// </summary>
		[Serializable]
		public class GodPrefabRecord
		{
			public IdolColor _color;
			public GodController _godPrefab;
		}
	}
}