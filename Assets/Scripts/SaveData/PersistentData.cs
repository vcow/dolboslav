using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace SaveData
{
	/// <summary>
	/// The base class for all persistable models.
	/// </summary>
	/// <typeparam name="TDataType">Type of the inherited class.</typeparam>
	public abstract class PersistentData<TDataType> where TDataType : PersistentData<TDataType>, new()
	{
		/// <summary>
		/// Unique key to identity the model.
		/// </summary>
		protected abstract string Key { get; }

		/// <summary>
		/// Save the model to the local storage.
		/// </summary>
		/// <returns>Returns true if model saved successfully.</returns>
		public virtual async Task<bool> Save()
		{
			var data = JsonConvert.SerializeObject(this);
			var fileName = Path.Combine(Application.persistentDataPath, $"{Key}.json");
			if (File.Exists(fileName))
			{
				var oldFileName = $"{fileName}.old";
				if (File.Exists(oldFileName))
				{
					File.Delete(oldFileName);
				}

				File.Move(fileName, oldFileName);
			}

			await File.WriteAllTextAsync(fileName, data, Encoding.Unicode);

			return true;
		}

		/// <summary>
		/// Restore the model from the local storage.
		/// </summary>
		/// <returns>Returns the restored model.</returns>
		public static async Task<TDataType> Restore()
		{
			var tempInstance = new TDataType();
			var key = tempInstance.Key;
			(tempInstance as IDisposable)?.Dispose();

			var fileName = Path.Combine(Application.persistentDataPath, $"{key}.json");
			string data;
			if (File.Exists(fileName))
			{
				data = await File.ReadAllTextAsync(fileName);
			}
			else
			{
				fileName = $"{fileName}.old";
				if (File.Exists(fileName))
				{
					data = await File.ReadAllTextAsync(fileName);
				}
				else
				{
					return null;
				}
			}

			var restored = JsonConvert.DeserializeObject<TDataType>(data);
			return restored;
		}
	}
}