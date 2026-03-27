using UnityEngine;

namespace DonGeonMaster.MapGeneration
{
    public class MapCleanupService : MonoBehaviour
    {
        [SerializeField] Transform mapRoot;

        public Transform MapRoot
        {
            get
            {
                if (mapRoot == null)
                {
                    var go = GameObject.Find("GeneratedMap");
                    if (go == null)
                    {
                        go = new GameObject("GeneratedMap");
                    }
                    mapRoot = go.transform;
                }
                return mapRoot;
            }
            set => mapRoot = value;
        }

        public void ClearMap()
        {
            int count = MapRoot.childCount;
            for (int i = count - 1; i >= 0; i--)
            {
                DestroyImmediate(MapRoot.GetChild(i).gameObject);
            }
            Debug.Log($"[MapCleanup] {count} objets supprimés");
        }

        public Transform GetFreshRoot()
        {
            ClearMap();
            return MapRoot;
        }

        public int GetObjectCount()
        {
            return MapRoot.childCount;
        }

        public void ClearCategory(string categoryId)
        {
            int count = 0;
            for (int i = MapRoot.childCount - 1; i >= 0; i--)
            {
                var child = MapRoot.GetChild(i);
                if (child.name.StartsWith(categoryId + "_"))
                {
                    DestroyImmediate(child.gameObject);
                    count++;
                }
            }
            Debug.Log($"[MapCleanup] {count} objets '{categoryId}' supprimés");
        }
    }
}
