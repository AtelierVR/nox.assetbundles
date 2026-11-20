using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.AssetBundles;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.AssetBundles {
	public class AssetManager : IAssetManager, IDisposable {
		private readonly List<Asset> Assets = new();
		private readonly Dictionary<string, UniTaskCompletionSource<Asset>> _pendingLoads = new();

		private static string PathToName(string path)
			=> "file://" + path.Replace("\\", "/");

		/// <summary>
		/// Cached entry for a path, or <c>null</c> when there is none or when its bundle is no longer
		/// alive. A player build unloads every <see cref="AssetBundle"/> the editor had loaded, and the
		/// project enters Play Mode with Reload Domain disabled (<c>Nox.Editor.DisableDomainReload</c>),
		/// so this static list survives the build: handing a dead bundle back made Unity read a closed
		/// archive and kill the editor with "The file 'archive:/CAB-…' is corrupted! / Position out of
		/// bounds!". A stale entry is dropped so the bundle is loaded again from its file.
		/// </summary>
		private Asset Find(string name) {
			var asset = Assets.Find(a => a.Name == name);
			if (asset == null)
				return null;

			if (asset.AssetBundle)
				return asset;

			Assets.Remove(asset);
			return null;
		}

		/// <summary>
		/// Unloads every bundle this manager kept loaded and forgets it. Required after a player build:
		/// the bundles it unloaded are still referenced here, and reusing one reads a dead archive.
		/// </summary>
		public void UnloadAll() {
			foreach (var asset in Assets)
				if (asset.AssetBundle)
					try { asset.AssetBundle.Unload(true); }
					catch (Exception e) { Logger.LogException(e, tag: nameof(AssetManager)); }

			Assets.Clear();
		}

		/// <summary>
		/// Opens a bundle for reading, synchronously, on the calling thread.
		/// </summary>
		/// <remarks>
		/// <see cref="AssetBundle.LoadFromFileAsync"/> reads the archive on Unity's own loading thread, and
		/// when that read fails the error is raised off the main thread: Unity cannot even display its
		/// "file is corrupted" dialog and terminates the whole process (return code 0, no crash report).
		/// The asynchronous read only buys a progress value, while making the failure unrecoverable — a
		/// direct open returns a plain <c>null</c> that callers handle (the avatar and world loaders fall
		/// back to their error asset).
		/// </remarks>
		private static AssetBundle OpenBundle(string path) {
			if (!File.Exists(path))
				throw new FileNotFoundException($"AssetBundle not found: {path}", path);

			var bundle = AssetBundle.LoadFromFile(path);
			if (bundle)
				return bundle;

			// Fallback: bundle may already be in Unity's memory (loaded externally or by a parallel session)
			var file = Path.GetFileName(path);
			bundle = AssetBundle.GetAllLoadedAssetBundles()
				.FirstOrDefault(b => b.name == file
					|| b.name.EndsWith("/" + file)
					|| b.name.EndsWith("\\" + file));

			if (!bundle)
				Logger.LogError($"Failed to read AssetBundle '{path}' ({new FileInfo(path).Length} bytes)", tag: nameof(AssetManager));

			return bundle;
		}

#if UNITY_EDITOR
		/// <summary>
		/// A player build unloads every <see cref="AssetBundle"/> the editor had loaded, so nothing cached
		/// here is still readable afterwards.
		/// </summary>
		[UnityEditor.Callbacks.PostProcessBuild]
		private static void OnPlayerBuilt(UnityEditor.BuildTarget target, string path)
			=> GlobalAssetBundleManager.Reset();
#endif

		public async UniTask<IAsset> LoadFileAsync(string path, string by, IProgress<float> progress = null) {
			var n = PathToName(path);

			var asset = Find(n);
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
				progress?.Report(0f);
				await UniTask.Yield();

				var bundle = OpenBundle(path);
				if (!bundle)
					throw new Exception($"Failed to load AssetBundle from path: {path}");

				progress?.Report(1f);

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

			var asset = Find(n);
			if (asset != null) {
				asset.AddUsedBy(by);
				return asset;
			}

			var bundle = OpenBundle(path);
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

			if (asset.AssetBundle)
				asset.AssetBundle.Unload(true);
		}

		public IAsset AttachFile(AssetBundle ab, string path, string by) {
			var n = PathToName(path);

			var asset = Find(n);
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
				if (asset?.AssetBundle)
					asset.AssetBundle.Unload(true);
			Assets.Clear();
		}
	}
}
