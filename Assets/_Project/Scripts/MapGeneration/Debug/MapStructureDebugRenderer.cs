using UnityEngine;

namespace DonGeonMaster.MapGeneration.DebugTools
{
    /// <summary>
    /// Affiche la structure generee (MapData) en runtime avec des cubes colores.
    /// Chaque type de cellule a un collider explicite pour etre praticable par le heros.
    /// Aucune dependance aux assets decoratifs.
    /// Duplique le Default-Material Unity (garanti URP-safe, jamais rose).
    /// </summary>
    public class MapStructureDebugRenderer : MonoBehaviour
    {
        [Header("Dimensions")]
        [SerializeField] float wallHeight = 3f;
        [SerializeField] float floorThickness = 0.2f;
        [SerializeField] float cellGap = 0.15f;

        Material matFloor, matCorridor, matWall, matWater, matSpawn, matExit, matBoss;
        Transform structureRoot;
        Mesh cubeMesh;

        public int RenderedCellCount { get; private set; }
        public int RenderedFloorCount { get; private set; }
        public int RenderedWallCount { get; private set; }
        public bool HasRendered { get; private set; }
        public bool HasSpawnMarker { get; private set; }
        public bool HasExitMarker { get; private set; }
        public Vector3 StructureCenter { get; private set; }
        public Vector3 StructureSize { get; private set; }

        void Awake()
        {
            CreateMaterials();
            cubeMesh = ExtractCubeMesh();
        }

        void CreateMaterials()
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var baseMat = temp.GetComponent<Renderer>().sharedMaterial;
            DestroyImmediate(temp);

            matFloor    = MakeMat(baseMat, new Color(0.22f, 0.45f, 0.22f), "DebugFloor");
            matCorridor = MakeMat(baseMat, new Color(0.55f, 0.50f, 0.35f), "DebugCorridor");
            matWall     = MakeMat(baseMat, new Color(0.28f, 0.18f, 0.12f), "DebugWall");
            matWater    = MakeMat(baseMat, new Color(0.15f, 0.35f, 0.70f), "DebugWater");
            matSpawn    = MakeMat(baseMat, new Color(0.10f, 1.00f, 0.20f), "DebugSpawn");
            matExit     = MakeMat(baseMat, new Color(1.00f, 0.15f, 0.15f), "DebugExit");
            matBoss     = MakeMat(baseMat, new Color(0.80f, 0.15f, 0.90f), "DebugBoss");
        }

        Material MakeMat(Material baseMat, Color color, string name)
        {
            var m = new Material(baseMat);
            m.name = name;
            m.SetColor("_Color", color);
            m.SetColor("_BaseColor", color);
            return m;
        }

        Mesh ExtractCubeMesh()
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(temp);
            return mesh;
        }

        // ════════════════════════════════════════════
        //  RENDER
        // ════════════════════════════════════════════

        public void Render(MapData map, MapGenConfig config)
        {
            Clear();

            if (map == null || config == null)
            {
                UnityEngine.Debug.LogError("[StructureRenderer] MapData ou config null");
                return;
            }

            var rootGO = new GameObject("StructureView");
            structureRoot = rootGO.transform;
            structureRoot.SetParent(transform);

            float cs = config.cellSize;
            float block = cs - cellGap;

            for (int x = 0; x < map.width; x++)
            {
                for (int y = 0; y < map.height; y++)
                {
                    var cell = map.cells[x, y];
                    if (cell.type == CellType.Vide) continue;

                    Vector3 pos = new Vector3(x * cs, 0, y * cs);

                    switch (cell.type)
                    {
                        case CellType.Sol:
                            Block(pos, block, floorThickness, matFloor, true);
                            RenderedFloorCount++;
                            break;

                        case CellType.Couloir:
                            Block(pos + Vector3.up * 0.05f, block, floorThickness, matCorridor, true);
                            RenderedFloorCount++;
                            break;

                        case CellType.Mur:
                            Block(pos, block, wallHeight, matWall, true);
                            RenderedWallCount++;
                            break;

                        case CellType.Eau:
                            // Eau : visible mais bloquante (le heros ne peut pas nager)
                            Block(pos - Vector3.up * 0.1f, block, floorThickness * 0.6f, matWater, true);
                            RenderedFloorCount++;
                            break;
                    }
                    RenderedCellCount++;

                    // Marqueurs : visuels uniquement, pas de collider
                    if (cell.isSpawnPoint)
                    {
                        float mh = wallHeight * 1.5f;
                        Block(pos + Vector3.up * floorThickness, cs * 0.6f, 0.4f, matSpawn, false);
                        Block(pos + Vector3.up * (floorThickness + 0.4f), cs * 0.3f, mh, matSpawn, false);
                        HasSpawnMarker = true;
                    }
                    if (cell.isExit)
                    {
                        float mh = wallHeight * 1.5f;
                        Block(pos + Vector3.up * floorThickness, cs * 0.6f, 0.4f, matExit, false);
                        Block(pos + Vector3.up * (floorThickness + 0.4f), cs * 0.3f, mh, matExit, false);
                        HasExitMarker = true;
                    }
                }
            }

            foreach (var room in map.rooms)
            {
                if (room.isBossRoom)
                {
                    Vector3 p = new Vector3(room.center.x * cs, floorThickness, room.center.y * cs);
                    Block(p, cs * 0.5f, wallHeight * 1.3f, matBoss, false);
                }
            }

            float mapW = map.width * cs;
            float mapH = map.height * cs;
            StructureCenter = new Vector3(mapW * 0.5f, 0, mapH * 0.5f);
            StructureSize = new Vector3(mapW, wallHeight, mapH);
            HasRendered = RenderedCellCount > 0;

            UnityEngine.Debug.Log($"[StructureRenderer] Rendu: {RenderedCellCount} cellules " +
                $"({RenderedFloorCount} sol, {RenderedWallCount} murs) " +
                $"Spawn:{HasSpawnMarker} Exit:{HasExitMarker}");
        }

        /// <param name="addCollider">true = BoxCollider pour collision physique</param>
        void Block(Vector3 position, float size, float height, Material mat, bool addCollider)
        {
            var go = new GameObject();
            go.transform.SetParent(structureRoot);
            go.transform.position = position + Vector3.up * (height * 0.5f);
            go.transform.localScale = new Vector3(size, height, size);
            go.AddComponent<MeshFilter>().sharedMesh = cubeMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            if (addCollider) go.AddComponent<BoxCollider>();
        }

        // ════════════════════════════════════════════
        //  CLEAR
        // ════════════════════════════════════════════

        public void Clear()
        {
            if (structureRoot != null)
            {
                DestroyImmediate(structureRoot.gameObject);
                structureRoot = null;
            }
            RenderedCellCount = 0;
            RenderedFloorCount = 0;
            RenderedWallCount = 0;
            HasRendered = false;
            HasSpawnMarker = false;
            HasExitMarker = false;
        }

        void OnDestroy()
        {
            if (matFloor) Destroy(matFloor);
            if (matCorridor) Destroy(matCorridor);
            if (matWall) Destroy(matWall);
            if (matWater) Destroy(matWater);
            if (matSpawn) Destroy(matSpawn);
            if (matExit) Destroy(matExit);
            if (matBoss) Destroy(matBoss);
        }
    }
}
