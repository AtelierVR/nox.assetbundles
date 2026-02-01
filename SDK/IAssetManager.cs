using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nox.AssetBundles {
	/// <summary>
	/// Interface for managing assets,
	/// providing methods for loading assets both asynchronously and synchronously.
	/// </summary>
	public interface IAssetManager {
		/// <summary>
		/// Asynchronously loads an asset from the specified path.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="by"></param>
		/// <param name="progress"></param>
		/// <returns></returns>
		public UniTask<IAsset> LoadFileAsync(string path, string by, IProgress<float> progress = null);

		/// <summary>
		/// Synchronously loads an asset from the specified path.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="by"></param>
		/// <returns></returns>
		public IAsset LoadFile(string path, string by);

		/// <summary>
		/// Unloads the asset at the specified path.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="by"></param>
		void DetachFile(string path, string by);

		/// <summary>
		/// Attaches an existing AssetBundle to the manager.
		/// </summary>
		/// <param name="ab"></param>
		/// <param name="path"></param>
		/// <param name="by"></param>
		/// <returns></returns>
		IAsset AttachFile(AssetBundle ab, string path, string by);
	}
}