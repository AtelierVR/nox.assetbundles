using System.Collections.Generic;
using System.Linq;
using Nox.AssetBundles;
using UnityEngine;

namespace Nox.CCK.AssetBundles {
	public class Asset : IAsset {
		private readonly HashSet<string> usedBy = new HashSet<string>();

		public string Name { get; }

		public AssetBundle AssetBundle { get; }

		public Asset(AssetBundle assetBundle, string name) {
			AssetBundle = assetBundle;
			Name = name;
		}

		public string[] UsedBy
			=> usedBy.ToArray();

		public void AddUsedBy(string by)
			=> usedBy.Add(by);

		public void RemoveUsedBy(string by)
			=> usedBy.Remove(by);
	}
}