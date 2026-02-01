using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.AssetBundles;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;

namespace Nox.AssetBundles.Runtime {
	public class Main : IMainModInitializer, IAssetBundleAPI {
		public AssetManager manager;

		public void OnInitializeMain(IMainModCoreAPI api) {
			manager = new AssetManager();
			GlobalAssetBundleManager.manager = manager;
		}

		public void OnDisposeMain() {
			if (GlobalAssetBundleManager.manager == manager)
				GlobalAssetBundleManager.manager = null;
			manager?.Dispose();
		}
		
		public UniTask<IAsset> LoadFileAsync(string path, string by, IProgress<float> progress = null)
			=> manager.LoadFileAsync(path, by, progress);

		public IAsset LoadFile(string path, string by)
			=> manager.LoadFile(path, by);

		public void DetachFile(string path, string by)
			=> manager.DetachFile(path, by);

		public IAsset AttachFile(AssetBundle ab, string path, string by)
			=> manager.AttachFile(ab, path, by);
	}
}