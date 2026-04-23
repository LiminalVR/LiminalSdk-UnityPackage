using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Liminal.SDK.Serialization
{
    /// <summary>
    /// Stores references to assets within an application scene and allocates a unique identifier to each asset.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public class AssetLookup : MonoBehaviour, ISerializationCallbackReceiver, IEnumerable<KeyValuePair<int, UnityEngine.Object>>
    {
        private Dictionary<int, UnityEngine.Object> mIdToAsset = new Dictionary<int, UnityEngine.Object>();

        [SerializeField] private List<UnityEngine.Object> m_AssetRefs = null;
        [SerializeField, HideInInspector] private List<int> m_Ids = null;

        #region Editor API

#if UNITY_EDITOR
        private int mNextId = 10000;
        private readonly Dictionary<UnityEngine.Object, int> mAssetToId = new Dictionary<UnityEngine.Object, int>();

        /// <summary>
        /// Gets the id of the specified asset.
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public int GetId(UnityEngine.Object asset)
        {
            int id;
            if (mAssetToId.TryGetValue(asset, out id))
                return id;

            Debug.LogError("Asset id not found: " + asset, asset);
            return -1;
        }

        /// <summary>
        /// Adds an asset to the lookup and returns the id. If the asset already exists in the lookup, the asset is not added again,
        /// and the allocated id is returned.
        /// </summary>
        /// <param name="asset">The asset to add to the lookup.</param>
        /// <returns>The allocated id of the asset.</returns>
        public int AddAsset(UnityEngine.Object asset)
        {
            if (asset == null)
                return -1;

            m_AssetRefs = m_AssetRefs ?? new List<UnityEngine.Object>();
            m_Ids = m_Ids ?? new List<int>();

            int id;
            if (mAssetToId.TryGetValue(asset, out id))
                return id;

            id = AllocateId();
            m_AssetRefs.Add(asset);
            m_Ids.Add(id);

            mIdToAsset[id] = asset;
            mAssetToId[asset] = id;
            return id;
        }

        private int AllocateId()
        {
            return mNextId++;
        }
#endif
        #endregion

        /// <summary>
        /// Gets the asset with the specified id.
        /// </summary>
        /// <param name="id">The id of the asset.</param>
        /// <returns>The asset with the specified id.</returns>
        public UnityEngine.Object GetAsset(int id)
        {
            UnityEngine.Object obj;
            mIdToAsset.TryGetValue(id, out obj);
            return obj;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        #region ISerializationCallbackReceiver

        public void OnAfterDeserialize()
        {
            if (m_AssetRefs == null)
                return;

            mIdToAsset = mIdToAsset ?? new Dictionary<int, UnityEngine.Object>();
            mIdToAsset.Clear();

            int len = Mathf.Min(m_AssetRefs.Count, m_Ids.Count);
            for (int i = 0; i < len; ++i)
            {
                var asset = m_AssetRefs[i];
                var id = m_Ids[i];
                mIdToAsset[id] = asset;
            }
        }

        public void OnBeforeSerialize()
        {
            //
        }

        #endregion

        #region IEnumerable

        public IEnumerator<KeyValuePair<int, UnityEngine.Object>> GetEnumerator()
        {
            return mIdToAsset.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return mIdToAsset.GetEnumerator();
        }

        #endregion
#pragma warning restore CS1591
    }
}
