using System;
using Cysharp.Threading.Tasks;
using Nox.AssetBundles;
using UnityEngine;
namespace Nox.CCK.AssetBundles {
	public static class GlobalAssetBundleManager {
		public static IAssetManager manager;

		public static UniTask<IAsset> LoadFileAsync(string path, string by, IProgress<float> progress = null)
			=> manager.LoadFileAsync(path, by, progress);

		public static IAsset LoadFile(string path, string by)
			=> manager.LoadFile(path, by);

		public static void DetachFile(string path, string by)
			=> manager.DetachFile(path, by);

		public static IAsset AttachFile(AssetBundle ab, string path, string by)
			=> manager.AttachFile(ab, path, by);
	}
}