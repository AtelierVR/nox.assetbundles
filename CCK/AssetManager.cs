using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.AssetBundles;
using UnityEngine;

namespace Nox.CCK.AssetBundles {
	public class AssetManager : IAssetManager, IDisposable {
		private readonly List<Asset> Assets = new();
		private readonly Dictionary<string, UniTaskCompletionSource<Asset>> _pendingLoads = new();

		private static string PathToName(string path)
			=> "file://" + path.Replace("\\", "/");

		public async UniTask<IAsset> LoadFileAsync(string path, string by, IProgress<float> progress = null) {
			var n = PathToName(path);

			var asset = Assets.Find(a => a.Name == n);
			if (asset != null) {
				asset.AddUsedBy(by);
				return asset;
			}

			// If a concurrent load for the same path is in progress, wait for it
			if (_pendingLoads.TryGetValue(n, out var pending)) {
				var result = await pending.Task;
				result.AddUsedBy(by);
				return result;
			}

			var tcs = new UniTaskCompletionSource<Asset>();
			_pendingLoads[n] = tcs;

			try {
				var request = AssetBundle.LoadFromFileAsync(path);
				while (!request.isDone) {
					progress?.Report(request.progress);
					await UniTask.Yield();
				}

				var bundle = request.assetBundle;
				if (!bundle) {
					// Fallback: bundle may already be in Unity's memory (loaded externally or by a parallel session)
					var file = Path.GetFileName(path);
					bundle = AssetBundle.GetAllLoadedAssetBundles()
						.FirstOrDefault(b => b.name == file
							|| b.name.EndsWith("/" + file)
							|| b.name.EndsWith("\\" + file));
				}

				if (!bundle)
					throw new Exception($"Failed to load AssetBundle from path: {path}");

				asset = new Asset(bundle, n);
				asset.AddUsedBy(by);
				Assets.Add(asset);
				tcs.TrySetResult(asset);
				return asset;
			} catch (Exception ex) {
				tcs.TrySetException(ex);
				throw;
			} finally {
				_pendingLoads.Remove(n);
			}
		}

		public IAsset LoadFile(string path, string by) {
			var n = PathToName(path);

			var asset = Assets.Find(a => a.Name == n);
			if (asset != null) {
				asset.AddUsedBy(by);
				return asset;
			}

			var bundle = AssetBundle.LoadFromFile(path);
			if (!bundle)
				throw new Exception($"Failed to load AssetBundle from path: {path}");

			asset = new Asset(bundle, n);
			asset.AddUsedBy(by);

			Assets.Add(asset);
			return asset;
		}

		public void DetachFile(string path, string by) {
			var n = PathToName(path);

			var asset = Assets.Find(a => a.Name == n);
			if (asset == null)
				return;

			asset.RemoveUsedBy(by);

			if (asset.UsedBy.Length != 0)
				return;

			Assets.Remove(asset);
			asset.AssetBundle.Unload(true);
		}

		public IAsset AttachFile(AssetBundle ab, string path, string by) {
			var n = PathToName(path);

			var asset = Assets.Find(a => a.Name == n);
			if (asset != null) {
				asset.AddUsedBy(by);
				return asset;
			}

			asset = new Asset(ab, n);
			asset.AddUsedBy(by);

			Assets.Add(asset);
			return asset;
		}

		public void Dispose() {
			foreach (var asset in Assets)
				asset.AssetBundle.Unload(true);
			Assets.Clear();
		}
	}
}