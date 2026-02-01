using UnityEngine;
namespace Nox.AssetBundles {
	/// <summary>
	/// Represents an asset within an asset bundle.
	/// </summary>
	public interface IAsset {
		/// <summary>
		/// The name of the asset.
		/// </summary>
		public string Name { get; }
		
		/// <summary>
		/// The AssetBundle associated with this asset.
		/// </summary>
		public AssetBundle AssetBundle { get; }
		
		/// <summary>
		/// List of identifiers representing entities using this asset.
		/// </summary>
		public string[] UsedBy { get; }
	}
}