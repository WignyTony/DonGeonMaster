using UnityEngine;

namespace DonGeonMaster.MapGeneration
{
    /// <summary>
    /// Construit la couche de collision sol (BoxColliders en mode blockout,
    /// ou simple comptage en mode sols reels ou les MeshColliders du prefab suffisent).
    /// </summary>
    public static class MapCollisionBuilder
    {
        public const float GroundY = 0f;
        public const float GroundThickness = 0.1f;

        public struct Result
        {
            public int cellsFloor;
            public int cellsCorridor;
            public int cellsTotal;
        }

        public static Result Build(MapData map, MapGenConfig config, Transform parent, bool hasRealGround)
        {
            var result = new Result();

            var collGO = new GameObject("CollisionGround");
            var collisionRoot = collGO.transform;
            collisionRoot.SetParent(parent);

            float cs = config.cellSize;

            for (int x = 0; x < map.width; x++)
            {
                for (int y = 0; y < map.height; y++)
                {
                    var cell = map.cells[x, y];
                    if (cell.type != CellType.Sol && cell.type != CellType.Couloir) continue;

                    // En mode realGround, les tiles ont deja des MeshColliders actifs
                    if (hasRealGround) { result.cellsTotal++; continue; }

                    var cgo = new GameObject($"CollGround_{x}_{y}");
                    cgo.transform.SetParent(collisionRoot);
                    cgo.transform.position = new Vector3(x * cs, cell.floorHeight, y * cs);
                    cgo.layer = 0;

                    var box = cgo.AddComponent<BoxCollider>();
                    box.center = Vector3.down * (GroundThickness * 0.5f);
                    box.size = new Vector3(cs, GroundThickness, cs);

                    result.cellsTotal++;
                    if (cell.type == CellType.Sol) result.cellsFloor++;
                    else result.cellsCorridor++;
                }
            }

            Debug.Log($"[MapCollisionBuilder] Collision ground: {result.cellsTotal} cells " +
                $"(Sol:{result.cellsFloor} Couloir:{result.cellsCorridor}) at Y={GroundY}");

            return result;
        }
    }
}
