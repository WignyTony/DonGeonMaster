using System.Collections.Generic;
using UnityEngine;

namespace DonGeonMaster.MapGeneration.DebugTools
{
    /// <summary>
    /// Affiche la structure generee (MapData) en runtime avec des cubes colores (blockout)
    /// ou avec des materiaux realistes pour Sol/Couloir (mode sols reels).
    /// </summary>
    public class MapStructureDebugRenderer : MonoBehaviour
    {
        [Header("Dimensions")]
        [SerializeField] float wallHeight = 3f;
        [SerializeField] float floorThickness = 0.2f;
        [SerializeField] float cellGap = 0.15f;

        /// <summary>Mode sols reels : Sol/Couloir utilisent des mats realistes au lieu des couleurs debug.</summary>
        public bool useRealGround;

        // Blockout materials
        Material matFloor, matCorridor, matWall, matWater, matSpawn, matExit, matBoss;
        // Real ground materials
        Material matRealFloor, matRealCorridor;

        Transform structureRoot;
        Mesh cubeMesh;

        // Tracking des cellules rendues pour les logs
        public int RealGroundFloorCount { get; private set; }
        public int RealGroundCorridorCount { get; private set; }
        public int BlockoutCellCount { get; private set; }

        // Info par cellule pour le dump
        public struct CellRenderInfo
        {
            public int x, y;
            public string cellType;
            public string biome;
            public string renderMode; // "blockout" ou "realGround"
            public string materialName;
            public string objectName;
            public Vector3 worldPos;
            public Vector3 scale;
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

            // Blockout debug
            matFloor    = MakeMat(baseMat, new Color(0.22f, 0.45f, 0.22f), "DebugFloor");
            matCorridor = MakeMat(baseMat, new Color(0.55f, 0.50f, 0.35f), "DebugCorridor");
            matWall     = MakeMat(baseMat, new Color(0.28f, 0.18f, 0.12f), "DebugWall");
            matWater    = MakeMat(baseMat, new Color(0.15f, 0.35f, 0.70f), "DebugWater");
            matSpawn    = MakeMat(baseMat, new Color(0.10f, 1.00f, 0.20f), "DebugSpawn");
            matExit     = MakeMat(baseMat, new Color(1.00f, 0.15f, 0.15f), "DebugExit");
            matBoss     = MakeMat(baseMat, new Color(0.80f, 0.15f, 0.90f), "DebugBoss");

            // Sols reels — URP Lit avec couleurs/proprietes realistes
            matRealFloor = MakeRealMat(baseMat, new Color(0.62f, 0.55f, 0.42f), 0.15f, "RealFloor_Stone");
            matRealCorridor = MakeRealMat(baseMat, new Color(0.45f, 0.35f, 0.25f), 0.25f, "RealCorridor_Wood");
        }

        Material MakeMat(Material baseMat, Color color, string name)
        {
            var m = new Material(baseMat);
            m.name = name;
            m.SetColor("_Color", color);
            m.SetColor("_BaseColor", color);
            return m;
        }

        Material MakeRealMat(Material baseMat, Color color, float smoothness, string name)
        {
            var m = new Material(baseMat);
            m.name = name;
            m.SetColor("_Color", color);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", 0f);
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

            // Epaisseur du sol selon le mode
            float floorH = useRealGround ? 0.05f : floorThickness;

            Debug.Log($"[StructureRenderer] Render mode: {(useRealGround ? "SOLS REELS" : "BLOCKOUT")}");

            for (int x = 0; x < map.width; x++)
            {
                for (int y = 0; y < map.height; y++)
                {
                    var cell = map.cells[x, y];
                    if (cell.type == CellType.Vide) continue;

                    Vector3 pos = new Vector3(x * cs, 0, y * cs);
                    string renderMode = "blockout";
                    Material usedMat = null;
                    Vector3 usedScale = Vector3.zero;
                    string objName = "";

                    switch (cell.type)
                    {
                        case CellType.Sol:
                            if (useRealGround)
                            {
                                usedMat = matRealFloor;
                                renderMode = "realGround";
                                RealGroundFloorCount++;
                            }
                            else
                            {
                                usedMat = matFloor;
                                BlockoutCellCount++;
                            }
                            objName = $"Floor_{x}_{y}";
                            usedScale = new Vector3(block, floorH, block);
                            Block(pos, block, floorH, usedMat, true, objName);
                            RenderedFloorCount++;
                            break;

                        case CellType.Couloir:
                            if (useRealGround)
                            {
                                usedMat = matRealCorridor;
                                renderMode = "realGround";
                                RealGroundCorridorCount++;
                            }
                            else
                            {
                                usedMat = matCorridor;
                                BlockoutCellCount++;
                            }
                            objName = $"Corridor_{x}_{y}";
                            usedScale = new Vector3(block, floorH, block);
                            Block(pos + Vector3.up * 0.05f, block, floorH, usedMat, true, objName);
                            RenderedFloorCount++;
                            break;

                        case CellType.Mur:
                            usedMat = matWall;
                            objName = $"Wall_{x}_{y}";
                            usedScale = new Vector3(block, wallHeight, block);
                            Block(pos, block, wallHeight, usedMat, true, objName);
                            RenderedWallCount++;
                            BlockoutCellCount++;
                            break;

                        case CellType.Eau:
                            usedMat = matWater;
                            objName = $"Water_{x}_{y}";
                            usedScale = new Vector3(block, floorThickness * 0.6f, block);
                            Block(pos - Vector3.up * 0.1f, block, floorThickness * 0.6f, usedMat, true, objName);
                            RenderedFloorCount++;
                            BlockoutCellCount++;
                            break;
                    }
                    RenderedCellCount++;

                    // Enregistrer l'info de rendu
                    cellRenderInfos.Add(new CellRenderInfo
                    {
                        x = x, y = y,
                        cellType = cell.type.ToString(),
                        biome = cell.biome.ToString(),
                        renderMode = renderMode,
                        materialName = usedMat != null ? usedMat.name : "",
                        objectName = objName,
                        worldPos = pos,
                        scale = usedScale
                    });

                    // Marqueurs
                    if (cell.isSpawnPoint)
                    {
                        float mh = wallHeight * 1.5f;
                        Block(pos + Vector3.up * floorH, cs * 0.6f, 0.4f, matSpawn, false);
                        Block(pos + Vector3.up * (floorH + 0.4f), cs * 0.3f, mh, matSpawn, false);
                        HasSpawnMarker = true;
                    }
                    if (cell.isExit)
                    {
                        float mh = wallHeight * 1.5f;
                        Block(pos + Vector3.up * floorH, cs * 0.6f, 0.4f, matExit, false);
                        Block(pos + Vector3.up * (floorH + 0.4f), cs * 0.3f, mh, matExit, false);
                        HasExitMarker = true;
                    }
                }
            }

            foreach (var room in map.rooms)
            {
                if (room.isBossRoom)
                {
                    Vector3 p = new Vector3(room.center.x * cs, floorH, room.center.y * cs);
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
            mr.receiveShadows = useRealGround; // Receive shadows en mode reel pour meilleur rendu

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
            if (matRealFloor) Destroy(matRealFloor);
            if (matRealCorridor) Destroy(matRealCorridor);
        }
    }
}
