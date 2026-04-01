using System.Collections.Generic;
using UnityEngine;

namespace DonGeonMaster.MapGeneration.DebugTools
{
    /// <summary>
    /// Affiche la structure generee (MapData) en cubes colores (blockout)
    /// ou avec de vrais prefabs TileGround pour Sol/Couloir (mode sols reels).
    /// </summary>
    public class MapStructureDebugRenderer : MonoBehaviour
    {
        [Header("Dimensions")]
        [SerializeField] float wallHeight = 3f;
        [SerializeField] float floorThickness = 0.2f;
        [SerializeField] float cellGap = 0.15f;

        /// <summary>Mode sols reels : Sol/Couloir utilisent de vrais prefabs TileGround.</summary>
        public bool useRealGround;

        /// <summary>Prefabs de sol fournis par le controller depuis le registry "Sols".</summary>
        public List<GameObject> floorPrefabs;

        // Blockout materials
        Material matFloor, matCorridor, matWall, matWater, matSpawn, matExit, matBoss;

        Transform structureRoot;
        Mesh cubeMesh;
        System.Random tileRng;

        // Tracking
        public int RealGroundFloorCount { get; private set; }
        public int RealGroundCorridorCount { get; private set; }
        public int BlockoutCellCount { get; private set; }

        // Info par cellule pour le dump
        public struct CellRenderInfo
        {
            public int x, y;
            public string cellType;
            public string biome;
            public string renderMode;
            public string materialName;
            public string prefabName;
            public string meshName;
            public string objectName;
            public Vector3 worldPos;
            public Vector3 scale;
            public Vector3 rotation;
        }
        public List<CellRenderInfo> cellRenderInfos = new();

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
                Debug.LogError("[StructureRenderer] MapData ou config null");
                return;
            }

            var rootGO = new GameObject("StructureView");
            structureRoot = rootGO.transform;
            structureRoot.SetParent(transform);

            float cs = config.cellSize;
            float block = cs - cellGap;

            bool canRealGround = useRealGround && floorPrefabs != null && floorPrefabs.Count > 0;
            tileRng = new System.Random(config.seed);

            if (useRealGround && !canRealGround)
                Debug.LogWarning("[StructureRenderer] Mode sols reels demande mais aucun prefab de sol disponible — fallback blockout");

            Debug.Log($"[StructureRenderer] Render mode: {(canRealGround ? "SOLS REELS (TileGround prefabs)" : "BLOCKOUT")} " +
                $"| prefabs dispo: {(floorPrefabs != null ? floorPrefabs.Count : 0)}");

            for (int x = 0; x < map.width; x++)
            {
                for (int y = 0; y < map.height; y++)
                {
                    var cell = map.cells[x, y];
                    if (cell.type == CellType.Vide) continue;

                    Vector3 pos = new Vector3(x * cs, 0, y * cs);
                    string renderMode = "blockout";
                    string matName = "";
                    string prefabName = "";
                    string meshName = "";
                    string objName = "";
                    Vector3 usedScale = Vector3.zero;
                    Vector3 usedRot = Vector3.zero;

                    switch (cell.type)
                    {
                        case CellType.Sol:
                            if (canRealGround)
                            {
                                var info = PlaceTile(pos, cs, block, x, y, "Floor");
                                renderMode = "realGround_prefab";
                                matName = info.matName;
                                prefabName = info.prefabName;
                                meshName = info.meshName;
                                objName = info.objName;
                                usedScale = info.scale;
                                usedRot = info.rot;
                                RealGroundFloorCount++;
                            }
                            else
                            {
                                objName = $"Floor_{x}_{y}";
                                usedScale = new Vector3(block, floorThickness, block);
                                Block(pos, block, floorThickness, matFloor, true, objName);
                                matName = "DebugFloor";
                                BlockoutCellCount++;
                            }
                            RenderedFloorCount++;
                            break;

                        case CellType.Couloir:
                            if (canRealGround)
                            {
                                var info = PlaceTile(pos, cs, block, x, y, "Corridor");
                                renderMode = "realGround_prefab";
                                matName = info.matName;
                                prefabName = info.prefabName;
                                meshName = info.meshName;
                                objName = info.objName;
                                usedScale = info.scale;
                                usedRot = info.rot;
                                RealGroundCorridorCount++;
                            }
                            else
                            {
                                objName = $"Corridor_{x}_{y}";
                                usedScale = new Vector3(block, floorThickness, block);
                                Block(pos + Vector3.up * 0.05f, block, floorThickness, matCorridor, true, objName);
                                matName = "DebugCorridor";
                                BlockoutCellCount++;
                            }
                            RenderedFloorCount++;
                            break;

                        case CellType.Mur:
                            objName = $"Wall_{x}_{y}";
                            usedScale = new Vector3(block, wallHeight, block);
                            Block(pos, block, wallHeight, matWall, true, objName);
                            matName = "DebugWall";
                            RenderedWallCount++;
                            BlockoutCellCount++;
                            break;

                        case CellType.Eau:
                            objName = $"Water_{x}_{y}";
                            usedScale = new Vector3(block, floorThickness * 0.6f, block);
                            Block(pos - Vector3.up * 0.1f, block, floorThickness * 0.6f, matWater, true, objName);
                            matName = "DebugWater";
                            RenderedFloorCount++;
                            BlockoutCellCount++;
                            break;
                    }
                    RenderedCellCount++;

                    cellRenderInfos.Add(new CellRenderInfo
                    {
                        x = x, y = y,
                        cellType = cell.type.ToString(),
                        biome = cell.biome.ToString(),
                        renderMode = renderMode,
                        materialName = matName,
                        prefabName = prefabName,
                        meshName = meshName,
                        objectName = objName,
                        worldPos = pos,
                        scale = usedScale,
                        rotation = usedRot
                    });

                    // Marqueurs spawn/exit
                    float markerBase = canRealGround && (cell.type == CellType.Sol || cell.type == CellType.Couloir) ? 0.1f : floorThickness;
                    if (cell.isSpawnPoint)
                    {
                        Block(pos + Vector3.up * markerBase, cs * 0.6f, 0.4f, matSpawn, false);
                        Block(pos + Vector3.up * (markerBase + 0.4f), cs * 0.3f, wallHeight * 1.5f, matSpawn, false);
                        HasSpawnMarker = true;
                    }
                    if (cell.isExit)
                    {
                        Block(pos + Vector3.up * markerBase, cs * 0.6f, 0.4f, matExit, false);
                        Block(pos + Vector3.up * (markerBase + 0.4f), cs * 0.3f, wallHeight * 1.5f, matExit, false);
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

            Debug.Log($"[StructureRenderer] Rendu: {RenderedCellCount} cellules " +
                $"({RenderedFloorCount} sol, {RenderedWallCount} murs) " +
                $"Spawn:{HasSpawnMarker} Exit:{HasExitMarker} " +
                $"RealFloor:{RealGroundFloorCount} RealCorridor:{RealGroundCorridorCount} Blockout:{BlockoutCellCount}");
        }

        // ════════════════════════════════════════════
        //  TILE PLACEMENT (vrais prefabs)
        // ════════════════════════════════════════════

        struct TilePlaceResult
        {
            public string prefabName, matName, meshName, objName;
            public Vector3 scale, rot;
        }

        TilePlaceResult PlaceTile(Vector3 cellCenter, float cellSize, float blockSize, int cx, int cy, string prefix)
        {
            // Choisir un prefab aleatoire
            int idx = tileRng.Next(floorPrefabs.Count);
            var prefab = floorPrefabs[idx];
            string pName = prefab != null ? prefab.name : "null";

            var res = new TilePlaceResult { prefabName = pName };

            if (prefab == null)
            {
                Debug.LogWarning($"[StructureRenderer] Prefab sol null a l'index {idx}");
                return res;
            }

            // Instancier avec rotation structurelle (tiles sont modelisees a plat dans le FBX)
            var go = Instantiate(prefab, cellCenter, Quaternion.Euler(-90f, 0f, 0f), structureRoot);
            go.name = $"{prefix}_{cx}_{cy}_Tile";
            res.objName = go.name;

            // Mesurer les bounds pour rescaler a la taille de la cellule
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                // Pas de renderer — fallback scale fixe
                go.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
                res.scale = go.transform.localScale;
                res.rot = go.transform.rotation.eulerAngles;
                return res;
            }

            // Calculer les bounds combinees AVANT rescale (a scale 1)
            Bounds cb = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                cb.Encapsulate(renderers[i].bounds);

            // On veut que le sol remplisse blockSize sur XZ
            float meshSpanX = Mathf.Max(cb.size.x, 0.001f);
            float meshSpanZ = Mathf.Max(cb.size.z, 0.001f);
            float targetSize = blockSize;

            // Le facteur de scale pour couvrir la cellule
            float scaleFactorX = targetSize / meshSpanX;
            float scaleFactorZ = targetSize / meshSpanZ;
            float scaleFactor = Mathf.Min(scaleFactorX, scaleFactorZ);

            go.transform.localScale = Vector3.one * scaleFactor;

            // Recentrer sur la cellule (les bounds peuvent etre decentrees)
            var newBounds = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in renderers) newBounds.Encapsulate(r.bounds);
            Vector3 offset = cellCenter - newBounds.center;
            offset.y = 0; // Garder au sol
            go.transform.position += offset;

            // Desactiver les MeshColliders du prefab (le blockout fournit deja les colliders)
            foreach (var mc in go.GetComponentsInChildren<MeshCollider>())
                mc.enabled = false;

            // Ajouter un BoxCollider simple pour la physique
            var box = go.AddComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = new Vector3(targetSize / scaleFactor, 0.1f / scaleFactor, targetSize / scaleFactor);

            // Info pour les logs
            res.scale = go.transform.localScale;
            res.rot = go.transform.rotation.eulerAngles;

            var mainRend = renderers[0];
            if (mainRend.sharedMaterial != null)
                res.matName = mainRend.sharedMaterial.name;
            var mf = go.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
                res.meshName = mf.sharedMesh.name;

            return res;
        }

        // ════════════════════════════════════════════
        //  BLOCKOUT CUBE
        // ════════════════════════════════════════════

        void Block(Vector3 position, float size, float height, Material mat, bool addCollider, string goName = null)
        {
            var go = new GameObject(goName ?? "block");
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
            RealGroundFloorCount = 0;
            RealGroundCorridorCount = 0;
            BlockoutCellCount = 0;
            cellRenderInfos.Clear();
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
