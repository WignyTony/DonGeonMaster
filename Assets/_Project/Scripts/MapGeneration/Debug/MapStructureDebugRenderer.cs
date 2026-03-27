using System.Collections.Generic;
using UnityEngine;

namespace DonGeonMaster.MapGeneration.Debug
{
    /// <summary>
    /// Affiche la structure generee (MapData) en runtime avec des cubes/quads colores.
    /// Aucune dependance aux assets decoratifs (Pandazole).
    /// Aucun Shader.Find : duplique le Default-Material Unity (garanti URP-safe).
    /// Lisible immediatement apres F5 meme sans aucun prefab charge.
    /// </summary>
    public class MapStructureDebugRenderer : MonoBehaviour
    {
        [Header("Dimensions des blocs")]
        [SerializeField] float wallHeight = 2.5f;
        [SerializeField] float floorThickness = 0.15f;
        [SerializeField] float markerHeight = 1.5f;
        [SerializeField] float cellGap = 0.05f; // espace entre cellules pour lisibilite

        // Materiaux crees a partir du Default-Material (jamais rose)
        Material matFloor, matCorridor, matWall, matWater, matSpawn, matExit, matBoss;

        // Parent de tous les objets rendus
        Transform structureRoot;

        // Stats de rendu pour validation
        public int RenderedCellCount { get; private set; }
        public int RenderedFloorCount { get; private set; }
        public int RenderedWallCount { get; private set; }
        public bool HasRendered { get; private set; }

        // Bounds de la structure pour cadrage camera
        public Vector3 StructureCenter { get; private set; }
        public Vector3 StructureSize { get; private set; }

        void Awake()
        {
            CreateMaterials();
        }

        /// <summary>
        /// Cree les materiaux debug en dupliquant le Default-Material Unity.
        /// Le Default-Material utilise toujours le bon shader pour le pipeline actif.
        /// Jamais de Shader.Find, jamais de rose.
        /// </summary>
        void CreateMaterials()
        {
            // Recuperer le materiau par defaut via un cube temporaire
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var baseMat = temp.GetComponent<Renderer>().sharedMaterial;
            DestroyImmediate(temp);

            matFloor    = CreateColorMat(baseMat, new Color(0.30f, 0.55f, 0.30f), "DebugFloor");
            matCorridor = CreateColorMat(baseMat, new Color(0.50f, 0.60f, 0.45f), "DebugCorridor");
            matWall     = CreateColorMat(baseMat, new Color(0.35f, 0.25f, 0.18f), "DebugWall");
            matWater    = CreateColorMat(baseMat, new Color(0.20f, 0.40f, 0.70f), "DebugWater");
            matSpawn    = CreateColorMat(baseMat, new Color(0.15f, 0.90f, 0.30f), "DebugSpawn");
            matExit     = CreateColorMat(baseMat, new Color(0.90f, 0.20f, 0.20f), "DebugExit");
            matBoss     = CreateColorMat(baseMat, new Color(0.80f, 0.20f, 0.80f), "DebugBoss");
        }

        Material CreateColorMat(Material baseMat, Color color, string name)
        {
            var m = new Material(baseMat);
            m.name = name;
            // Couvrir les deux conventions de nommage (Standard + URP)
            m.SetColor("_Color", color);
            m.SetColor("_BaseColor", color);
            return m;
        }

        /// <summary>
        /// Construit le rendu 3D de la structure a partir de MapData.
        /// Detruit l'ancien rendu, cree des cubes par cellule.
        /// </summary>
        public void Render(MapData map, MapGenConfig config)
        {
            Clear();
            HasRendered = false;
            RenderedCellCount = 0;
            RenderedFloorCount = 0;
            RenderedWallCount = 0;

            if (map == null || config == null)
            {
                UnityEngine.Debug.LogError("[StructureRenderer] MapData ou config null");
                return;
            }

            // Creer le parent
            var rootGO = new GameObject("StructureView");
            structureRoot = rootGO.transform;
            structureRoot.SetParent(transform);

            float cs = config.cellSize;
            float blockSize = cs - cellGap;

            // Pool de meshes : un seul mesh cube reutilise
            var cubeMesh = GetCubeMesh();

            for (int x = 0; x < map.width; x++)
            {
                for (int y = 0; y < map.height; y++)
                {
                    var cell = map.cells[x, y];
                    if (cell.type == CellType.Vide) continue;

                    Vector3 worldPos = new Vector3(x * cs, 0, y * cs);

                    switch (cell.type)
                    {
                        case CellType.Sol:
                            CreateBlock(worldPos, blockSize, floorThickness, matFloor, $"Sol_{x}_{y}", cubeMesh);
                            RenderedFloorCount++;
                            break;

                        case CellType.Couloir:
                            CreateBlock(worldPos, blockSize, floorThickness, matCorridor, $"Couloir_{x}_{y}", cubeMesh);
                            RenderedFloorCount++;
                            break;

                        case CellType.Mur:
                            CreateBlock(worldPos, blockSize, wallHeight, matWall, $"Mur_{x}_{y}", cubeMesh);
                            RenderedWallCount++;
                            break;

                        case CellType.Eau:
                            CreateBlock(worldPos, blockSize, floorThickness * 0.5f, matWater, $"Eau_{x}_{y}", cubeMesh);
                            RenderedFloorCount++;
                            break;
                    }

                    RenderedCellCount++;

                    // Marqueurs speciaux (par dessus le sol)
                    if (cell.isSpawnPoint)
                    {
                        CreateBlock(worldPos + Vector3.up * floorThickness,
                            cs * 0.4f, markerHeight, matSpawn, "SPAWN", cubeMesh);
                    }
                    if (cell.isExit)
                    {
                        CreateBlock(worldPos + Vector3.up * floorThickness,
                            cs * 0.4f, markerHeight, matExit, "EXIT", cubeMesh);
                    }
                }
            }

            // Marqueurs de boss rooms
            foreach (var room in map.rooms)
            {
                if (room.isBossRoom)
                {
                    Vector3 bossPos = new Vector3(room.center.x * cs, floorThickness, room.center.y * cs);
                    CreateBlock(bossPos, cs * 0.5f, markerHeight * 1.2f, matBoss, "BOSS", cubeMesh);
                }
            }

            // Calculer les bounds pour le cadrage camera
            float mapW = map.width * cs;
            float mapH = map.height * cs;
            StructureCenter = new Vector3(mapW * 0.5f, 0, mapH * 0.5f);
            StructureSize = new Vector3(mapW, wallHeight, mapH);

            HasRendered = RenderedCellCount > 0;
            UnityEngine.Debug.Log($"[StructureRenderer] {RenderedCellCount} cellules rendues " +
                $"({RenderedFloorCount} sol, {RenderedWallCount} murs)");
        }

        void CreateBlock(Vector3 position, float size, float height, Material mat, string name, Mesh mesh)
        {
            var go = new GameObject(name);
            go.transform.SetParent(structureRoot);
            go.transform.position = position + Vector3.up * (height * 0.5f);
            go.transform.localScale = new Vector3(size, height, size);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;

            // Collider pour les murs uniquement (block le joueur)
            if (height > floorThickness * 2)
            {
                go.AddComponent<BoxCollider>();
            }
        }

        Mesh GetCubeMesh()
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(temp);
            return mesh;
        }

        /// <summary>Detruit toute la structure rendue.</summary>
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
        }

        void OnDestroy()
        {
            // Nettoyer les materiaux runtime
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
