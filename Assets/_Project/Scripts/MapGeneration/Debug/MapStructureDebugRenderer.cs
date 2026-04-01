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

        // Collision ground
        public const float CollisionGroundY = 0f;
        public const float CollisionGroundThickness = 0.1f;
        public int CollisionCellsFloor { get; private set; }
        public int CollisionCellsCorridor { get; private set; }
        public int CollisionCellsTotal { get; private set; }
        Transform collisionRoot;

        // Info par cellule pour le dump
        public struct CellRenderInfo
        {
            public int x, y;
            public string cellType, biome, renderMode;
            public string materialName, prefabName, meshName, objectName;
            public Vector3 worldPos, scale, rotation;
            // Tile diagnostic (rempli seulement en mode realGround_prefab)
            public Vector3 rawBoundsSize;     // bounds a identity/scale1
            public float rawX, rawY, rawZ;    // dims triees
            public float footprintW, footprintD, footprintH; // emprise retenue
            public float scaleFactorX, scaleFactorY, scaleFactorZ;
            public Vector3 finalBoundsSize;   // bounds apres scale+rotation
            public float aspectRatio;         // max(W,D)/min(W,D)
            public bool looksLikeStrip;       // ratio > 2
            public bool coversCellProperly;   // both dims >= blockSize * 0.9
            public string chosenUpAxis;       // "Y", "Z", ou "X" — axe d'epaisseur detecte
            public string appliedRotation;    // rotation appliquee pour coucher la tile
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

                    // Hauteur reelle de la cellule
                    float cellY = cell.IsWalkable ? cell.floorHeight : 0f;
                    Vector3 pos = new Vector3(x * cs, cellY, y * cs);

                    string renderMode = "blockout";
                    string matName = "";
                    string prefabName = "";
                    string meshName = "";
                    string objName = "";
                    Vector3 usedScale = Vector3.zero;
                    Vector3 usedRot = Vector3.zero;
                    TilePlaceResult? tileInfo = null;

                    switch (cell.type)
                    {
                        case CellType.Sol:
                            if (canRealGround)
                            {
                                var info = PlaceTile(pos, cs, block, x, y, "Floor");
                                tileInfo = info;
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
                                tileInfo = info;
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

                    var cri = new CellRenderInfo
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
                    };
                    if (tileInfo.HasValue)
                    {
                        var t = tileInfo.Value;
                        cri.rawBoundsSize = t.rawBoundsSize;
                        cri.rawX = t.rawX; cri.rawY = t.rawY; cri.rawZ = t.rawZ;
                        cri.footprintW = t.footprintW; cri.footprintD = t.footprintD; cri.footprintH = t.footprintH;
                        cri.scaleFactorX = t.sfX; cri.scaleFactorY = t.sfY; cri.scaleFactorZ = t.sfZ;
                        cri.finalBoundsSize = t.finalBoundsSize;
                        cri.aspectRatio = t.aspectRatio;
                        cri.looksLikeStrip = t.looksLikeStrip;
                        cri.coversCellProperly = t.coversCellProperly;
                        cri.chosenUpAxis = t.chosenUpAxis;
                        cri.appliedRotation = t.appliedRotation;
                    }
                    cellRenderInfos.Add(cri);

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

            // ── COLLISION GROUND : sol physique invisible a la vraie hauteur de chaque cellule ──
            var collGO = new GameObject("CollisionGround");
            collisionRoot = collGO.transform;
            collisionRoot.SetParent(structureRoot);

            for (int x = 0; x < map.width; x++)
            {
                for (int y = 0; y < map.height; y++)
                {
                    var cell = map.cells[x, y];
                    if (cell.type != CellType.Sol && cell.type != CellType.Couloir) continue;

                    var cgo = new GameObject($"CollGround_{x}_{y}");
                    cgo.transform.SetParent(collisionRoot);
                    // Position a la vraie hauteur de la cellule
                    cgo.transform.position = new Vector3(x * cs, cell.floorHeight, y * cs);
                    cgo.layer = 0;

                    var box = cgo.AddComponent<BoxCollider>();
                    box.center = Vector3.down * (CollisionGroundThickness * 0.5f);
                    box.size = new Vector3(cs, CollisionGroundThickness, cs);

                    CollisionCellsTotal++;
                    if (cell.type == CellType.Sol) CollisionCellsFloor++;
                    else CollisionCellsCorridor++;
                }
            }

            Debug.Log($"[StructureRenderer] Collision ground: {CollisionCellsTotal} cells " +
                $"(Sol:{CollisionCellsFloor} Couloir:{CollisionCellsCorridor}) at Y={CollisionGroundY}");

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
            public Vector3 rawBoundsSize, finalBoundsSize;
            public float rawX, rawY, rawZ;
            public float footprintW, footprintD, footprintH;
            public float sfX, sfY, sfZ;
            public float aspectRatio;
            public bool looksLikeStrip, coversCellProperly;
            public string chosenUpAxis, appliedRotation;
        }

        TilePlaceResult PlaceTile(Vector3 cellCenter, float cellSize, float blockSize, int cx, int cy, string prefix)
        {
            int idx = tileRng.Next(floorPrefabs.Count);
            var prefab = floorPrefabs[idx];
            string pName = prefab != null ? prefab.name : "null";
            var res = new TilePlaceResult { prefabName = pName };

            if (prefab == null)
            {
                Debug.LogWarning($"[StructureRenderer] Prefab sol null a l'index {idx}");
                return res;
            }

            // 1) Instancier a identity pour mesurer les bounds bruts
            var go = Instantiate(prefab, cellCenter, Quaternion.identity, structureRoot);
            go.name = $"{prefix}_{cx}_{cy}_Tile";
            res.objName = go.name;

            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                go.transform.localScale = Vector3.one * (blockSize / 0.06f);
                res.scale = go.transform.localScale;
                res.chosenUpAxis = "?"; res.appliedRotation = "none";
                return res;
            }

            Bounds cb = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                cb.Encapsulate(renderers[i].bounds);

            float rX = Mathf.Max(cb.size.x, 0.0001f);
            float rY = Mathf.Max(cb.size.y, 0.0001f);
            float rZ = Mathf.Max(cb.size.z, 0.0001f);
            res.rawBoundsSize = cb.size;
            res.rawX = rX; res.rawY = rY; res.rawZ = rZ;

            // 2) Detecter l'axe d'epaisseur (le plus petit) et les axes de surface
            //    upAxis = axe d'epaisseur natif du mesh
            //    surfA, surfB = les deux axes de surface
            float surfA, surfB, thickness;
            Quaternion layFlat;

            if (rY <= rX && rY <= rZ)
            {
                // Epaisseur sur Y → deja a plat dans le plan XZ
                surfA = rX; surfB = rZ; thickness = rY;
                layFlat = Quaternion.identity;
                res.chosenUpAxis = "Y"; res.appliedRotation = "identity";
            }
            else if (rZ <= rX && rZ <= rY)
            {
                // Epaisseur sur Z → surface dans XY, rotation -90 X pour coucher sur XZ
                surfA = rX; surfB = rY; thickness = rZ;
                layFlat = Quaternion.Euler(-90f, 0f, 0f);
                res.chosenUpAxis = "Z"; res.appliedRotation = "Euler(-90,0,0)";
            }
            else
            {
                // Epaisseur sur X → surface dans YZ, rotation 90 Z pour coucher sur XZ
                surfA = rY; surfB = rZ; thickness = rX;
                layFlat = Quaternion.Euler(0f, 0f, 90f);
                res.chosenUpAxis = "X"; res.appliedRotation = "Euler(0,0,90)";
            }

            // 3) Appliquer la rotation
            go.transform.rotation = layFlat;

            // 4) Remesurer les bounds apres rotation (a scale 1)
            Bounds rotBounds = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in renderers) rotBounds.Encapsulate(r.bounds);
            float worldSpanX = Mathf.Max(rotBounds.size.x, 0.0001f);
            float worldSpanZ = Mathf.Max(rotBounds.size.z, 0.0001f);

            // 5) Scale non-uniforme pour couvrir la cellule sur X et Z monde
            float sX = blockSize / worldSpanX;
            float sZ = blockSize / worldSpanZ;
            // Pour Y (epaisseur monde), prendre un scale moyen
            float worldSpanY = Mathf.Max(rotBounds.size.y, 0.0001f);
            float sY = (sX + sZ) * 0.5f;
            go.transform.localScale = new Vector3(sX, sY, sZ);
            res.sfX = sX; res.sfY = sY; res.sfZ = sZ;

            // Diagnostics
            res.footprintW = worldSpanX * sX;
            res.footprintD = worldSpanZ * sZ;
            res.footprintH = worldSpanY * sY;
            float minS = Mathf.Min(surfA, surfB);
            float maxS = Mathf.Max(surfA, surfB);
            res.aspectRatio = minS > 0.001f ? maxS / minS : 999f;
            res.looksLikeStrip = res.aspectRatio > 2f;

            // 6) Recentrer sur la cellule
            var nb = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in renderers) nb.Encapsulate(r.bounds);
            Vector3 off = cellCenter - nb.center;
            off.y = 0;
            go.transform.position += off;

            // 7) Poser au sol
            nb = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in renderers) nb.Encapsulate(r.bounds);
            go.transform.position += Vector3.up * (-nb.min.y);

            // Bounds finales
            var fb = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in renderers) fb.Encapsulate(r.bounds);
            res.finalBoundsSize = fb.size;
            res.coversCellProperly = fb.size.x >= blockSize * 0.9f && fb.size.z >= blockSize * 0.9f;

            // Desactiver MeshColliders (collision ground separe)
            foreach (var mc in go.GetComponentsInChildren<MeshCollider>())
                mc.enabled = false;

            // Info logs
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
            CollisionCellsFloor = 0;
            CollisionCellsCorridor = 0;
            CollisionCellsTotal = 0;
            collisionRoot = null;
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
