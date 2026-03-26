using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;
using DonGeonMaster.Core;
using DonGeonMaster.UI;
using DonGeonMaster.Effects;
using DonGeonMaster.Player;
using DonGeonMaster.Equipment;
using DonGeonMaster.Character;
using DonGeonMaster.Inventory;

public class ProjectSetup : EditorWindow
{
    // Cached materials
    private static Material stoneMat;
    private static Material floorMat;
    private static Material woodMat;
    private static Sprite woodSprite;
    private static Sprite parchmentSprite;

    [MenuItem("DonGeonMaster/Setup Project")]
    public static void SetupProject()
    {
        if (!EditorUtility.DisplayDialog("DonGeonMaster Setup",
            "Ceci va générer les scènes avec ambiance médiévale.\nTextures, matériaux et scènes seront créés.\n\nContinuer ?",
            "Oui", "Annuler"))
        {
            return;
        }

        RunSetup();

        EditorUtility.DisplayDialog("DonGeonMaster",
            "Setup médiéval terminé !\n\n" +
            "• Textures procédurales générées\n" +
            "• Scène MainMenu avec donjon 3D\n" +
            "• Scène Hub créée\n" +
            "• Build Settings configuré\n\n" +
            "Ouvre MainMenu et lance Play pour tester !",
            "OK");
    }

    /// <summary>
    /// Runs the full setup without any dialogs. Called by AutoSetup.
    /// </summary>
    public static void RunSetup()
    {
        EditorUtility.DisplayProgressBar("DonGeonMaster", "Configuration avatar GanzSe...", 0.03f);
        GanzSeAvatarSetup.ConfigureAvatar();

        EditorUtility.DisplayProgressBar("DonGeonMaster", "Génération des armures GanzSe...", 0.05f);
        DefaultArmorConfigs.EnsureArmorExists();

        EditorUtility.DisplayProgressBar("DonGeonMaster", "Génération des thumbnails 3D...", 0.08f);
        ArmorThumbnailGenerator.GenerateAll();
        ArmorThumbnailGenerator.AssignThumbnailsToArmor();

        EditorUtility.DisplayProgressBar("DonGeonMaster", "Génération des textures...", 0.1f);
        GenerateAllAssets();

        EditorUtility.DisplayProgressBar("DonGeonMaster", "Création scène MainMenu...", 0.4f);
        CreateMainMenuScene();

        EditorUtility.DisplayProgressBar("DonGeonMaster", "Création scène Hub...", 0.6f);
        CreateHubScene();

        EditorUtility.DisplayProgressBar("DonGeonMaster", "Création scène AnimationPreview...", 0.8f);
        CreateAnimationPreviewScene();

        EditorUtility.DisplayProgressBar("DonGeonMaster", "Création scène ScreenManager...", 0.83f);
        CreateScreenManagerScene();

        EditorUtility.DisplayProgressBar("DonGeonMaster", "Création scène ItemEditor...", 0.87f);
        CreateItemEditorScene();

        EditorUtility.DisplayProgressBar("DonGeonMaster", "Configuration Build Settings...", 0.9f);
        SetupBuildSettings();

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[DonGeonMaster] Setup médiéval complet !");
    }

    /// <summary>
    /// Returns true if the medieval setup has already been done.
    /// </summary>
    public static bool IsSetupDone()
    {
        return System.IO.File.Exists("Assets/_Project/Art/Textures/Stone_Wall.png")
            && System.IO.File.Exists("Assets/_Project/Scenes/MainMenu.unity")
            && System.IO.File.Exists("Assets/_Project/Materials/MAT_StoneWall.mat");
    }

    // =========================================================================
    // ASSET GENERATION
    // =========================================================================
    private static void GenerateAllAssets()
    {
        // Generate textures
        var stoneTex = ProceduralTextures.GenerateStoneTexture(512, 512);
        string stonePath = ProceduralTextures.SaveTexture(stoneTex, "Stone_Wall");
        stoneMat = ProceduralTextures.CreateLitMaterial(stonePath, "MAT_StoneWall", 0.15f);

        var floorTex = ProceduralTextures.GenerateFloorTexture(512, 512);
        string floorPath = ProceduralTextures.SaveTexture(floorTex, "Stone_Floor");
        floorMat = ProceduralTextures.CreateLitMaterial(floorPath, "MAT_StoneFloor", 0.2f);

        var woodTex = ProceduralTextures.GenerateWoodTexture(512, 128);
        string woodPath = ProceduralTextures.SaveTextureAsSprite(woodTex, "Wood_Plank");
        woodMat = ProceduralTextures.CreateLitMaterial(woodPath, "MAT_Wood", 0.3f);
        woodSprite = AssetDatabase.LoadAssetAtPath<Sprite>(woodPath);

        var parchTex = ProceduralTextures.GenerateParchmentTexture(512, 512);
        string parchPath = ProceduralTextures.SaveTextureAsSprite(parchTex, "Parchment");
        parchmentSprite = AssetDatabase.LoadAssetAtPath<Sprite>(parchPath);

        Debug.Log("[DonGeonMaster] All procedural textures and materials generated.");
    }

    // =========================================================================
    // MAIN MENU SCENE
    // =========================================================================
    private static void CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // -- Camera
        var camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        var cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.04f);
        cam.fieldOfView = 50;
        camObj.AddComponent<AudioListener>();

        // Add Universal Additional Camera Data for URP
        var camData = camObj.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        // Position camera inside the room, looking forward
        camObj.transform.position = new Vector3(0f, 3f, -1f);
        camObj.transform.rotation = Quaternion.Euler(12f, 0f, 0f);

        // -- Build dungeon room
        BuildDungeonRoom();

        // -- Torches (front pair near camera)
        CreateTorch(new Vector3(-5f, 3.5f, 2f), true);
        CreateTorch(new Vector3(5f, 3.5f, 2f), false);
        // -- Torches (back pair for depth)
        CreateTorch(new Vector3(-5f, 3.5f, 10f), true);
        CreateTorch(new Vector3(5f, 3.5f, 10f), false);

        // -- Dust particles
        CreateDustParticles(camObj.transform);

        // -- Post Processing
        CreatePostProcessing();

        // -- Managers
        var gmObj = new GameObject("GameManager");
        gmObj.AddComponent<GameManager>();

        var kbmObj = new GameObject("KeyBindingManager");
        kbmObj.AddComponent<KeyBindingManager>();

        // -- EventSystem
        var esObj = new GameObject("EventSystem");
        esObj.AddComponent<EventSystem>();
        esObj.AddComponent<InputSystemUIInputModule>();

        // -- UI Canvas (Overlay so it renders on top of the 3D scene)
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // -- Semi-transparent dark overlay (vignette-like)
        var overlay = CreateUIObject("DarkOverlay", canvasObj.transform);
        SetAnchorsStretch(overlay);
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.35f);

        // -- Title: "DonGeon Master"
        CreateTitle(canvasObj.transform);

        // -- Button container
        var buttonContainer = CreateUIObject("ButtonContainer", canvasObj.transform);
        var containerRect = buttonContainer.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(0, -40);
        containerRect.sizeDelta = new Vector2(420, 320);

        var vlg = buttonContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 25;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // -- Medieval buttons
        var playBtn = CreateMedievalButton("Jouer", buttonContainer.transform, 65);
        var customizeBtn = CreateMedievalButton("Customiser", buttonContainer.transform, 65);
        var animBtn = CreateMedievalButton("Animations", buttonContainer.transform, 65);
        var layoutBtn = CreateMedievalButton("Screenshots", buttonContainer.transform, 55);
        var itemEdBtn = CreateMedievalButton("Items", buttonContainer.transform, 65);
        var settingsBtn = CreateMedievalButton("Settings", buttonContainer.transform, 65);
        var quitBtn = CreateMedievalButton("Quitter", buttonContainer.transform, 65);

        // -- Settings Panel (hidden by default)
        var settingsPanel = CreateSettingsPanel(canvasObj.transform);
        settingsPanel.SetActive(false);

        // -- Character Showcase (3D pedestal + rotating characters)
        var showcaseObj = CreateCharacterShowcase();

        // -- Character navigation UI (below main buttons)
        var navContainer = CreateUIObject("CharacterNav", canvasObj.transform);
        var navRect = navContainer.GetComponent<RectTransform>();
        navRect.anchorMin = new Vector2(0.5f, 0.5f);
        navRect.anchorMax = new Vector2(0.5f, 0.5f);
        navRect.pivot = new Vector2(0.5f, 0.5f);
        navRect.anchoredPosition = new Vector2(0, -230);
        navRect.sizeDelta = new Vector2(420, 50);

        var navHlg = navContainer.AddComponent<HorizontalLayoutGroup>();
        navHlg.spacing = 15;
        navHlg.childAlignment = TextAnchor.MiddleCenter;
        navHlg.childControlWidth = true;
        navHlg.childControlHeight = true;
        navHlg.childForceExpandWidth = false;
        navHlg.childForceExpandHeight = false;

        // Prev button
        var prevBtn = CreateMedievalButton("<", navContainer.transform, 45);
        var prevLE = prevBtn.GetComponent<LayoutElement>();
        prevLE.preferredWidth = 60;

        // Character name label
        var nameObj = CreateUIObject("CharacterName", navContainer.transform);
        var nameLE = nameObj.AddComponent<LayoutElement>();
        nameLE.preferredWidth = 250;
        nameLE.preferredHeight = 45;
        var nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
        nameTMP.text = "Human";
        nameTMP.fontSize = 28;
        nameTMP.alignment = TextAlignmentOptions.Center;
        nameTMP.color = new Color(0.95f, 0.78f, 0.25f);
        nameTMP.fontStyle = FontStyles.Bold;

        // Next button
        var nextBtn = CreateMedievalButton(">", navContainer.transform, 45);
        var nextLE = nextBtn.GetComponent<LayoutElement>();
        nextLE.preferredWidth = 60;

        // -- Customizer Panel (face customization)
        var custResult = CreateCustomizerPanel(canvasObj.transform);
        var customizerComp = canvasObj.AddComponent<CharacterCustomizer>();
        var custSO = new SerializedObject(customizerComp);
        custSO.FindProperty("customizerPanel").objectReferenceValue = custResult.panel;
        custSO.FindProperty("facePreviewImage").objectReferenceValue = custResult.facePreview;

        // GanzSe prefab + URP material for preview character
        string ganzseP = "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/Modular Character/Modular Character Update 1.1/GanzSe Free Modular Character Update 1_1.prefab";
        var ganzseA = AssetDatabase.LoadAssetAtPath<GameObject>(ganzseP);
        if (ganzseA != null) custSO.FindProperty("ganzsePrefab").objectReferenceValue = ganzseA;
        var urpM = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/MAT_GanzSe_URP.mat");
        if (urpM != null) custSO.FindProperty("urpMaterial").objectReferenceValue = urpM;
        var custAnimCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/_Project/Art/Animations/AnimPreviewController.controller");
        if (custAnimCtrl != null) custSO.FindProperty("animController").objectReferenceValue = custAnimCtrl;

        // Type labels (6 — one per category)
        var tlProp = custSO.FindProperty("typeLabels");
        tlProp.arraySize = 6;
        for (int i = 0; i < 6; i++)
            tlProp.GetArrayElementAtIndex(i).objectReferenceValue = custResult.labels[i];
        // Color labels (4 — eyes, hair, beard, brows)
        var clProp = custSO.FindProperty("colorLabels");
        clProp.arraySize = custResult.colorLabels.Length;
        for (int i = 0; i < custResult.colorLabels.Length; i++)
            clProp.GetArrayElementAtIndex(i).objectReferenceValue = custResult.colorLabels[i];
        custSO.ApplyModifiedPropertiesWithoutUndo();

        // Wire customizer buttons
        UnityEventTools.AddPersistentListener(custResult.closeBtn.GetComponent<Button>().onClick, customizerComp.Close);
        UnityEventTools.AddPersistentListener(custResult.applyBtn.GetComponent<Button>().onClick, customizerComp.Apply);
        // Type buttons: 0=Eyes, 1=Hair, 2=Beard, 3=Brows, 4=Nose, 5=Ears
        UnityEventTools.AddPersistentListener(custResult.prevType[0].GetComponent<Button>().onClick, customizerComp.PrevEyeType);
        UnityEventTools.AddPersistentListener(custResult.nextType[0].GetComponent<Button>().onClick, customizerComp.NextEyeType);
        UnityEventTools.AddPersistentListener(custResult.prevType[1].GetComponent<Button>().onClick, customizerComp.PrevHairType);
        UnityEventTools.AddPersistentListener(custResult.nextType[1].GetComponent<Button>().onClick, customizerComp.NextHairType);
        UnityEventTools.AddPersistentListener(custResult.prevType[2].GetComponent<Button>().onClick, customizerComp.PrevBeardType);
        UnityEventTools.AddPersistentListener(custResult.nextType[2].GetComponent<Button>().onClick, customizerComp.NextBeardType);
        UnityEventTools.AddPersistentListener(custResult.prevType[3].GetComponent<Button>().onClick, customizerComp.PrevBrowsType);
        UnityEventTools.AddPersistentListener(custResult.nextType[3].GetComponent<Button>().onClick, customizerComp.NextBrowsType);
        UnityEventTools.AddPersistentListener(custResult.prevType[4].GetComponent<Button>().onClick, customizerComp.PrevNoseType);
        UnityEventTools.AddPersistentListener(custResult.nextType[4].GetComponent<Button>().onClick, customizerComp.NextNoseType);
        UnityEventTools.AddPersistentListener(custResult.prevType[5].GetComponent<Button>().onClick, customizerComp.PrevEarsType);
        UnityEventTools.AddPersistentListener(custResult.nextType[5].GetComponent<Button>().onClick, customizerComp.NextEarsType);
        // Color buttons (only first 4)
        UnityEventTools.AddPersistentListener(custResult.prevColor[0].GetComponent<Button>().onClick, customizerComp.PrevEyeColor);
        UnityEventTools.AddPersistentListener(custResult.nextColor[0].GetComponent<Button>().onClick, customizerComp.NextEyeColor);
        UnityEventTools.AddPersistentListener(custResult.prevColor[1].GetComponent<Button>().onClick, customizerComp.PrevHairColor);
        UnityEventTools.AddPersistentListener(custResult.nextColor[1].GetComponent<Button>().onClick, customizerComp.NextHairColor);
        UnityEventTools.AddPersistentListener(custResult.prevColor[2].GetComponent<Button>().onClick, customizerComp.PrevBeardColor);
        UnityEventTools.AddPersistentListener(custResult.nextColor[2].GetComponent<Button>().onClick, customizerComp.NextBeardColor);
        UnityEventTools.AddPersistentListener(custResult.prevColor[3].GetComponent<Button>().onClick, customizerComp.PrevBrowsColor);
        UnityEventTools.AddPersistentListener(custResult.nextColor[3].GetComponent<Button>().onClick, customizerComp.NextBrowsColor);

        // -- Wire MainMenuController
        var menuController = canvasObj.AddComponent<MainMenuController>();
        var menuSO = new SerializedObject(menuController);
        menuSO.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        menuSO.FindProperty("showcase").objectReferenceValue = showcaseObj.GetComponent<CharacterShowcase>();
        menuSO.FindProperty("characterNameLabel").objectReferenceValue = nameTMP;
        menuSO.FindProperty("characterNav").objectReferenceValue = navContainer;
        menuSO.FindProperty("customizer").objectReferenceValue = customizerComp;
        menuSO.ApplyModifiedPropertiesWithoutUndo();

        // Button events
        UnityEventTools.AddPersistentListener(
            playBtn.GetComponent<Button>().onClick, menuController.OnPlayClicked);
        UnityEventTools.AddPersistentListener(
            customizeBtn.GetComponent<Button>().onClick, menuController.OnCustomizeClicked);
        UnityEventTools.AddPersistentListener(
            animBtn.GetComponent<Button>().onClick, menuController.OnAnimPreviewClicked);
        UnityEventTools.AddPersistentListener(
            layoutBtn.GetComponent<Button>().onClick, menuController.OnScreenManagerClicked);
        UnityEventTools.AddPersistentListener(
            itemEdBtn.GetComponent<Button>().onClick, menuController.OnItemEditorClicked);
        UnityEventTools.AddPersistentListener(
            settingsBtn.GetComponent<Button>().onClick, menuController.OnSettingsClicked);
        UnityEventTools.AddPersistentListener(
            quitBtn.GetComponent<Button>().onClick, menuController.OnQuitClicked);
        UnityEventTools.AddPersistentListener(
            prevBtn.GetComponent<Button>().onClick, menuController.OnPrevCharacter);
        UnityEventTools.AddPersistentListener(
            nextBtn.GetComponent<Button>().onClick, menuController.OnNextCharacter);

        // -- Ambient light (very dim)
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.03f, 0.02f, 0.015f);

        // Fog - hides distant edges, adds depth
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.02f, 0.015f, 0.01f);
        RenderSettings.fogDensity = 0.055f;

        // Save
        string scenePath = "Assets/_Project/Scenes/MainMenu.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("[DonGeonMaster] Medieval MainMenu scene created.");
    }

    // =========================================================================
    // FBX IMPORT CONFIGURATION
    // =========================================================================
    // CHARACTER SHOWCASE
    // =========================================================================
    private static GameObject CreateCharacterShowcase()
    {
        // -- Pedestal (raised stone platform)
        var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pedestal.name = "Pedestal";
        pedestal.transform.position = new Vector3(3.5f, 0.15f, 4f);
        pedestal.transform.localScale = new Vector3(1.5f, 0.3f, 1.5f);
        SetMaterial(pedestal, floorMat);

        // -- Spotlight above pedestal
        var spotObj = new GameObject("ShowcaseSpotlight");
        spotObj.transform.position = new Vector3(3.5f, 4.5f, 4f);
        spotObj.transform.rotation = Quaternion.Euler(90f, 0, 0); // Point straight down
        var spot = spotObj.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = new Color(1f, 0.9f, 0.7f);
        spot.intensity = 3f;
        spot.range = 8f;
        spot.spotAngle = 40f;
        spot.shadows = LightShadows.Soft;

        // -- Spawn point
        var spawnPoint = new GameObject("ShowcaseSpawnPoint");
        spawnPoint.transform.position = new Vector3(3.5f, 0.3f, 4f);

        // -- CharacterShowcase component
        var showcaseObj = new GameObject("CharacterShowcase");
        var showcase = showcaseObj.AddComponent<CharacterShowcase>();

        // Wire showcase to GanzSe prefab + animator controller
        var showcaseSO = new SerializedObject(showcase);
        showcaseSO.FindProperty("spawnPoint").objectReferenceValue = spawnPoint.transform;

        string ganzsePath = "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/Modular Character/Modular Character Update 1.1/GanzSe Free Modular Character Update 1_1.prefab";
        var ganzseAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ganzsePath);
        if (ganzseAsset != null)
            showcaseSO.FindProperty("ganzsePrefab").objectReferenceValue = ganzseAsset;

        var animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/_Project/Art/Animations/AnimPreviewController.controller");
        if (animCtrl != null)
            showcaseSO.FindProperty("animController").objectReferenceValue = animCtrl;

        showcaseSO.ApplyModifiedPropertiesWithoutUndo();

        return showcaseObj;
    }

    // =========================================================================
    // DUNGEON ROOM
    // =========================================================================
    private static void BuildDungeonRoom()
    {
        float roomW = 14f;
        float roomH = 6f;
        float roomD = 20f;
        float wallThick = 0.3f;

        var dungeon = new GameObject("DungeonRoom");

        // Floor
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(dungeon.transform);
        floor.transform.position = new Vector3(0, 0, roomD / 2f);
        floor.transform.localScale = new Vector3(roomW, wallThick, roomD);
        SetMaterial(floor, floorMat);
        SetUVTiling(floor, 7f, 10f);

        // Ceiling
        var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.name = "Ceiling";
        ceiling.transform.SetParent(dungeon.transform);
        ceiling.transform.position = new Vector3(0, roomH, roomD / 2f);
        ceiling.transform.localScale = new Vector3(roomW, wallThick, roomD);
        SetMaterial(ceiling, stoneMat);
        SetUVTiling(ceiling, 7f, 10f);

        // Left wall
        var leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWall.name = "Wall_Left";
        leftWall.transform.SetParent(dungeon.transform);
        leftWall.transform.position = new Vector3(-roomW / 2f, roomH / 2f, roomD / 2f);
        leftWall.transform.localScale = new Vector3(wallThick, roomH, roomD);
        SetMaterial(leftWall, stoneMat);
        SetUVTiling(leftWall, 10f, 3f);

        // Right wall
        var rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWall.name = "Wall_Right";
        rightWall.transform.SetParent(dungeon.transform);
        rightWall.transform.position = new Vector3(roomW / 2f, roomH / 2f, roomD / 2f);
        rightWall.transform.localScale = new Vector3(wallThick, roomH, roomD);
        SetMaterial(rightWall, stoneMat);
        SetUVTiling(rightWall, 10f, 3f);

        // Back wall
        var backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backWall.name = "Wall_Back";
        backWall.transform.SetParent(dungeon.transform);
        backWall.transform.position = new Vector3(0, roomH / 2f, roomD);
        backWall.transform.localScale = new Vector3(roomW, roomH, wallThick);
        SetMaterial(backWall, stoneMat);
        SetUVTiling(backWall, 7f, 3f);

        // Front wall (behind camera, with large doorway arch)
        float doorWidth = 4f;
        float sideWidth = (roomW - doorWidth) / 2f;

        var frontWallLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frontWallLeft.name = "Wall_Front_Left";
        frontWallLeft.transform.SetParent(dungeon.transform);
        frontWallLeft.transform.position = new Vector3(-(doorWidth / 2f + sideWidth / 2f), roomH / 2f, -0.5f);
        frontWallLeft.transform.localScale = new Vector3(sideWidth, roomH, wallThick);
        SetMaterial(frontWallLeft, stoneMat);
        SetUVTiling(frontWallLeft, 2.5f, 3f);

        var frontWallRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frontWallRight.name = "Wall_Front_Right";
        frontWallRight.transform.SetParent(dungeon.transform);
        frontWallRight.transform.position = new Vector3(doorWidth / 2f + sideWidth / 2f, roomH / 2f, -0.5f);
        frontWallRight.transform.localScale = new Vector3(sideWidth, roomH, wallThick);
        SetMaterial(frontWallRight, stoneMat);
        SetUVTiling(frontWallRight, 2.5f, 3f);

        // Arch top above doorway
        var archTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        archTop.name = "Wall_Front_Top";
        archTop.transform.SetParent(dungeon.transform);
        archTop.transform.position = new Vector3(0, roomH - 0.6f, -0.5f);
        archTop.transform.localScale = new Vector3(doorWidth, 1.2f, wallThick);
        SetMaterial(archTop, stoneMat);

        // Pillars along the walls for detail
        for (int i = 0; i < 3; i++)
        {
            float z = 4f + i * 6f;
            CreatePillar(dungeon.transform, new Vector3(-roomW / 2f + 0.3f, roomH / 2f, z), roomH);
            CreatePillar(dungeon.transform, new Vector3(roomW / 2f - 0.3f, roomH / 2f, z), roomH);
        }
    }

    private static void CreatePillar(Transform parent, Vector3 position, float height)
    {
        var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pillar.name = "Pillar";
        pillar.transform.SetParent(parent);
        pillar.transform.position = position;
        pillar.transform.localScale = new Vector3(0.5f, height, 0.5f);
        SetMaterial(pillar, stoneMat);
    }

    // =========================================================================
    // TORCHES
    // =========================================================================
    private static void CreateTorch(Vector3 position, bool isLeft)
    {
        var torchRoot = new GameObject("Torch");
        torchRoot.transform.position = position;

        // Torch holder (cylinder)
        var holder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        holder.name = "TorchHolder";
        holder.transform.SetParent(torchRoot.transform);
        holder.transform.localPosition = new Vector3(0, -0.3f, 0);
        holder.transform.localScale = new Vector3(0.08f, 0.35f, 0.08f);
        holder.transform.localRotation = Quaternion.Euler(0, 0, isLeft ? 15f : -15f);
        if (woodMat != null) holder.GetComponent<Renderer>().sharedMaterial = woodMat;

        // Point light
        var lightObj = new GameObject("TorchLight");
        lightObj.transform.SetParent(torchRoot.transform);
        lightObj.transform.localPosition = new Vector3(0, 0.15f, 0);

        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.65f, 0.25f); // Warm amber
        light.intensity = 2f;
        light.range = 8f;
        light.shadows = LightShadows.Soft;

        // Flicker script
        lightObj.AddComponent<TorchFlicker>();

        // Fire particle system
        var fireObj = new GameObject("FireParticles");
        fireObj.transform.SetParent(torchRoot.transform);
        fireObj.transform.localPosition = new Vector3(0, 0.05f, 0);

        var ps = fireObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 0.8f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.6f, 0.1f, 1f),
            new Color(1f, 0.3f, 0.05f, 1f));
        main.gravityModifier = -0.3f;

        var emission = ps.emission;
        emission.rateOverTime = 20;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15;
        shape.radius = 0.03f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        var sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.7f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 0.3f, 0.05f), 0.5f),
                new GradientColorKey(new Color(0.3f, 0.05f, 0.0f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        // Renderer - use default particle material (additive)
        var psRenderer = fireObj.GetComponent<ParticleSystemRenderer>();
        var fireMat = new Material(Shader.Find("Particles/Standard Unlit"));
        fireMat.SetFloat("_Mode", 1); // Additive
        fireMat.color = Color.white;
        psRenderer.sharedMaterial = fireMat;
    }

    // =========================================================================
    // DUST PARTICLES
    // =========================================================================
    private static void CreateDustParticles(Transform cameraTransform)
    {
        var dustObj = new GameObject("DustParticles");
        dustObj.transform.position = new Vector3(0, 3f, 6f);

        var ps = dustObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new Color(1f, 0.9f, 0.7f, 0.3f);
        main.gravityModifier = -0.01f;

        var emission = ps.emission;
        emission.rateOverTime = 10;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(12f, 5f, 16f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.1f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.9f, 0.7f), 0f), new GradientColorKey(new Color(1f, 0.85f, 0.6f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.25f, 0.3f), new GradientAlphaKey(0.25f, 0.7f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;

        var psRenderer = dustObj.GetComponent<ParticleSystemRenderer>();
        var dustMat = new Material(Shader.Find("Particles/Standard Unlit"));
        dustMat.color = new Color(1f, 0.9f, 0.7f, 0.5f);
        psRenderer.sharedMaterial = dustMat;
    }

    // =========================================================================
    // POST PROCESSING
    // =========================================================================
    private static void CreatePostProcessing()
    {
        var ppObj = new GameObject("PostProcessVolume");
        var volume = ppObj.AddComponent<Volume>();
        volume.isGlobal = true;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();

        // Bloom
        var bloom = profile.Add<Bloom>();
        bloom.active = true;
        bloom.threshold.Override(0.8f);
        bloom.intensity.Override(1.5f);
        bloom.scatter.Override(0.7f);

        // Vignette
        var vignette = profile.Add<Vignette>();
        vignette.active = true;
        vignette.intensity.Override(0.45f);
        vignette.smoothness.Override(0.4f);
        vignette.color.Override(new Color(0.05f, 0.02f, 0f));

        // Color Adjustments - warm medieval tone
        var colorAdj = profile.Add<ColorAdjustments>();
        colorAdj.active = true;
        colorAdj.colorFilter.Override(new Color(1f, 0.9f, 0.75f));
        colorAdj.contrast.Override(15f);
        colorAdj.saturation.Override(-10f);

        // Save profile
        string profilePath = "Assets/_Project/Settings/MenuPostProcess.asset";
        System.IO.Directory.CreateDirectory("Assets/_Project/Settings");
        AssetDatabase.CreateAsset(profile, profilePath);
        volume.profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
    }

    // =========================================================================
    // CUSTOMIZER PANEL (Face customization)
    // =========================================================================
    private struct CustomizerResult
    {
        public GameObject panel;
        public GameObject closeBtn, applyBtn;
        public RawImage facePreview;
        public TextMeshProUGUI[] labels; // 6 type labels
        public TextMeshProUGUI[] colorLabels; // 4 color labels
        public GameObject[] prevType, nextType; // 6 each
        public GameObject[] prevColor, nextColor; // 4 each
    }

    private static CustomizerResult CreateCustomizerPanel(Transform parent)
    {
        var result = new CustomizerResult
        {
            labels = new TextMeshProUGUI[6],
            colorLabels = new TextMeshProUGUI[4],
            prevType = new GameObject[6],
            nextType = new GameObject[6],
            prevColor = new GameObject[4],
            nextColor = new GameObject[4]
        };

        var panel = CreateUIObject("CustomizerPanel", parent);
        SetAnchorsStretch(panel);
        result.panel = panel;

        var backdrop = CreateUIObject("Backdrop", panel.transform);
        SetAnchorsStretch(backdrop);
        backdrop.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);

        var main = CreateUIObject("MainPanel", panel.transform);
        var mainR = main.GetComponent<RectTransform>();
        mainR.anchorMin = new Vector2(0.5f, 0.5f); mainR.anchorMax = new Vector2(0.5f, 0.5f);
        mainR.sizeDelta = new Vector2(1100, 560);
        main.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        // Title
        var title = CreateUIObject("Title", main.transform);
        var titleR = title.GetComponent<RectTransform>();
        titleR.anchorMin = new Vector2(0.5f, 1); titleR.anchorMax = new Vector2(0.5f, 1);
        titleR.pivot = new Vector2(0.5f, 1);
        titleR.anchoredPosition = new Vector2(0, -15); titleR.sizeDelta = new Vector2(500, 45);
        var titleTMP = title.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "CUSTOMISER LE HEROS"; titleTMP.fontSize = 28;
        titleTMP.fontStyle = FontStyles.Bold; titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(0.3f, 0.8f, 0.9f);

        // Close button
        result.closeBtn = CreateMedievalButton("X", main.transform, 35);
        var cbR = result.closeBtn.GetComponent<RectTransform>();
        cbR.anchorMin = new Vector2(1, 1); cbR.anchorMax = new Vector2(1, 1);
        cbR.pivot = new Vector2(1, 1);
        cbR.anchoredPosition = new Vector2(-10, -10); cbR.sizeDelta = new Vector2(40, 35);

        Color textC = new Color(0.9f, 0.85f, 0.75f);
        string[] names = { "Yeux", "Cheveux", "Barbe", "Sourcils", "Nez", "Oreilles" };
        float startY = -75;

        for (int i = 0; i < 6; i++)
        {
            float y = startY - i * 55;

            // Row label
            var lbl = CreateUIObject("Lbl_" + names[i], main.transform);
            var lr = lbl.GetComponent<RectTransform>();
            lr.anchorMin = new Vector2(0, 1); lr.anchorMax = new Vector2(0, 1);
            lr.pivot = new Vector2(0, 1);
            lr.anchoredPosition = new Vector2(30, y); lr.sizeDelta = new Vector2(120, 40);
            var lt = lbl.AddComponent<TextMeshProUGUI>();
            lt.text = names[i]; lt.fontSize = 20; lt.color = textC;
            lt.alignment = TextAlignmentOptions.MidlineLeft;

            // < Type >
            result.prevType[i] = CreateMedievalButton("<", main.transform, 35);
            var ptR = result.prevType[i].GetComponent<RectTransform>();
            ptR.anchorMin = new Vector2(0, 1); ptR.anchorMax = new Vector2(0, 1);
            ptR.pivot = new Vector2(0, 1);
            ptR.anchoredPosition = new Vector2(160, y); ptR.sizeDelta = new Vector2(35, 35);

            var tl = CreateUIObject("TL_" + i, main.transform);
            var tlR = tl.GetComponent<RectTransform>();
            tlR.anchorMin = new Vector2(0, 1); tlR.anchorMax = new Vector2(0, 1);
            tlR.pivot = new Vector2(0, 1);
            tlR.anchoredPosition = new Vector2(200, y); tlR.sizeDelta = new Vector2(80, 40);
            result.labels[i] = tl.AddComponent<TextMeshProUGUI>();
            result.labels[i].text = "T1"; result.labels[i].fontSize = 18;
            result.labels[i].color = textC; result.labels[i].alignment = TextAlignmentOptions.Center;

            result.nextType[i] = CreateMedievalButton(">", main.transform, 35);
            var ntR = result.nextType[i].GetComponent<RectTransform>();
            ntR.anchorMin = new Vector2(0, 1); ntR.anchorMax = new Vector2(0, 1);
            ntR.pivot = new Vector2(0, 1);
            ntR.anchoredPosition = new Vector2(285, y); ntR.sizeDelta = new Vector2(35, 35);

            // < Color > (only for first 4: eyes, hair, beard, brows)
            if (i < 4)
            {
                result.prevColor[i] = CreateMedievalButton("<", main.transform, 35);
                var pcR = result.prevColor[i].GetComponent<RectTransform>();
                pcR.anchorMin = new Vector2(0, 1); pcR.anchorMax = new Vector2(0, 1);
                pcR.pivot = new Vector2(0, 1);
                pcR.anchoredPosition = new Vector2(360, y); pcR.sizeDelta = new Vector2(35, 35);

                var cl = CreateUIObject("CL_" + i, main.transform);
                var clR = cl.GetComponent<RectTransform>();
                clR.anchorMin = new Vector2(0, 1); clR.anchorMax = new Vector2(0, 1);
                clR.pivot = new Vector2(0, 1);
                clR.anchoredPosition = new Vector2(400, y); clR.sizeDelta = new Vector2(80, 40);
                var clT = cl.AddComponent<TextMeshProUGUI>();
                clT.text = "Couleur 1"; clT.fontSize = 16; clT.color = textC;
                clT.alignment = TextAlignmentOptions.Center;
                result.colorLabels[i] = clT;

                result.nextColor[i] = CreateMedievalButton(">", main.transform, 35);
                var ncR = result.nextColor[i].GetComponent<RectTransform>();
                ncR.anchorMin = new Vector2(0, 1); ncR.anchorMax = new Vector2(0, 1);
                ncR.pivot = new Vector2(0, 1);
                ncR.anchoredPosition = new Vector2(485, y); ncR.sizeDelta = new Vector2(35, 35);
            }
        }

        // Face preview (RawImage for RenderTexture)
        // Background frame
        var pvBg = CreateUIObject("PreviewBg", main.transform);
        var pvBgR = pvBg.GetComponent<RectTransform>();
        pvBgR.anchorMin = new Vector2(0.55f, 0.18f); pvBgR.anchorMax = new Vector2(0.95f, 0.80f);
        pvBgR.offsetMin = Vector2.zero; pvBgR.offsetMax = Vector2.zero;
        pvBg.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.08f, 1f);

        // RawImage (separate object, no Image conflict)
        var previewObj = CreateUIObject("FacePreview", pvBg.transform);
        SetAnchorsStretch(previewObj);
        var pvRect = previewObj.GetComponent<RectTransform>();
        pvRect.offsetMin = new Vector2(4, 4); pvRect.offsetMax = new Vector2(-4, -4);
        var rawImg = previewObj.AddComponent<RawImage>();
        rawImg.color = Color.white;
        result.facePreview = rawImg;

        // Apply button
        result.applyBtn = CreateMedievalButton("Appliquer", main.transform, 50);
        var abR = result.applyBtn.GetComponent<RectTransform>();
        abR.anchorMin = new Vector2(0.5f, 0); abR.anchorMax = new Vector2(0.5f, 0);
        abR.pivot = new Vector2(0.5f, 0);
        abR.anchoredPosition = new Vector2(0, 20); abR.sizeDelta = new Vector2(200, 50);

        panel.SetActive(false);
        return result;
    }

    // =========================================================================
    // SETTINGS PANEL (Parchment themed)
    // =========================================================================
    private static GameObject CreateSettingsPanel(Transform parent)
    {
        // Full-screen dark backdrop
        var panel = CreateUIObject("SettingsPanel", parent);
        SetAnchorsStretch(panel);

        var backdropImg = panel.AddComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.7f);

        // Parchment center panel
        var parchment = CreateUIObject("Parchment", panel.transform);
        var parchRect = parchment.GetComponent<RectTransform>();
        parchRect.anchorMin = new Vector2(0.5f, 0.5f);
        parchRect.anchorMax = new Vector2(0.5f, 0.5f);
        parchRect.pivot = new Vector2(0.5f, 0.5f);
        parchRect.sizeDelta = new Vector2(650, 680);

        var parchImg = parchment.AddComponent<Image>();
        if (parchmentSprite != null)
        {
            parchImg.sprite = parchmentSprite;
            parchImg.type = Image.Type.Simple;
        }
        else
        {
            parchImg.color = new Color(0.78f, 0.68f, 0.48f);
        }

        // Settings Title
        var stObj = CreateUIObject("SettingsTitle", parchment.transform);
        var stRect = stObj.GetComponent<RectTransform>();
        stRect.anchorMin = new Vector2(0.5f, 1f);
        stRect.anchorMax = new Vector2(0.5f, 1f);
        stRect.pivot = new Vector2(0.5f, 1f);
        stRect.anchoredPosition = new Vector2(0, -40);
        stRect.sizeDelta = new Vector2(500, 70);

        var stTMP = stObj.AddComponent<TextMeshProUGUI>();
        stTMP.text = "- Controles -";
        stTMP.fontSize = 42;
        stTMP.alignment = TextAlignmentOptions.Center;
        stTMP.color = new Color(0.25f, 0.15f, 0.05f); // Dark brown ink
        stTMP.fontStyle = FontStyles.Bold;

        // Bindings container
        var bindContainer = CreateUIObject("BindingsContainer", parchment.transform);
        var bcRect = bindContainer.GetComponent<RectTransform>();
        bcRect.anchorMin = new Vector2(0.5f, 0.5f);
        bcRect.anchorMax = new Vector2(0.5f, 0.5f);
        bcRect.pivot = new Vector2(0.5f, 0.5f);
        bcRect.anchoredPosition = new Vector2(0, 10);
        bcRect.sizeDelta = new Vector2(480, 400);

        var vlg = bindContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Binding rows — generated dynamically from KeyBindingManager.Actions
        var actions = KeyBindingManager.Actions;
        var allButtons = new System.Collections.Generic.List<Button>();
        var allLabels = new System.Collections.Generic.List<TextMeshProUGUI>();

        foreach (string action in actions)
        {
            string label = KeyBindingManager.GetActionLabel(action);
            string defaultKey = KeyBindingManager.Instance != null
                ? KeyBindingManager.Instance.GetBindingDisplayName(action)
                : action switch
                {
                    "MoveForward" => "W", "MoveBack" => "S", "MoveLeft" => "A", "MoveRight" => "D",
                    "Run" => "LeftShift", "Jump" => "Space", "Interact" => "E",
                    _ => "?"
                };
            var row = CreateParchmentBindingRow(label, defaultKey, bindContainer.transform);
            allButtons.Add(row.bindButton);
            allLabels.Add(row.bindLabel);
        }

        // Bottom buttons
        var bottomBtns = CreateUIObject("BottomButtons", parchment.transform);
        var bbRect = bottomBtns.GetComponent<RectTransform>();
        bbRect.anchorMin = new Vector2(0.5f, 0f);
        bbRect.anchorMax = new Vector2(0.5f, 0f);
        bbRect.pivot = new Vector2(0.5f, 0f);
        bbRect.anchoredPosition = new Vector2(0, 35);
        bbRect.sizeDelta = new Vector2(420, 55);

        var hlg = bottomBtns.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 30;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = false;

        // Fullscreen toggle row
        var fsRow = CreateUIObject("FullscreenRow", bindContainer.transform);
        fsRow.AddComponent<LayoutElement>().preferredHeight = 50;
        var fsHLG = fsRow.AddComponent<HorizontalLayoutGroup>();
        fsHLG.spacing = 15; fsHLG.childAlignment = TextAnchor.MiddleCenter;
        fsHLG.childControlWidth = true; fsHLG.childControlHeight = true;
        fsHLG.childForceExpandWidth = false;

        var fsLabel = CreateUIObject("FsLabel", fsRow.transform);
        fsLabel.AddComponent<LayoutElement>().preferredWidth = 200;
        var fsLabelTMP = fsLabel.AddComponent<TextMeshProUGUI>();
        fsLabelTMP.text = "Plein écran"; fsLabelTMP.fontSize = 22;
        fsLabelTMP.color = new Color(0.8f, 0.75f, 0.65f);
        fsLabelTMP.alignment = TextAlignmentOptions.MidlineRight;

        var fsToggleObj = CreateUIObject("FsToggle", fsRow.transform);
        fsToggleObj.AddComponent<LayoutElement>().preferredWidth = 50;

        var fsBg = CreateUIObject("Background", fsToggleObj.transform);
        SetAnchorsStretch(fsBg);
        fsBg.AddComponent<Image>().color = new Color(0.15f, 0.13f, 0.10f);

        var fsCheck = CreateUIObject("Checkmark", fsBg.transform);
        SetAnchorsStretch(fsCheck);
        var fsCheckImg = fsCheck.AddComponent<Image>();
        fsCheckImg.color = new Color(0.3f, 0.8f, 0.9f);

        var fsToggle = fsToggleObj.AddComponent<Toggle>();
        fsToggle.targetGraphic = fsBg.GetComponent<Image>();
        fsToggle.graphic = fsCheckImg;
        fsToggle.isOn = true;

        var resetBtn = CreateMedievalButton("Reset", bottomBtns.transform, 50);
        var backBtn = CreateMedievalButton("Retour", bottomBtns.transform, 50);

        // Wire SettingsMenuController with dynamic arrays
        var settingsController = panel.AddComponent<SettingsMenuController>();
        var sso = new SerializedObject(settingsController);

        // Set button array
        var btnsProp = sso.FindProperty("bindButtons");
        btnsProp.arraySize = allButtons.Count;
        for (int i = 0; i < allButtons.Count; i++)
            btnsProp.GetArrayElementAtIndex(i).objectReferenceValue = allButtons[i];

        // Set label array
        var lblsProp = sso.FindProperty("bindLabels");
        lblsProp.arraySize = allLabels.Count;
        for (int i = 0; i < allLabels.Count; i++)
            lblsProp.GetArrayElementAtIndex(i).objectReferenceValue = allLabels[i];

        sso.FindProperty("settingsPanel").objectReferenceValue = panel;
        sso.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(
            resetBtn.GetComponent<Button>().onClick, settingsController.OnResetClicked);
        UnityEventTools.AddPersistentListener(
            backBtn.GetComponent<Button>().onClick, settingsController.OnBackClicked);
        UnityEventTools.AddPersistentListener(
            fsToggle.onValueChanged, settingsController.OnFullscreenToggled);

        return panel;
    }

    // =========================================================================
    // HUB SCENE
    // =========================================================================
    private static void CreateHubScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // == CAMERA ==
        var camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        var cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.03f, 0.02f);
        cam.fieldOfView = 50;
        camObj.AddComponent<AudioListener>();
        var camData = camObj.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        // Initial camera position (CameraController will take over in Play)
        camObj.transform.position = new Vector3(0, 10, -7);
        camObj.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

        var camController = camObj.AddComponent<CameraController>();

        // == LIGHTING ==
        var lightObj = new GameObject("Directional Light");
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.85f, 0.6f); // Warm torch-like
        light.intensity = 0.8f;
        light.shadows = LightShadows.Soft;
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.08f, 0.06f, 0.04f);

        // Fog
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.04f, 0.03f, 0.02f);
        RenderSettings.fogDensity = 0.025f;

        // == GROUND ==
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(5, 1, 5); // 50x50 units
        SetMaterial(ground, floorMat);
        SetUVTiling(ground, 25f, 25f);

        // == BORDER WALLS (invisible colliders to keep player in bounds) ==
        float arenaSize = 24f;
        float wallHeight = 4f;
        CreateBorderWall("Wall_North", new Vector3(0, wallHeight / 2f, arenaSize), new Vector3(arenaSize * 2, wallHeight, 0.5f));
        CreateBorderWall("Wall_South", new Vector3(0, wallHeight / 2f, -arenaSize), new Vector3(arenaSize * 2, wallHeight, 0.5f));
        CreateBorderWall("Wall_East", new Vector3(arenaSize, wallHeight / 2f, 0), new Vector3(0.5f, wallHeight, arenaSize * 2));
        CreateBorderWall("Wall_West", new Vector3(-arenaSize, wallHeight / 2f, 0), new Vector3(0.5f, wallHeight, arenaSize * 2));

        // == PLAYER (GanzSe modular character) ==
        string ganzsePrefabPath = "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/Modular Character/Modular Character Update 1.1/GanzSe Free Modular Character Update 1_1.prefab";
        var ganzsePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ganzsePrefabPath);

        GameObject playerObj = (GameObject)PrefabUtility.InstantiatePrefab(ganzsePrefab);
        playerObj.name = "Player";
        playerObj.transform.position = Vector3.zero;

        FixGanzSeMaterials(playerObj);

        var cc = playerObj.AddComponent<CharacterController>();
        cc.height = 1.7f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0, 0.85f, 0);

        playerObj.AddComponent<PlayerController>();
        playerObj.AddComponent<ModularEquipmentManager>();

        DonGeonMaster.Character.GanzSeHelper.DisableAllArmor(playerObj);
        DonGeonMaster.UI.CharacterCustomizer.ApplyFaceCustomization(playerObj);
        playerObj.AddComponent<DonGeonMaster.Character.ApplyFaceOnStart>();

        // Use Animator with full player state machine
        var animator = playerObj.GetComponent<Animator>();
        if (animator == null) animator = playerObj.AddComponent<Animator>();
        animator.applyRootMotion = false;
        var playerCtrl = CreatePlayerAnimatorController();
        if (playerCtrl != null)
            animator.runtimeAnimatorController = playerCtrl;

        playerObj.AddComponent<DonGeonMaster.Player.PlayerAnimationBridge>();

        Debug.Log("[DonGeonMaster] Player created from GanzSe modular character.");

        playerObj.tag = "Player";

        // Wire camera target to player
        var camSO = new SerializedObject(camController);
        camSO.FindProperty("target").objectReferenceValue = playerObj.transform;
        camSO.ApplyModifiedPropertiesWithoutUndo();

        // == MANAGERS (backup — singletons will self-destroy if duplicates) ==
        var gmObj = new GameObject("GameManager");
        gmObj.AddComponent<GameManager>();

        var kbmObj = new GameObject("KeyBindingManager");
        kbmObj.AddComponent<KeyBindingManager>();

        // == EVENT SYSTEM ==
        var esObj = new GameObject("EventSystem");
        esObj.AddComponent<EventSystem>();
        esObj.AddComponent<InputSystemUIInputModule>();

        // == MINIMAL UI (menu button top-left) ==
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Small "Menu" button top-left
        var menuBtn = CreateMedievalButton("Menu", canvasObj.transform, 40);
        var menuRect = menuBtn.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0f, 1f);
        menuRect.anchorMax = new Vector2(0f, 1f);
        menuRect.pivot = new Vector2(0f, 1f);
        menuRect.anchoredPosition = new Vector2(20, -20);
        menuRect.sizeDelta = new Vector2(140, 40);

        var hubNav = canvasObj.AddComponent<HubNavigation>();
        UnityEventTools.AddPersistentListener(
            menuBtn.GetComponent<Button>().onClick, hubNav.ReturnToMainMenu);

        // == POST-PROCESSING ==
        var ppObj = new GameObject("PostProcessVolume");
        var volume = ppObj.AddComponent<Volume>();
        volume.isGlobal = true;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var bloom = profile.Add<Bloom>();
        bloom.active = true;
        bloom.threshold.Override(1f);
        bloom.intensity.Override(0.8f);

        var vignette = profile.Add<Vignette>();
        vignette.active = true;
        vignette.intensity.Override(0.3f);

        string profilePath = "Assets/_Project/Settings/HubPostProcess.asset";
        System.IO.Directory.CreateDirectory("Assets/_Project/Settings");
        AssetDatabase.CreateAsset(profile, profilePath);
        volume.profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);

        // == INVENTORY (on its own GO to avoid DontDestroyOnLoad destroying the Player) ==
        var invObj = new GameObject("PlayerInventory");
        invObj.AddComponent<PlayerInventory>();
        var invUI = CreateInventoryUI();

        // Wire inventoryUI to PlayerController
        var pc = playerObj.GetComponent<PlayerController>();
        if (pc != null && invUI != null)
        {
            var pcSO = new SerializedObject(pc);
            pcSO.FindProperty("inventoryUI").objectReferenceValue = invUI;
            pcSO.ApplyModifiedPropertiesWithoutUndo();
        }

        // == DEBUG: Load all armor + weapons into inventory ==
        var armorLoader = playerObj.AddComponent<DonGeonMaster.Debugging.DebugArmorLoader>();
        var allEquipGuids = new System.Collections.Generic.List<string>();
        allEquipGuids.AddRange(AssetDatabase.FindAssets("t:EquipmentData", new[] { "Assets/_Project/Configs/Armor" }));
        if (AssetDatabase.IsValidFolder("Assets/_Project/Configs/Weapons"))
            allEquipGuids.AddRange(AssetDatabase.FindAssets("t:EquipmentData", new[] { "Assets/_Project/Configs/Weapons" }));
        if (allEquipGuids.Count > 0)
        {
            var alSO = new SerializedObject(armorLoader);
            var alProp = alSO.FindProperty("armorsToLoad");
            alProp.arraySize = allEquipGuids.Count;
            for (int i = 0; i < allEquipGuids.Count; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(allEquipGuids[i]);
                var item = AssetDatabase.LoadAssetAtPath<EquipmentData>(path);
                alProp.GetArrayElementAtIndex(i).objectReferenceValue = item;
            }
            alSO.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[DonGeonMaster] DebugArmorLoader: {allEquipGuids.Count} items assigned ({allEquipGuids.Count} armor+weapons).");
        }

        // == SAVE ==
        string scenePath = "Assets/_Project/Scenes/Hub.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("[DonGeonMaster] Playable Hub scene created.");
    }

    // =========================================================================
    // GANZSE MATERIAL FIX (Standard → URP Lit)
    // =========================================================================
    private static void FixGanzSeMaterials(GameObject root)
    {
        // Convert the ORIGINAL Base Palette Material from Standard → URP Lit (in place)
        // This way all prefabs/meshes that reference it automatically get URP rendering
        // and color variations (different UV meshes) are preserved.
        string originalMatPath = "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Material/Base Palette Material.mat";
        var originalMat = AssetDatabase.LoadAssetAtPath<Material>(originalMatPath);

        if (originalMat != null && originalMat.shader.name == "Standard")
        {
            // Get the palette texture from the Standard shader property
            var paletteTex = originalMat.GetTexture("_MainTex");

            // Switch shader to URP Lit
            var urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader != null)
            {
                originalMat.shader = urpShader;
                // Remap Standard _MainTex → URP _BaseMap
                if (paletteTex != null)
                    originalMat.SetTexture("_BaseMap", paletteTex);
                originalMat.SetColor("_BaseColor", Color.white);
                originalMat.SetFloat("_Smoothness", 0.2f);
                originalMat.SetFloat("_Metallic", 0f);

                EditorUtility.SetDirty(originalMat);
                AssetDatabase.SaveAssets();
                Debug.Log("[DonGeonMaster] Converted Base Palette Material to URP Lit (in-place).");
            }
        }
        else if (originalMat != null)
        {
            Debug.Log("[DonGeonMaster] Base Palette Material already URP (" + originalMat.shader.name + ").");
        }

    }

    // =========================================================================
    // INVENTORY UI — Split-screen Dark Fantasy
    // =========================================================================
    private static InventoryUI CreateInventoryUI()
    {
        Color panelL = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        Color panelR = new Color(0.06f, 0.06f, 0.10f, 0.95f);
        Color textC = new Color(0.9f, 0.85f, 0.75f);
        Color cyan = new Color(0.3f, 0.8f, 0.9f);

        var canvasObj = new GameObject("InventoryCanvas");
        var cv = canvasObj.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;
        var sc = canvasObj.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        var root = CreateUIObject("InventoryPanel", canvasObj.transform);
        SetAnchorsStretch(root);
        var bd = CreateUIObject("BG", root.transform);
        SetAnchorsStretch(bd);
        bd.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

        // LEFT SIDE (45%)
        var lp = CreateUIObject("Left", root.transform);
        var lr = lp.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0, 0); lr.anchorMax = new Vector2(0.45f, 1);
        lr.offsetMin = new Vector2(15, 15); lr.offsetMax = new Vector2(-5, -15);
        lp.AddComponent<Image>().color = panelL;

        var tt = CreateUIObject("Title", lp.transform);
        var ttr = tt.GetComponent<RectTransform>();
        ttr.anchorMin = new Vector2(0, 1); ttr.anchorMax = new Vector2(0.7f, 1);
        ttr.pivot = new Vector2(0, 1); ttr.anchoredPosition = new Vector2(10, -8);
        ttr.sizeDelta = new Vector2(200, 35);
        var ttTMP = tt.AddComponent<TextMeshProUGUI>();
        ttTMP.text = "INVENTAIRE"; ttTMP.fontSize = 24; ttTMP.fontStyle = FontStyles.Bold;
        ttTMP.color = cyan;

        var cnt = CreateUIObject("Counter", lp.transform);
        var cntr = cnt.GetComponent<RectTransform>();
        cntr.anchorMin = new Vector2(1, 1); cntr.anchorMax = new Vector2(1, 1);
        cntr.pivot = new Vector2(1, 1); cntr.anchoredPosition = new Vector2(-10, -12);
        cntr.sizeDelta = new Vector2(80, 30);
        var cntTMP = cnt.AddComponent<TextMeshProUGUI>();
        cntTMP.text = "0/40"; cntTMP.fontSize = 16;
        cntTMP.alignment = TextAlignmentOptions.MidlineRight; cntTMP.color = textC;

        // Tabs
        var tb = CreateUIObject("Tabs", lp.transform);
        var tbr = tb.GetComponent<RectTransform>();
        tbr.anchorMin = new Vector2(0, 1); tbr.anchorMax = new Vector2(1, 1);
        tbr.pivot = new Vector2(0.5f, 1); tbr.anchoredPosition = new Vector2(0, -45);
        tbr.sizeDelta = new Vector2(-16, 28);
        var tbH = tb.AddComponent<HorizontalLayoutGroup>();
        tbH.spacing = 3; tbH.childControlWidth = true; tbH.childControlHeight = true;
        tbH.childForceExpandWidth = true;

        string[] tNames = { "Tout", "Armes", "Armure", "Acces", "Conso", "Mat" };
        var tabs = new Button[tNames.Length];
        for (int i = 0; i < tNames.Length; i++)
        {
            var t = CreateMedievalButton(tNames[i], tb.transform, 26);
            var tmp = t.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp) tmp.fontSize = 12;
            tabs[i] = t.GetComponent<Button>();
        }

        // ScrollView wrapping the grid
        var scrollObj = CreateUIObject("ScrollView", lp.transform);
        var scrollRect = scrollObj.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0.22f); scrollRect.anchorMax = new Vector2(1, 0.89f);
        scrollRect.offsetMin = new Vector2(8, 0); scrollRect.offsetMax = new Vector2(-8, -4);
        var scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30;
        scrollObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f); // Nearly invisible, needed for scroll input
        scrollObj.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;

        // Grid (content of scroll)
        var gr = CreateUIObject("Grid", scrollObj.transform);
        var grr = gr.GetComponent<RectTransform>();
        grr.anchorMin = new Vector2(0, 1); grr.anchorMax = new Vector2(1, 1);
        grr.pivot = new Vector2(0.5f, 1);
        grr.anchoredPosition = Vector2.zero;
        grr.sizeDelta = new Vector2(0, 0); // Will expand based on content
        var grd = gr.AddComponent<GridLayoutGroup>();
        grd.cellSize = new Vector2(72, 72); grd.spacing = new Vector2(5, 5);
        grd.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grd.constraintCount = 7;
        grd.padding = new RectOffset(3, 3, 3, 3);
        var csf = gr.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = grr;
        scroll.viewport = scrollRect;

        // Details
        var dp = CreateUIObject("Details", lp.transform);
        var dpr = dp.GetComponent<RectTransform>();
        dpr.anchorMin = Vector2.zero; dpr.anchorMax = new Vector2(1, 0.21f);
        dpr.offsetMin = new Vector2(6, 6); dpr.offsetMax = new Vector2(-6, -2);
        dp.AddComponent<Image>().color = new Color(0.1f, 0.08f, 0.06f, 0.7f);

        var dn = CreateUIObject("N", dp.transform); SetRect(dn, 0,0.65f, 0.7f,1, 6,0,0,-4);
        var dnT = dn.AddComponent<TextMeshProUGUI>();
        dnT.fontSize = 18; dnT.fontStyle = FontStyles.Bold; dnT.color = Color.white;

        var dd = CreateUIObject("D", dp.transform); SetRect(dd, 0,0.3f, 0.7f,0.65f, 6,0,0,0);
        var ddT = dd.AddComponent<TextMeshProUGUI>();
        ddT.fontSize = 13; ddT.fontStyle = FontStyles.Italic; ddT.color = new Color(0.7f,0.65f,0.55f);

        var ds = CreateUIObject("S", dp.transform); SetRect(ds, 0,0, 0.7f,0.3f, 6,4,0,0);
        var dsT = ds.AddComponent<TextMeshProUGUI>();
        dsT.fontSize = 14; dsT.color = textC; dsT.richText = true;

        var be = CreateMedievalButton("Equiper", dp.transform, 30);
        SetRect(be, 0.72f,0.55f, 0.98f,0.95f);
        var bdr = CreateMedievalButton("Jeter", dp.transform, 30);
        SetRect(bdr, 0.72f,0.05f, 0.98f,0.45f);

        // RIGHT SIDE (55%)
        var rp = CreateUIObject("Right", root.transform);
        var rpr = rp.GetComponent<RectTransform>();
        rpr.anchorMin = new Vector2(0.45f, 0); rpr.anchorMax = Vector2.one;
        rpr.offsetMin = new Vector2(5, 15); rpr.offsetMax = new Vector2(-15, -15);
        rp.AddComponent<Image>().color = panelR;

        var et = CreateUIObject("EqTitle", rp.transform);
        SetRect(et, 0,1, 1,1); et.GetComponent<RectTransform>().pivot = new Vector2(0.5f,1);
        et.GetComponent<RectTransform>().anchoredPosition = new Vector2(0,-8);
        et.GetComponent<RectTransform>().sizeDelta = new Vector2(0,35);
        var etT = et.AddComponent<TextMeshProUGUI>();
        etT.text = "EQUIPEMENT"; etT.fontSize = 24; etT.fontStyle = FontStyles.Bold;
        etT.alignment = TextAlignmentOptions.Center; etT.color = cyan;

        // Equipment slots container
        var esc = CreateUIObject("EqSlots", rp.transform);
        var escr = esc.GetComponent<RectTransform>();
        escr.anchorMin = new Vector2(0, 0.2f); escr.anchorMax = new Vector2(1, 0.95f);
        escr.offsetMin = new Vector2(8, 0); escr.offsetMax = new Vector2(-8, 0);

        // Silhouette
        var sil = CreateUIObject("Sil", esc.transform);
        SetRect(sil, 0.3f,0.1f, 0.7f,0.9f);
        sil.AddComponent<Image>().color = new Color(0.12f,0.10f,0.15f,0.4f);

        // Equipment slot factory
        var eqList = new List<EquipmentSlotUI>();
        // Load frame sprite for equipment slots
        var eqFrameSprite = ProceduralTextures.GenerateSlotFrame();
        var eqRaritySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Textures/RarityBorder.png");
        if (eqRaritySprite == null) eqRaritySprite = ProceduralTextures.GenerateRarityBorder();

        void EqSlot(string n, CharacterStandards.EquipmentSlot st, float x1, float y1, float x2, float y2)
        {
            var o = CreateUIObject("Eq_"+n, esc.transform);
            SetRect(o, x1,y1, x2,y2);
            var bgImg = o.AddComponent<Image>();
            if (eqFrameSprite != null) { bgImg.sprite = eqFrameSprite; bgImg.type = Image.Type.Simple; }
            bgImg.color = Color.white;

            // Placeholder icon (silhouette visible when slot is empty)
            string iconName = n == "Weap" ? "Weapon" : n == "Boots" ? "Feet" : n == "Belt" ? "Amulet" : n == "Arms" ? "Accessory" : n;
            var phObj = CreateUIObject("Ph", o.transform);
            SetRect(phObj, 0.12f,0.22f, 0.88f,0.88f);
            var phImg = phObj.AddComponent<Image>();
            var phSprite = ProceduralTextures.GenerateSlotIcon(iconName);
            if (phSprite != null) phImg.sprite = phSprite;
            phImg.color = Color.white;
            phImg.preserveAspect = true;

            // Item icon (hidden by default, shown when equipped)
            var ico = CreateUIObject("I", o.transform);
            SetRect(ico, 0.12f,0.22f, 0.88f,0.88f);
            var icoI = ico.AddComponent<Image>();
            icoI.preserveAspect = true;
            icoI.enabled = false;

            // Rarity glow overlay
            var glow = CreateUIObject("Glow", o.transform);
            SetRect(glow, 0,0, 1,1);
            var glowImg = glow.AddComponent<Image>();
            if (eqRaritySprite != null) { glowImg.sprite = eqRaritySprite; glowImg.type = Image.Type.Simple; }
            glowImg.raycastTarget = false;
            glowImg.enabled = false;

            var lbl = CreateUIObject("L", o.transform);
            SetRect(lbl, 0,0, 1,0.2f);
            var lblT = lbl.AddComponent<TextMeshProUGUI>();
            lblT.fontSize = 10; lblT.alignment = TextAlignmentOptions.Center;
            lblT.color = new Color(0.6f,0.55f,0.45f);
            var btn = o.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor = Color.white;
            cols.highlightedColor = new Color(1.15f,1.08f,1.0f);
            cols.pressedColor = new Color(0.7f,0.65f,0.6f);
            cols.fadeDuration = 0.08f;
            btn.colors = cols;
            var ui = o.AddComponent<EquipmentSlotUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("iconImage").objectReferenceValue = icoI;
            so.FindProperty("placeholderIcon").objectReferenceValue = phImg;
            so.FindProperty("borderImage").objectReferenceValue = bgImg;
            so.FindProperty("glowImage").objectReferenceValue = glowImg;
            so.FindProperty("slotLabel").objectReferenceValue = lblT;
            so.FindProperty("button").objectReferenceValue = btn;
            so.FindProperty("slot").intValue = (int)st;
            so.ApplyModifiedPropertiesWithoutUndo();
            eqList.Add(ui);
        }

        EqSlot("Head",  CharacterStandards.EquipmentSlot.Head,       0.40f,0.83f, 0.60f,0.98f);
        EqSlot("Weap",  CharacterStandards.EquipmentSlot.Weapon,     0.72f,0.58f, 0.95f,0.78f);
        EqSlot("Shield",CharacterStandards.EquipmentSlot.Shield,     0.05f,0.58f, 0.28f,0.78f);
        EqSlot("Chest", CharacterStandards.EquipmentSlot.Chest,      0.05f,0.38f, 0.28f,0.56f);
        EqSlot("Belt",  CharacterStandards.EquipmentSlot.Belt,        0.72f,0.38f, 0.95f,0.56f);
        EqSlot("Legs",  CharacterStandards.EquipmentSlot.Legs,       0.05f,0.16f, 0.28f,0.36f);
        EqSlot("Arms",  CharacterStandards.EquipmentSlot.Arms,       0.72f,0.16f, 0.95f,0.36f);
        EqSlot("Boots", CharacterStandards.EquipmentSlot.Feet,       0.05f,0.00f, 0.28f,0.15f);
        EqSlot("Back",  CharacterStandards.EquipmentSlot.Back,       0.40f,0.00f, 0.60f,0.15f);
        // Removed Acc2 — replaced by Belt and Arms slots above

        // Stats panel
        var stp = CreateUIObject("Stats", rp.transform);
        var stpr = stp.GetComponent<RectTransform>();
        stpr.anchorMin = Vector2.zero; stpr.anchorMax = new Vector2(1, 0.19f);
        stpr.offsetMin = new Vector2(8, 6); stpr.offsetMax = new Vector2(-8, -2);
        stp.AddComponent<Image>().color = new Color(0.1f,0.08f,0.06f,0.7f);
        var stG = stp.AddComponent<GridLayoutGroup>();
        stG.cellSize = new Vector2(130, 22); stG.spacing = new Vector2(8, 2);
        stG.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        stG.constraintCount = 3; stG.childAlignment = TextAnchor.MiddleCenter;
        stG.padding = new RectOffset(8,8,4,4);

        // Change grid to accommodate icon + text per stat
        stG.cellSize = new Vector2(150, 24);

        string[] sn = {"ATK","DEF","MAG","SPD","CRT","VIT"};
        var stTexts = new TextMeshProUGUI[6];
        for(int i=0;i<6;i++)
        {
            // Container for icon + text
            var row = CreateUIObject("S_"+sn[i], stp.transform);
            var rowHLG = row.AddComponent<HorizontalLayoutGroup>();
            rowHLG.spacing = 4;
            rowHLG.childControlWidth = false; rowHLG.childControlHeight = true;
            rowHLG.childForceExpandWidth = false;

            // Stat icon
            var icoObj = CreateUIObject("Ico", row.transform);
            icoObj.AddComponent<LayoutElement>().preferredWidth = 20;
            var icoImg = icoObj.AddComponent<Image>();
            var statSprite = ProceduralTextures.GenerateStatIcon(sn[i]);
            if (statSprite != null) { icoImg.sprite = statSprite; icoImg.preserveAspect = true; }

            // Stat text
            var txtObj = CreateUIObject("Txt", row.transform);
            txtObj.AddComponent<LayoutElement>().preferredWidth = 120;
            var st2 = txtObj.AddComponent<TextMeshProUGUI>();
            st2.text = sn[i]+": 0"; st2.fontSize = 15; st2.color = textC; st2.richText = true;
            stTexts[i] = st2;
        }

        var sp = stp.AddComponent<StatsPanel>();
        var spSO = new SerializedObject(sp);
        spSO.FindProperty("atkText").objectReferenceValue = stTexts[0];
        spSO.FindProperty("defText").objectReferenceValue = stTexts[1];
        spSO.FindProperty("magText").objectReferenceValue = stTexts[2];
        spSO.FindProperty("spdText").objectReferenceValue = stTexts[3];
        spSO.FindProperty("crtText").objectReferenceValue = stTexts[4];
        spSO.FindProperty("vitText").objectReferenceValue = stTexts[5];
        spSO.ApplyModifiedPropertiesWithoutUndo();

        // Slot prefab
        var pfb = CreateSlotPrefab();

        // Wire InventoryUI
        var invUI = canvasObj.AddComponent<InventoryUI>();
        var iso = new SerializedObject(invUI);
        iso.FindProperty("inventoryPanel").objectReferenceValue = root;
        iso.FindProperty("slotGrid").objectReferenceValue = gr.transform;
        iso.FindProperty("slotPrefab").objectReferenceValue = pfb;
        iso.FindProperty("slotCounterText").objectReferenceValue = cntTMP;
        iso.FindProperty("detailsPanel").objectReferenceValue = dp;
        iso.FindProperty("detailName").objectReferenceValue = dnT;
        iso.FindProperty("detailDescription").objectReferenceValue = ddT;
        iso.FindProperty("detailStats").objectReferenceValue = dsT;
        iso.FindProperty("btnEquip").objectReferenceValue = be.GetComponent<Button>();
        iso.FindProperty("btnDrop").objectReferenceValue = bdr.GetComponent<Button>();
        iso.FindProperty("statsPanel").objectReferenceValue = sp;
        var ftP = iso.FindProperty("filterTabs");
        ftP.arraySize = tabs.Length;
        for(int i=0;i<tabs.Length;i++) ftP.GetArrayElementAtIndex(i).objectReferenceValue = tabs[i];
        var eqP = iso.FindProperty("equipmentSlots");
        eqP.arraySize = eqList.Count;
        for(int i=0;i<eqList.Count;i++) eqP.GetArrayElementAtIndex(i).objectReferenceValue = eqList[i];
        iso.ApplyModifiedPropertiesWithoutUndo();

        // Close
        var cb = CreateMedievalButton("X", root.transform, 38);
        var cbr = cb.GetComponent<RectTransform>();
        cbr.anchorMin = new Vector2(1,1); cbr.anchorMax = new Vector2(1,1);
        cbr.pivot = new Vector2(1,1); cbr.anchoredPosition = new Vector2(-18,-18);
        cbr.sizeDelta = new Vector2(42,38);
        UnityEventTools.AddPersistentListener(cb.GetComponent<Button>().onClick, invUI.Close);

        root.SetActive(false);
        dp.SetActive(false);
        return invUI;
    }

    private static void SetRect(GameObject o, float ax1, float ay1, float ax2, float ay2, float ox1=0,float oy1=0,float ox2=0,float oy2=0)
    {
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(ax1,ay1); r.anchorMax = new Vector2(ax2,ay2);
        r.offsetMin = new Vector2(ox1,oy1); r.offsetMax = new Vector2(ox2,oy2);
    }

    private static GameObject CreateSlotPrefab()
    {
        // Generate UI textures
        var frameSprite = ProceduralTextures.GenerateSlotFrame();
        var raritySprite = ProceduralTextures.GenerateRarityBorder();
        var badgeSprite = ProceduralTextures.GenerateQuantityBadge();

        var s = new GameObject("InventorySlot");
        s.AddComponent<RectTransform>().sizeDelta = new Vector2(70,70);

        // Background — stone frame texture
        var bg = s.AddComponent<Image>();
        if (frameSprite != null) { bg.sprite = frameSprite; bg.type = Image.Type.Simple; }
        bg.color = Color.white;
        bg.raycastTarget = true;

        // Icon — centered with padding, preserveAspect
        var ico = CreateUIObject("I", s.transform);
        SetRect(ico, 0.12f,0.12f, 0.88f,0.88f);
        var icoI = ico.AddComponent<Image>();
        icoI.preserveAspect = true;
        icoI.enabled = false;

        // Rarity border overlay — colored frame
        var rb = CreateUIObject("RB", s.transform);
        SetRect(rb, 0,0, 1,1);
        var rbI = rb.AddComponent<Image>();
        if (raritySprite != null) { rbI.sprite = raritySprite; rbI.type = Image.Type.Simple; }
        rbI.raycastTarget = false;
        rbI.enabled = false;

        // Quantity badge + text — bottom-right
        var qBadge = CreateUIObject("QB", s.transform);
        SetRect(qBadge, 0.52f,-0.02f, 1.06f,0.26f);
        var qbI = qBadge.AddComponent<Image>();
        if (badgeSprite != null) { qbI.sprite = badgeSprite; qbI.type = Image.Type.Simple; }
        qbI.color = new Color(0,0,0,0.7f);
        qbI.raycastTarget = false;
        var qt = CreateUIObject("Q", qBadge.transform);
        SetRect(qt, 0.05f,0, 0.9f,1);
        var qtT = qt.AddComponent<TextMeshProUGUI>();
        qtT.fontSize = 11; qtT.alignment = TextAlignmentOptions.Center;
        qtT.color = new Color(1f,0.95f,0.85f);
        qtT.fontStyle = FontStyles.Bold;

        // Button — subtle hover effect on stone frame
        var btn = s.AddComponent<Button>();
        var cols = btn.colors;
        cols.normalColor = Color.white;
        cols.highlightedColor = new Color(1.2f,1.1f,1.0f);
        cols.pressedColor = new Color(0.7f,0.65f,0.6f);
        cols.fadeDuration = 0.08f;
        btn.colors = cols;

        var ui = s.AddComponent<InventorySlotUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("iconImage").objectReferenceValue = icoI;
        so.FindProperty("quantityText").objectReferenceValue = qtT;
        so.FindProperty("borderImage").objectReferenceValue = bg;
        so.FindProperty("rarityBorderImage").objectReferenceValue = rbI;
        so.FindProperty("button").objectReferenceValue = btn;
        so.ApplyModifiedPropertiesWithoutUndo();
        System.IO.Directory.CreateDirectory("Assets/_Project/Prefabs");
        var pfb = PrefabUtility.SaveAsPrefabAsset(s, "Assets/_Project/Prefabs/InventorySlot.prefab");
        Object.DestroyImmediate(s);
        return pfb;
    }

    private static void CreateBorderWall(string name, Vector3 pos, Vector3 scale)
    {
        var wall = new GameObject(name);
        wall.transform.position = pos;
        var col = wall.AddComponent<BoxCollider>();
        col.size = scale;
        // No renderer — invisible wall
    }

    // =========================================================================
    // BUILD SETTINGS
    // =========================================================================
    // =========================================================================
    // ANIMATION PREVIEW SCENE
    // =========================================================================
    private static UnityEditor.Animations.AnimatorController CreatePlayerAnimatorController()
    {
        string ctrlPath = "Assets/_Project/Art/Animations/PlayerAnimatorController.controller";
        System.IO.Directory.CreateDirectory("Assets/_Project/Art/Animations");

        // Load animation clips
        var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Idle.anim");
        var walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Walk_F_InPlace.anim");
        var runClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Run_F_InPlace.anim");
        var jumpClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Jump_B_InPlace.anim");
        var attackSlowClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_1_InPlace.anim");
        var attackFastClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_3_InPlace.anim");

        if (idleClip == null) { Debug.LogWarning("[Animator] Idle clip not found!"); return null; }

        var ctrl = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);

        // Add parameters
        ctrl.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("IsJumping", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("AttackSlow", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("AttackFast", AnimatorControllerParameterType.Trigger);

        var rootSM = ctrl.layers[0].stateMachine;

        // Create states
        var idleState = rootSM.AddState("Idle");
        idleState.motion = idleClip;
        rootSM.defaultState = idleState;

        var walkState = rootSM.AddState("Walk");
        walkState.motion = walkClip;

        var runState = rootSM.AddState("Run");
        runState.motion = runClip;

        var jumpState = rootSM.AddState("Jump");
        jumpState.motion = jumpClip;

        var attackSlowState = rootSM.AddState("AttackSlow");
        attackSlowState.motion = attackSlowClip;

        var attackFastState = rootSM.AddState("AttackFast");
        attackFastState.motion = attackFastClip;

        // Idle → Walk (IsMoving && !IsRunning)
        var t = idleState.AddTransition(walkState);
        t.hasExitTime = false;
        t.duration = 0.15f;
        t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsMoving");
        t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "IsRunning");

        // Idle → Run (IsMoving && IsRunning)
        t = idleState.AddTransition(runState);
        t.hasExitTime = false;
        t.duration = 0.15f;
        t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsMoving");
        t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsRunning");

        // Walk → Idle (!IsMoving)
        t = walkState.AddTransition(idleState);
        t.hasExitTime = false;
        t.duration = 0.15f;
        t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "IsMoving");

        // Walk → Run (IsRunning)
        t = walkState.AddTransition(runState);
        t.hasExitTime = false;
        t.duration = 0.15f;
        t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsRunning");

        // Run → Walk (!IsRunning && IsMoving)
        t = runState.AddTransition(walkState);
        t.hasExitTime = false;
        t.duration = 0.15f;
        t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "IsRunning");
        t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsMoving");

        // Run → Idle (!IsMoving)
        t = runState.AddTransition(idleState);
        t.hasExitTime = false;
        t.duration = 0.15f;
        t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "IsMoving");

        // Any → Jump (IsJumping)
        t = rootSM.AddAnyStateTransition(jumpState);
        t.hasExitTime = false;
        t.duration = 0.1f;
        t.canTransitionToSelf = false;
        t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsJumping");

        // Jump → Idle (!IsJumping — landed)
        t = jumpState.AddTransition(idleState);
        t.hasExitTime = false;
        t.duration = 0.15f;
        t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "IsJumping");

        // Any → AttackSlow (trigger — for slow/normal/very slow weapons)
        t = rootSM.AddAnyStateTransition(attackSlowState);
        t.hasExitTime = false;
        t.duration = 0.1f;
        t.canTransitionToSelf = false;
        t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "AttackSlow");

        // AttackSlow → Idle (exit time)
        t = attackSlowState.AddTransition(idleState);
        t.hasExitTime = true;
        t.exitTime = 0.9f;
        t.duration = 0.15f;

        // Any → AttackFast (trigger — for fast/very fast weapons)
        t = rootSM.AddAnyStateTransition(attackFastState);
        t.hasExitTime = false;
        t.duration = 0.1f;
        t.canTransitionToSelf = false;
        t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "AttackFast");

        // AttackFast → Idle (exit time)
        t = attackFastState.AddTransition(idleState);
        t.hasExitTime = true;
        t.exitTime = 0.9f;
        t.duration = 0.15f;

        AssetDatabase.SaveAssets();
        Debug.Log("[DonGeonMaster] Player AnimatorController created with 5 states.");
        return ctrl;
    }

    private static void CreateAnimationPreviewScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        var cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.06f, 0.10f);
        cam.fieldOfView = 45;
        cam.transform.position = new Vector3(0, 1.0f, -2.5f);
        cam.transform.LookAt(new Vector3(0, 0.9f, 0));
        camObj.AddComponent<AudioListener>();
        camObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = true;

        // Light
        var lightObj = new GameObject("Directional Light");
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.9f, 0.8f);
        light.intensity = 1.2f;
        lightObj.transform.rotation = Quaternion.Euler(40, -30, 0);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.15f, 0.12f, 0.10f);

        // Ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(2, 1, 2);
        if (floorMat != null) ground.GetComponent<Renderer>().sharedMaterial = floorMat;

        // EventSystem
        var esObj = new GameObject("EventSystem");
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // Canvas
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Animation name label (center bottom)
        var navContainer = CreateUIObject("NavContainer", canvasObj.transform);
        var navRect = navContainer.GetComponent<RectTransform>();
        navRect.anchorMin = new Vector2(0.5f, 0);
        navRect.anchorMax = new Vector2(0.5f, 0);
        navRect.pivot = new Vector2(0.5f, 0);
        navRect.anchoredPosition = new Vector2(0, 80);
        navRect.sizeDelta = new Vector2(600, 60);

        var navHLG = navContainer.AddComponent<HorizontalLayoutGroup>();
        navHLG.spacing = 15;
        navHLG.childAlignment = TextAnchor.MiddleCenter;
        navHLG.childControlWidth = true;
        navHLG.childControlHeight = true;
        navHLG.childForceExpandWidth = false;

        var prevBtn = CreateMedievalButton("<", navContainer.transform, 50);
        prevBtn.GetComponent<LayoutElement>().preferredWidth = 60;

        var nameObj = CreateUIObject("AnimName", navContainer.transform);
        nameObj.AddComponent<LayoutElement>().preferredWidth = 400;
        var nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
        nameTMP.text = "Idle";
        nameTMP.fontSize = 26;
        nameTMP.alignment = TextAlignmentOptions.Center;
        nameTMP.color = new Color(0.95f, 0.78f, 0.25f);
        nameTMP.fontStyle = FontStyles.Bold;

        var nextBtn = CreateMedievalButton(">", navContainer.transform, 50);
        nextBtn.GetComponent<LayoutElement>().preferredWidth = 60;

        // Weapon navigation (second row)
        var weaponNav = CreateUIObject("WeaponNav", canvasObj.transform);
        var wNavRect = weaponNav.GetComponent<RectTransform>();
        wNavRect.anchorMin = new Vector2(0.5f, 0);
        wNavRect.anchorMax = new Vector2(0.5f, 0);
        wNavRect.pivot = new Vector2(0.5f, 0);
        wNavRect.anchoredPosition = new Vector2(0, 145);
        wNavRect.sizeDelta = new Vector2(600, 50);

        var wNavHLG = weaponNav.AddComponent<HorizontalLayoutGroup>();
        wNavHLG.spacing = 15;
        wNavHLG.childAlignment = TextAnchor.MiddleCenter;
        wNavHLG.childControlWidth = true;
        wNavHLG.childControlHeight = true;
        wNavHLG.childForceExpandWidth = false;

        var wPrevBtn = CreateMedievalButton("<", weaponNav.transform, 45);
        wPrevBtn.GetComponent<LayoutElement>().preferredWidth = 60;

        var wNameObj = CreateUIObject("WeaponName", weaponNav.transform);
        wNameObj.AddComponent<LayoutElement>().preferredWidth = 400;
        var wNameTMP = wNameObj.AddComponent<TextMeshProUGUI>();
        wNameTMP.text = "Aucune arme";
        wNameTMP.fontSize = 22;
        wNameTMP.alignment = TextAlignmentOptions.Center;
        wNameTMP.color = new Color(0.8f, 0.75f, 0.65f);

        var wNextBtn = CreateMedievalButton(">", weaponNav.transform, 45);
        wNextBtn.GetComponent<LayoutElement>().preferredWidth = 60;

        // Armor Slot navigation (third row)
        var armorSlotNav = CreateUIObject("ArmorSlotNav", canvasObj.transform);
        var asNavRect = armorSlotNav.GetComponent<RectTransform>();
        asNavRect.anchorMin = new Vector2(0.5f, 0);
        asNavRect.anchorMax = new Vector2(0.5f, 0);
        asNavRect.pivot = new Vector2(0.5f, 0);
        asNavRect.anchoredPosition = new Vector2(0, 210);
        asNavRect.sizeDelta = new Vector2(600, 50);

        var asHLG = armorSlotNav.AddComponent<HorizontalLayoutGroup>();
        asHLG.spacing = 15;
        asHLG.childAlignment = TextAnchor.MiddleCenter;
        asHLG.childControlWidth = true;
        asHLG.childControlHeight = true;
        asHLG.childForceExpandWidth = false;

        var asPrevBtn = CreateMedievalButton("<", armorSlotNav.transform, 45);
        asPrevBtn.GetComponent<LayoutElement>().preferredWidth = 60;

        var asNameObj = CreateUIObject("SlotName", armorSlotNav.transform);
        asNameObj.AddComponent<LayoutElement>().preferredWidth = 400;
        var asNameTMP = asNameObj.AddComponent<TextMeshProUGUI>();
        asNameTMP.text = "Casque";
        asNameTMP.fontSize = 22;
        asNameTMP.alignment = TextAlignmentOptions.Center;
        asNameTMP.color = new Color(0.7f, 0.85f, 0.65f);
        asNameTMP.fontStyle = FontStyles.Bold;

        var asNextBtn = CreateMedievalButton(">", armorSlotNav.transform, 45);
        asNextBtn.GetComponent<LayoutElement>().preferredWidth = 60;

        // Armor Piece navigation (fourth row)
        var armorPieceNav = CreateUIObject("ArmorPieceNav", canvasObj.transform);
        var apNavRect = armorPieceNav.GetComponent<RectTransform>();
        apNavRect.anchorMin = new Vector2(0.5f, 0);
        apNavRect.anchorMax = new Vector2(0.5f, 0);
        apNavRect.pivot = new Vector2(0.5f, 0);
        apNavRect.anchoredPosition = new Vector2(0, 275);
        apNavRect.sizeDelta = new Vector2(600, 50);

        var apHLG = armorPieceNav.AddComponent<HorizontalLayoutGroup>();
        apHLG.spacing = 15;
        apHLG.childAlignment = TextAnchor.MiddleCenter;
        apHLG.childControlWidth = true;
        apHLG.childControlHeight = true;
        apHLG.childForceExpandWidth = false;

        var apPrevBtn = CreateMedievalButton("<", armorPieceNav.transform, 45);
        apPrevBtn.GetComponent<LayoutElement>().preferredWidth = 60;

        var apNameObj = CreateUIObject("PieceName", armorPieceNav.transform);
        apNameObj.AddComponent<LayoutElement>().preferredWidth = 400;
        var apNameTMP = apNameObj.AddComponent<TextMeshProUGUI>();
        apNameTMP.text = "Aucun";
        apNameTMP.fontSize = 20;
        apNameTMP.alignment = TextAlignmentOptions.Center;
        apNameTMP.color = new Color(0.7f, 0.85f, 0.65f);

        var apNextBtn = CreateMedievalButton(">", armorPieceNav.transform, 45);
        apNextBtn.GetComponent<LayoutElement>().preferredWidth = 60;

        // Return button
        var returnBtn = CreateMedievalButton("Retour au menu", canvasObj.transform, 45);
        var retRect = returnBtn.GetComponent<RectTransform>();
        retRect.anchorMin = new Vector2(0.5f, 0);
        retRect.anchorMax = new Vector2(0.5f, 0);
        retRect.pivot = new Vector2(0.5f, 0);
        retRect.anchoredPosition = new Vector2(0, 20);
        retRect.sizeDelta = new Vector2(250, 45);

        // Title
        var titleObj = CreateUIObject("Title", canvasObj.transform);
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1);
        titleRect.anchorMax = new Vector2(0.5f, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -20);
        titleRect.sizeDelta = new Vector2(600, 50);
        var titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "ANIMATION PREVIEW";
        titleTMP.fontSize = 32;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(0.3f, 0.8f, 0.9f);

        // Weapon Position Tool (right side panel)
        var toolPanel = CreateUIObject("ToolPanel", canvasObj.transform);
        var tpRect = toolPanel.GetComponent<RectTransform>();
        tpRect.anchorMin = new Vector2(1, 0.15f);
        tpRect.anchorMax = new Vector2(1, 0.85f);
        tpRect.pivot = new Vector2(1, 0.5f);
        tpRect.anchoredPosition = new Vector2(-10, 0);
        tpRect.sizeDelta = new Vector2(220, 0);
        toolPanel.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.85f);

        var toolVLG = toolPanel.AddComponent<VerticalLayoutGroup>();
        toolVLG.spacing = 4;
        toolVLG.padding = new RectOffset(8, 8, 8, 8);
        toolVLG.childControlWidth = true;
        toolVLG.childControlHeight = true;
        toolVLG.childForceExpandWidth = true;
        toolVLG.childForceExpandHeight = false;

        Color lblColor = new Color(0.8f, 0.75f, 0.65f);

        // Helper to create a labeled slider
        Slider MakeSlider(string label, float min, float max, float defaultVal)
        {
            var row = CreateUIObject("Row_" + label, toolPanel.transform);
            row.AddComponent<LayoutElement>().preferredHeight = 30;

            var lbl = CreateUIObject("L", row.transform);
            var lr = lbl.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero; lr.anchorMax = new Vector2(0.3f, 1);
            lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;
            var lt = lbl.AddComponent<TextMeshProUGUI>();
            lt.text = label; lt.fontSize = 12; lt.color = lblColor;
            lt.alignment = TextAlignmentOptions.MidlineLeft;

            var sliderObj = CreateUIObject("S", row.transform);
            var sr = sliderObj.GetComponent<RectTransform>();
            sr.anchorMin = new Vector2(0.32f, 0.2f); sr.anchorMax = new Vector2(1, 0.8f);
            sr.offsetMin = Vector2.zero; sr.offsetMax = Vector2.zero;

            // Background
            var bgObj = CreateUIObject("BG", sliderObj.transform);
            SetAnchorsStretch(bgObj);
            bgObj.AddComponent<Image>().color = new Color(0.15f, 0.13f, 0.10f);

            // Fill Area
            var fillArea = CreateUIObject("Fill Area", sliderObj.transform);
            SetAnchorsStretch(fillArea);
            var fillR = fillArea.GetComponent<RectTransform>();
            fillR.offsetMin = new Vector2(5, 0); fillR.offsetMax = new Vector2(-5, 0);
            var fill = CreateUIObject("Fill", fillArea.transform);
            SetAnchorsStretch(fill);
            fill.AddComponent<Image>().color = new Color(0.3f, 0.7f, 0.8f, 0.6f);

            // Handle
            var handleArea = CreateUIObject("Handle Slide Area", sliderObj.transform);
            SetAnchorsStretch(handleArea);
            var har = handleArea.GetComponent<RectTransform>();
            har.offsetMin = new Vector2(10, 0); har.offsetMax = new Vector2(-10, 0);
            var handle = CreateUIObject("Handle", handleArea.transform);
            var hRect = handle.GetComponent<RectTransform>();
            hRect.sizeDelta = new Vector2(14, 0);
            handle.AddComponent<Image>().color = new Color(0.9f, 0.85f, 0.7f);

            var slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = hRect;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = defaultVal;

            return slider;
        }

        var sPosX = MakeSlider("Pos X", -1f, 1f, 0f);
        var sPosY = MakeSlider("Pos Y", -1f, 1f, -0.3f);
        var sPosZ = MakeSlider("Pos Z", -1f, 1f, 0f);
        var sRotX = MakeSlider("Rot X", -180f, 180f, 0f);
        var sRotY = MakeSlider("Rot Y", -180f, 180f, 0f);
        var sRotZ = MakeSlider("Rot Z", -180f, 180f, 0f);

        // Values label
        var valObj = CreateUIObject("Values", toolPanel.transform);
        valObj.AddComponent<LayoutElement>().preferredHeight = 40;
        var valTMP = valObj.AddComponent<TextMeshProUGUI>();
        valTMP.text = "Pos(0, -0.3, 0)\nRot(0, 0, 0)";
        valTMP.fontSize = 11;
        valTMP.color = new Color(0.3f, 0.8f, 0.9f);
        valTMP.alignment = TextAlignmentOptions.Center;

        // Valider button
        var validateBtn = CreateMedievalButton("Valider", toolPanel.transform, 35);
        validateBtn.GetComponent<LayoutElement>().preferredHeight = 40;

        // AnimationPreviewController
        var controller = canvasObj.AddComponent<AnimationPreviewController>();

        // Load GanzSe prefab
        string ganzsePath = "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/Modular Character/Modular Character Update 1.1/GanzSe Free Modular Character Update 1_1.prefab";
        var ganzsePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ganzsePath);

        // Load animation clips from DoubleL
        var clipNames = new string[]
        {
            "Idle", "Walk Forward", "Walk Back", "Run Forward", "Sprint",
            "Crouch Idle", "Jump", "Attack A1", "Attack A2", "Attack A3",
            "Attack B1", "Attack B2", "Shield Block", "Hit 1", "Hit 2",
            "Dialogue 1"
        };
        var clipPaths = new string[]
        {
            "Assets/DoubleL/Demo/Anim/OneHand_Up_Idle.anim",
            "Assets/DoubleL/Demo/Anim/OneHand_Up_Walk_F_InPlace.anim",
            "Assets/DoubleL/Demo/Anim/OneHand_Up_Walk_B_InPlace.anim",
            "Assets/DoubleL/Demo/Anim/OneHand_Up_Run_F_InPlace.anim",
            "Assets/DoubleL/Demo/Anim/OneHand_Up_Sprint_InPlace.anim",
            "Assets/DoubleL/Demo/Anim/OneHand_Up_Crouch_Idle.anim",
            "Assets/DoubleL/Demo/Anim/OneHand_Up_Jump_B_InPlace.anim",
            "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_1_InPlace.anim",
            "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_2_InPlace.anim",
            "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_3_InPlace.anim",
            "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_B_1_InPlace.anim",
            "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_B_2_InPlace.anim",
            "Assets/DoubleL/Demo/Anim/OneHand_Up_Shield_Block_Idle.anim",
            "Assets/DoubleL/Demo/Anim/Hit_F_1_InPlace.anim",
            "Assets/DoubleL/Demo/Anim/Hit_F_2_InPlace.anim",
            "Assets/DoubleL/Demo/Anim/Dialogue_1.anim",
        };

        var clips = new List<AnimationClip>();
        var names = new List<string>();
        for (int i = 0; i < clipPaths.Length; i++)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPaths[i]);
            if (clip != null)
            {
                clips.Add(clip);
                names.Add(clipNames[i]);
            }
        }

        // Also create a simple AnimatorController with a default state for the override system
        string ctrlPath = "Assets/_Project/Art/Animations/AnimPreviewController.controller";
        System.IO.Directory.CreateDirectory("Assets/_Project/Art/Animations");
        var animCtrl = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        if (clips.Count > 0)
        {
            var rootSM = animCtrl.layers[0].stateMachine;
            var defaultState = rootSM.AddState("Default");
            defaultState.motion = clips[0]; // Idle as default
            rootSM.defaultState = defaultState;
        }

        // Wire the controller
        var ctrlSO = new SerializedObject(controller);
        if (ganzsePrefab != null)
            ctrlSO.FindProperty("ganzsePrefab").objectReferenceValue = ganzsePrefab;
        ctrlSO.FindProperty("animController").objectReferenceValue = animCtrl;
        ctrlSO.FindProperty("animNameLabel").objectReferenceValue = nameTMP;
        ctrlSO.FindProperty("weaponNameLabel").objectReferenceValue = wNameTMP;
        ctrlSO.FindProperty("slotNameLabel").objectReferenceValue = asNameTMP;
        ctrlSO.FindProperty("pieceNameLabel").objectReferenceValue = apNameTMP;
        ctrlSO.FindProperty("sliderPosX").objectReferenceValue = sPosX;
        ctrlSO.FindProperty("sliderPosY").objectReferenceValue = sPosY;
        ctrlSO.FindProperty("sliderPosZ").objectReferenceValue = sPosZ;
        ctrlSO.FindProperty("sliderRotX").objectReferenceValue = sRotX;
        ctrlSO.FindProperty("sliderRotY").objectReferenceValue = sRotY;
        ctrlSO.FindProperty("sliderRotZ").objectReferenceValue = sRotZ;
        ctrlSO.FindProperty("valuesLabel").objectReferenceValue = valTMP;

        // Set clip arrays
        var clipsProp = ctrlSO.FindProperty("animationClips");
        clipsProp.arraySize = clips.Count;
        for (int i = 0; i < clips.Count; i++)
            clipsProp.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];

        var namesProp = ctrlSO.FindProperty("animationNames");
        namesProp.arraySize = names.Count;
        for (int i = 0; i < names.Count; i++)
            namesProp.GetArrayElementAtIndex(i).stringValue = names[i];

        // Load GanzSe weapon prefabs
        var weaponFolders = new[] {
            "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/GREAT SWORDS",
            "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/ONE-HANDED SWORDS",
            "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/SHIELDS",
            "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/HAMMERS",

            "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/WEAPONS UPDATE 1.1"
        };
        var weaponPrefabsList = new List<GameObject>();
        var weaponNamesList = new List<string>();
        foreach (var folder in weaponFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && !prefab.name.Contains("Arrow"))
                {
                    weaponPrefabsList.Add(prefab);
                    // Clean up name: "FREE GREAT SWORD 1 COLOR 1" → "Great Sword 1 C1"
                    string wn = prefab.name.Replace("FREE ", "").Replace("COLOR ", "C");
                    weaponNamesList.Add(wn);
                }
            }
        }

        var wPrefabsProp = ctrlSO.FindProperty("weaponPrefabs");
        wPrefabsProp.arraySize = weaponPrefabsList.Count;
        for (int i = 0; i < weaponPrefabsList.Count; i++)
            wPrefabsProp.GetArrayElementAtIndex(i).objectReferenceValue = weaponPrefabsList[i];

        var wNamesProp = ctrlSO.FindProperty("weaponNames");
        wNamesProp.arraySize = weaponNamesList.Count;
        for (int i = 0; i < weaponNamesList.Count; i++)
            wNamesProp.GetArrayElementAtIndex(i).stringValue = weaponNamesList[i];

        // Match each weapon prefab to its EquipmentData asset
        var wEqDataProp = ctrlSO.FindProperty("weaponEquipmentData");
        wEqDataProp.arraySize = weaponPrefabsList.Count;
        var eqGuids = AssetDatabase.FindAssets("t:EquipmentData", new[] { "Assets/_Project/Configs/Weapons" });
        for (int i = 0; i < weaponPrefabsList.Count; i++)
        {
            EquipmentData match = null;
            foreach (var guid in eqGuids)
            {
                var eq = AssetDatabase.LoadAssetAtPath<EquipmentData>(AssetDatabase.GUIDToAssetPath(guid));
                if (eq != null && eq.meshPrefab == weaponPrefabsList[i])
                { match = eq; break; }
            }
            wEqDataProp.GetArrayElementAtIndex(i).objectReferenceValue = match;
        }

        ctrlSO.ApplyModifiedPropertiesWithoutUndo();

        // Wire buttons
        UnityEventTools.AddPersistentListener(
            prevBtn.GetComponent<Button>().onClick, controller.PrevAnimation);
        UnityEventTools.AddPersistentListener(
            nextBtn.GetComponent<Button>().onClick, controller.NextAnimation);
        UnityEventTools.AddPersistentListener(
            wPrevBtn.GetComponent<Button>().onClick, controller.PrevWeapon);
        UnityEventTools.AddPersistentListener(
            wNextBtn.GetComponent<Button>().onClick, controller.NextWeapon);
        UnityEventTools.AddPersistentListener(
            returnBtn.GetComponent<Button>().onClick, controller.ReturnToMenu);
        UnityEventTools.AddPersistentListener(
            asPrevBtn.GetComponent<Button>().onClick, controller.PrevSlot);
        UnityEventTools.AddPersistentListener(
            asNextBtn.GetComponent<Button>().onClick, controller.NextSlot);
        UnityEventTools.AddPersistentListener(
            apPrevBtn.GetComponent<Button>().onClick, controller.PrevPiece);
        UnityEventTools.AddPersistentListener(
            apNextBtn.GetComponent<Button>().onClick, controller.NextPiece);
        UnityEventTools.AddPersistentListener(
            validateBtn.GetComponent<Button>().onClick, controller.ValidateWeaponPosition);

        // Save scene
        string scenePath = "Assets/_Project/Scenes/AnimationPreview.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("[DonGeonMaster] Animation Preview scene created.");
    }

    private static void CreateScreenManagerScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 3D Render Camera (for item preview)
        var renderCamObj = new GameObject("RenderCamera");
        var renderCam = renderCamObj.AddComponent<Camera>();
        renderCam.clearFlags = CameraClearFlags.SolidColor;
        renderCam.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
        renderCam.fieldOfView = 30;
        renderCam.nearClipPlane = 0.01f;
        renderCam.enabled = true; // Renders continuously to RenderTexture
        renderCamObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = false;

        // UI Camera
        var uiCamObj = new GameObject("Main Camera");
        uiCamObj.tag = "MainCamera";
        var uiCam = uiCamObj.AddComponent<Camera>();
        uiCam.clearFlags = CameraClearFlags.SolidColor;
        uiCam.backgroundColor = new Color(0.06f, 0.06f, 0.10f);
        uiCam.cullingMask = 0; // UI only
        uiCamObj.AddComponent<AudioListener>();
        uiCamObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

        // Light
        var lightObj = new GameObject("Directional Light");
        lightObj.transform.rotation = Quaternion.Euler(30, -30, 0);
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.95f, 0.9f);
        light.intensity = 1.5f;
        light.shadows = LightShadows.None;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.15f, 0.12f, 0.10f);

        // EventSystem
        var esObj = new GameObject("EventSystem");
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // Canvas
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Title
        var titleObj = CreateUIObject("Title", canvasObj.transform);
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1);
        titleRect.anchorMax = new Vector2(0.5f, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -10);
        titleRect.sizeDelta = new Vector2(600, 40);
        var titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "SCREENSHOT ITEMS";
        titleTMP.fontSize = 28;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(0.3f, 0.8f, 0.9f);

        // Preview image (center — shows the 3D render)
        var previewObj = CreateUIObject("Preview", canvasObj.transform);
        var previewRect = previewObj.GetComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0.2f, 0.15f);
        previewRect.anchorMax = new Vector2(0.7f, 0.88f);
        previewRect.offsetMin = Vector2.zero;
        previewRect.offsetMax = Vector2.zero;
        var rawImg = previewObj.AddComponent<RawImage>();
        rawImg.color = Color.white;

        // Item navigation (bottom center)
        var navContainer = CreateUIObject("ItemNav", canvasObj.transform);
        var navRect = navContainer.GetComponent<RectTransform>();
        navRect.anchorMin = new Vector2(0.5f, 0);
        navRect.anchorMax = new Vector2(0.5f, 0);
        navRect.pivot = new Vector2(0.5f, 0);
        navRect.anchoredPosition = new Vector2(0, 70);
        navRect.sizeDelta = new Vector2(600, 55);

        var navHLG = navContainer.AddComponent<HorizontalLayoutGroup>();
        navHLG.spacing = 15;
        navHLG.childAlignment = TextAnchor.MiddleCenter;
        navHLG.childControlWidth = true;
        navHLG.childControlHeight = true;
        navHLG.childForceExpandWidth = false;

        var prevBtn = CreateMedievalButton("<", navContainer.transform, 50);
        prevBtn.GetComponent<LayoutElement>().preferredWidth = 60;

        var nameObj = CreateUIObject("ItemName", navContainer.transform);
        nameObj.AddComponent<LayoutElement>().preferredWidth = 400;
        var nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
        nameTMP.text = "...";
        nameTMP.fontSize = 22;
        nameTMP.alignment = TextAlignmentOptions.Center;
        nameTMP.color = new Color(0.95f, 0.78f, 0.25f);
        nameTMP.fontStyle = FontStyles.Bold;

        var nextBtn = CreateMedievalButton(">", navContainer.transform, 50);
        nextBtn.GetComponent<LayoutElement>().preferredWidth = 60;

        // Return button
        var returnBtn = CreateMedievalButton("Retour", canvasObj.transform, 40);
        var retRect = returnBtn.GetComponent<RectTransform>();
        retRect.anchorMin = new Vector2(0.5f, 0);
        retRect.anchorMax = new Vector2(0.5f, 0);
        retRect.pivot = new Vector2(0.5f, 0);
        retRect.anchoredPosition = new Vector2(0, 15);
        retRect.sizeDelta = new Vector2(200, 45);

        // Tool panel (right side)
        var toolPanel = CreateUIObject("ToolPanel", canvasObj.transform);
        var tpRect = toolPanel.GetComponent<RectTransform>();
        tpRect.anchorMin = new Vector2(1, 0.15f);
        tpRect.anchorMax = new Vector2(1, 0.85f);
        tpRect.pivot = new Vector2(1, 0.5f);
        tpRect.anchoredPosition = new Vector2(-10, 0);
        tpRect.sizeDelta = new Vector2(220, 0);
        toolPanel.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.85f);

        var toolVLG = toolPanel.AddComponent<VerticalLayoutGroup>();
        toolVLG.spacing = 6;
        toolVLG.padding = new RectOffset(8, 8, 8, 8);
        toolVLG.childControlWidth = true;
        toolVLG.childControlHeight = true;
        toolVLG.childForceExpandWidth = true;
        toolVLG.childForceExpandHeight = false;

        Color lblColor = new Color(0.8f, 0.75f, 0.65f);

        // Slider helper
        Slider MakeSlider(string label, float min, float max, float defaultVal)
        {
            var row = CreateUIObject("Row_" + label, toolPanel.transform);
            row.AddComponent<LayoutElement>().preferredHeight = 35;

            var lbl = CreateUIObject("L", row.transform);
            var lr = lbl.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero; lr.anchorMax = new Vector2(0.3f, 1);
            lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;
            var lt = lbl.AddComponent<TextMeshProUGUI>();
            lt.text = label; lt.fontSize = 13; lt.color = lblColor;
            lt.alignment = TextAlignmentOptions.MidlineLeft;

            var sliderObj = CreateUIObject("S", row.transform);
            var sr = sliderObj.GetComponent<RectTransform>();
            sr.anchorMin = new Vector2(0.32f, 0.15f); sr.anchorMax = new Vector2(1, 0.85f);
            sr.offsetMin = Vector2.zero; sr.offsetMax = Vector2.zero;

            var bgObj = CreateUIObject("BG", sliderObj.transform);
            SetAnchorsStretch(bgObj);
            bgObj.AddComponent<Image>().color = new Color(0.15f, 0.13f, 0.10f);

            var fillArea = CreateUIObject("Fill Area", sliderObj.transform);
            SetAnchorsStretch(fillArea);
            var fillR = fillArea.GetComponent<RectTransform>();
            fillR.offsetMin = new Vector2(5, 0); fillR.offsetMax = new Vector2(-5, 0);
            var fill = CreateUIObject("Fill", fillArea.transform);
            SetAnchorsStretch(fill);
            fill.AddComponent<Image>().color = new Color(0.3f, 0.7f, 0.8f, 0.6f);

            var handleArea = CreateUIObject("Handle Slide Area", sliderObj.transform);
            SetAnchorsStretch(handleArea);
            var har = handleArea.GetComponent<RectTransform>();
            har.offsetMin = new Vector2(10, 0); har.offsetMax = new Vector2(-10, 0);
            var handle = CreateUIObject("Handle", handleArea.transform);
            var hRect = handle.GetComponent<RectTransform>();
            hRect.sizeDelta = new Vector2(14, 0);
            handle.AddComponent<Image>().color = new Color(0.9f, 0.85f, 0.7f);

            var slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = hRect;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = defaultVal;
            return slider;
        }

        var sDist = MakeSlider("Distance", 0.1f, 10f, 1.5f);
        var sRotY = MakeSlider("Rotation", -180f, 180f, 30f);
        var sHeight = MakeSlider("Hauteur", -3f, 3f, 0.2f);

        // Values label
        var valObj = CreateUIObject("Values", toolPanel.transform);
        valObj.AddComponent<LayoutElement>().preferredHeight = 35;
        var valTMP = valObj.AddComponent<TextMeshProUGUI>();
        valTMP.text = "Dist: 1.00  Rot: 30°  H: 0.20";
        valTMP.fontSize = 12;
        valTMP.color = new Color(0.3f, 0.8f, 0.9f);
        valTMP.alignment = TextAlignmentOptions.Center;

        // Screenshot button
        var screenshotBtn = CreateMedievalButton("Screenshot", toolPanel.transform, 30);
        screenshotBtn.GetComponent<LayoutElement>().preferredHeight = 45;

        // Thumbnail preview label
        var thumbLblObj = CreateUIObject("ThumbLabel", toolPanel.transform);
        thumbLblObj.AddComponent<LayoutElement>().preferredHeight = 20;
        var thumbLblTMP = thumbLblObj.AddComponent<TextMeshProUGUI>();
        thumbLblTMP.text = "Thumbnail actuel :";
        thumbLblTMP.fontSize = 12;
        thumbLblTMP.color = lblColor;
        thumbLblTMP.alignment = TextAlignmentOptions.Center;

        // Thumbnail preview image (shows existing saved screenshot)
        var thumbPreviewObj = CreateUIObject("ThumbPreview", toolPanel.transform);
        thumbPreviewObj.AddComponent<LayoutElement>().preferredHeight = 128;
        var thumbRawImg = thumbPreviewObj.AddComponent<RawImage>();
        thumbRawImg.color = new Color(1, 1, 1, 0.5f); // Dim when no thumbnail

        // Load armor prefabs (same source as ArmorThumbnailGenerator)
        var prefabFolders = new[] {
            "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/Non-Skinned Mesh Parts/Armor Parts",
            "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/Non-Skinned Mesh Parts/Armor Parts 1.1",
            "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/GREAT SWORDS",
            "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/ONE-HANDED SWORDS",
            "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/SHIELDS",
            "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/HAMMERS",
            "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/WEAPONS UPDATE 1.1"
        };
        var itemPrefabsList = new System.Collections.Generic.List<GameObject>();
        var itemNamesList = new System.Collections.Generic.List<string>();
        foreach (var folder in prefabFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && !prefab.name.Contains("Arrow"))
                {
                    itemPrefabsList.Add(prefab);
                    string pn = prefab.name.Replace("FREE ", "").Replace("COLOR ", "C");
                    if (pn.EndsWith(" Part")) pn = pn.Substring(0, pn.Length - 5);
                    itemNamesList.Add(pn);
                }
            }
        }

        // Wire controller
        var controller = canvasObj.AddComponent<ScreenManagerController>();
        var ctrlSO = new SerializedObject(controller);

        ctrlSO.FindProperty("renderCamera").objectReferenceValue = renderCam;
        ctrlSO.FindProperty("sceneLight").objectReferenceValue = light;
        ctrlSO.FindProperty("itemLabel").objectReferenceValue = nameTMP;
        ctrlSO.FindProperty("valuesLabel").objectReferenceValue = valTMP;
        ctrlSO.FindProperty("sliderDist").objectReferenceValue = sDist;
        ctrlSO.FindProperty("sliderRotY").objectReferenceValue = sRotY;
        ctrlSO.FindProperty("sliderHeight").objectReferenceValue = sHeight;
        ctrlSO.FindProperty("previewImage").objectReferenceValue = rawImg;
        ctrlSO.FindProperty("thumbPreview").objectReferenceValue = thumbRawImg;

        var prefabsProp = ctrlSO.FindProperty("itemPrefabs");
        prefabsProp.arraySize = itemPrefabsList.Count;
        for (int i = 0; i < itemPrefabsList.Count; i++)
            prefabsProp.GetArrayElementAtIndex(i).objectReferenceValue = itemPrefabsList[i];

        var namesProp = ctrlSO.FindProperty("itemNames");
        namesProp.arraySize = itemNamesList.Count;
        for (int i = 0; i < itemNamesList.Count; i++)
            namesProp.GetArrayElementAtIndex(i).stringValue = itemNamesList[i];

        ctrlSO.ApplyModifiedPropertiesWithoutUndo();

        // Wire buttons
        UnityEventTools.AddPersistentListener(
            prevBtn.GetComponent<Button>().onClick, controller.PrevItem);
        UnityEventTools.AddPersistentListener(
            nextBtn.GetComponent<Button>().onClick, controller.NextItem);
        UnityEventTools.AddPersistentListener(
            screenshotBtn.GetComponent<Button>().onClick, controller.TakeScreenshot);
        UnityEventTools.AddPersistentListener(
            returnBtn.GetComponent<Button>().onClick, controller.ReturnToMenu);

        // Save scene
        string scenePath = "Assets/_Project/Scenes/ScreenManager.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"[DonGeonMaster] Screen Manager scene created with {itemPrefabsList.Count} items.");
    }

    private static void CreateItemEditorScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 3D Render Camera
        var renderCamObj = new GameObject("RenderCamera");
        var renderCam = renderCamObj.AddComponent<Camera>();
        renderCam.clearFlags = CameraClearFlags.SolidColor;
        renderCam.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
        renderCam.fieldOfView = 30;
        renderCam.nearClipPlane = 0.01f;
        renderCam.enabled = true;
        renderCamObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = false;

        // UI Camera
        var camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        var cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.06f, 0.10f);
        cam.cullingMask = 0;
        camObj.AddComponent<AudioListener>();
        camObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

        // Light
        var lightObj = new GameObject("Directional Light");
        lightObj.transform.rotation = Quaternion.Euler(30, -30, 0);
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.95f, 0.9f);
        light.intensity = 1.5f;
        light.shadows = LightShadows.None;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.15f, 0.12f, 0.10f);

        var esObj = new GameObject("EventSystem");
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        var titleObj = CreateUIObject("Title", canvasObj.transform);
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1); titleRect.anchorMax = new Vector2(0.5f, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -10); titleRect.sizeDelta = new Vector2(600, 40);
        var titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "ITEM EDITOR"; titleTMP.fontSize = 28;
        titleTMP.fontStyle = FontStyles.Bold; titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(0.3f, 0.8f, 0.9f);

        Color lblColor = new Color(0.8f, 0.75f, 0.65f);
        Color valColor = new Color(0.95f, 0.78f, 0.25f);

        // === 3D Preview (left) ===
        var previewObj = CreateUIObject("Preview", canvasObj.transform);
        var prvRect = previewObj.GetComponent<RectTransform>();
        prvRect.anchorMin = new Vector2(0, 0.12f); prvRect.anchorMax = new Vector2(0.3f, 0.92f);
        prvRect.offsetMin = new Vector2(15, 0); prvRect.offsetMax = new Vector2(-5, 0);
        var rawImg = previewObj.AddComponent<RawImage>();
        rawImg.color = Color.white;

        // === MIDDLE PANEL: Info + Dropdowns ===
        var leftPanel = CreateUIObject("MiddlePanel", canvasObj.transform);
        var lpRect = leftPanel.GetComponent<RectTransform>();
        lpRect.anchorMin = new Vector2(0.32f, 0.08f); lpRect.anchorMax = new Vector2(0.65f, 0.92f);
        lpRect.offsetMin = new Vector2(5, 0); lpRect.offsetMax = new Vector2(-5, 0);
        leftPanel.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.05f, 0.85f);
        var lpVLG = leftPanel.AddComponent<VerticalLayoutGroup>();
        lpVLG.spacing = 4; lpVLG.padding = new RectOffset(10, 10, 8, 8);
        lpVLG.childControlWidth = true; lpVLG.childControlHeight = true;
        lpVLG.childForceExpandWidth = true; lpVLG.childForceExpandHeight = false;

        // Item name
        var nameObj = CreateUIObject("ItemName", leftPanel.transform);
        nameObj.AddComponent<LayoutElement>().preferredHeight = 50;
        var nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
        nameTMP.text = "..."; nameTMP.fontSize = 18; nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.alignment = TextAlignmentOptions.Center; nameTMP.color = valColor;

        // Icon
        var iconObj = CreateUIObject("Icon", leftPanel.transform);
        iconObj.AddComponent<LayoutElement>().preferredHeight = 64;
        var iconImg = iconObj.AddComponent<Image>();
        iconImg.preserveAspect = true; iconImg.enabled = false;

        // Slot
        var slotObj = CreateUIObject("Slot", leftPanel.transform);
        slotObj.AddComponent<LayoutElement>().preferredHeight = 22;
        var slotTMP = slotObj.AddComponent<TextMeshProUGUI>();
        slotTMP.text = "Slot: ..."; slotTMP.fontSize = 14; slotTMP.color = lblColor;
        slotTMP.alignment = TextAlignmentOptions.Center;

        // Dropdown factory
        TMP_Dropdown MakeDropdown(string label)
        {
            var row = CreateUIObject("Drop_" + label, leftPanel.transform);
            row.AddComponent<LayoutElement>().preferredHeight = 38;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true; hlg.childControlHeight = true; hlg.childForceExpandWidth = false;

            var lbl = CreateUIObject("L", row.transform);
            lbl.AddComponent<LayoutElement>().preferredWidth = 110;
            var lt = lbl.AddComponent<TextMeshProUGUI>();
            lt.text = label; lt.fontSize = 13; lt.color = lblColor; lt.alignment = TextAlignmentOptions.MidlineRight;

            var dropObj = CreateUIObject("D", row.transform);
            dropObj.AddComponent<LayoutElement>().preferredWidth = 180;
            dropObj.AddComponent<Image>().color = new Color(0.15f, 0.13f, 0.10f);

            // Dropdown label
            var capObj = CreateUIObject("Label", dropObj.transform);
            SetAnchorsStretch(capObj);
            capObj.GetComponent<RectTransform>().offsetMin = new Vector2(8, 0);
            capObj.GetComponent<RectTransform>().offsetMax = new Vector2(-25, 0);
            var capTMP = capObj.AddComponent<TextMeshProUGUI>();
            capTMP.fontSize = 13; capTMP.color = valColor; capTMP.alignment = TextAlignmentOptions.MidlineLeft;

            // Arrow
            var arrowObj = CreateUIObject("Arrow", dropObj.transform);
            var arrowRect = arrowObj.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0); arrowRect.anchorMax = new Vector2(1, 1);
            arrowRect.pivot = new Vector2(1, 0.5f);
            arrowRect.sizeDelta = new Vector2(20, 0); arrowRect.anchoredPosition = new Vector2(-4, 0);
            var arrowTMP = arrowObj.AddComponent<TextMeshProUGUI>();
            arrowTMP.text = "▼"; arrowTMP.fontSize = 10; arrowTMP.color = lblColor;
            arrowTMP.alignment = TextAlignmentOptions.Center;

            // Template (dropdown popup)
            var templateObj = CreateUIObject("Template", dropObj.transform);
            var tRect = templateObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0, 0); tRect.anchorMax = new Vector2(1, 0);
            tRect.pivot = new Vector2(0.5f, 1); tRect.sizeDelta = new Vector2(0, 150);
            templateObj.AddComponent<Image>().color = new Color(0.12f, 0.10f, 0.08f);
            var scroll = templateObj.AddComponent<ScrollRect>();

            var vpObj = CreateUIObject("Viewport", templateObj.transform);
            SetAnchorsStretch(vpObj);
            vpObj.AddComponent<Image>().color = new Color(0.12f, 0.10f, 0.08f);
            vpObj.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = vpObj.GetComponent<RectTransform>();

            var contentObj = CreateUIObject("Content", vpObj.transform);
            var cRect = contentObj.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1); cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1); cRect.sizeDelta = new Vector2(0, 28);
            scroll.content = cRect;

            // Item template
            var itemObj = CreateUIObject("Item", contentObj.transform);
            var iRect = itemObj.GetComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0, 0.5f); iRect.anchorMax = new Vector2(1, 0.5f);
            iRect.sizeDelta = new Vector2(0, 28); iRect.pivot = new Vector2(0.5f, 0.5f);
            itemObj.AddComponent<Toggle>();

            var itemBg = CreateUIObject("Item Background", itemObj.transform);
            SetAnchorsStretch(itemBg);
            itemBg.AddComponent<Image>().color = new Color(0.18f, 0.15f, 0.12f);

            var itemLbl = CreateUIObject("Item Label", itemObj.transform);
            SetAnchorsStretch(itemLbl);
            itemLbl.GetComponent<RectTransform>().offsetMin = new Vector2(8, 0);
            var ilTMP = itemLbl.AddComponent<TextMeshProUGUI>();
            ilTMP.fontSize = 13; ilTMP.color = Color.white; ilTMP.alignment = TextAlignmentOptions.MidlineLeft;

            templateObj.SetActive(false);

            var dropdown = dropObj.AddComponent<TMP_Dropdown>();
            dropdown.template = tRect;
            dropdown.captionText = capTMP;
            dropdown.itemText = ilTMP;

            return dropdown;
        }

        var dropRarity = MakeDropdown("Rareté");
        var dropWeight = MakeDropdown("Poids");
        var dropWeaponType = MakeDropdown("Type Arme");
        var dropArmorMat = MakeDropdown("Matériau");
        var dropHandling = MakeDropdown("Maniabilité");
        var dropElement = MakeDropdown("Élément");

        // === RIGHT PANEL: Stats (input fields) ===
        var rightPanel = CreateUIObject("RightPanel", canvasObj.transform);
        var rpRect = rightPanel.GetComponent<RectTransform>();
        rpRect.anchorMin = new Vector2(0.67f, 0.08f); rpRect.anchorMax = new Vector2(0.98f, 0.92f);
        rpRect.offsetMin = new Vector2(5, 0); rpRect.offsetMax = new Vector2(-5, 0);
        rightPanel.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.05f, 0.85f);
        var rpVLG = rightPanel.AddComponent<VerticalLayoutGroup>();
        rpVLG.spacing = 6; rpVLG.padding = new RectOffset(10, 10, 10, 10);
        rpVLG.childControlWidth = true; rpVLG.childControlHeight = true;
        rpVLG.childForceExpandWidth = true; rpVLG.childForceExpandHeight = false;

        var stTitle = CreateUIObject("StatsTitle", rightPanel.transform);
        stTitle.AddComponent<LayoutElement>().preferredHeight = 30;
        var stTitleTMP = stTitle.AddComponent<TextMeshProUGUI>();
        stTitleTMP.text = "STATS"; stTitleTMP.fontSize = 20; stTitleTMP.fontStyle = FontStyles.Bold;
        stTitleTMP.alignment = TextAlignmentOptions.Center; stTitleTMP.color = new Color(0.3f, 0.8f, 0.9f);

        // Input field factory
        TMP_InputField MakeInputField(string label, string defaultVal, TMP_InputField.ContentType contentType)
        {
            var row = CreateUIObject("Input_" + label, rightPanel.transform);
            row.AddComponent<LayoutElement>().preferredHeight = 38;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true; hlg.childControlHeight = true; hlg.childForceExpandWidth = false;

            var lbl = CreateUIObject("L", row.transform);
            lbl.AddComponent<LayoutElement>().preferredWidth = 130;
            var lt = lbl.AddComponent<TextMeshProUGUI>();
            lt.text = label; lt.fontSize = 14; lt.color = lblColor; lt.alignment = TextAlignmentOptions.MidlineRight;

            var fieldObj = CreateUIObject("F", row.transform);
            fieldObj.AddComponent<LayoutElement>().preferredWidth = 120;
            fieldObj.AddComponent<Image>().color = new Color(0.15f, 0.13f, 0.10f);

            var textArea = CreateUIObject("Text Area", fieldObj.transform);
            SetAnchorsStretch(textArea);
            textArea.GetComponent<RectTransform>().offsetMin = new Vector2(6, 0);
            textArea.GetComponent<RectTransform>().offsetMax = new Vector2(-6, 0);
            textArea.AddComponent<RectMask2D>();

            var textObj = CreateUIObject("Text", textArea.transform);
            SetAnchorsStretch(textObj);
            var textTMP = textObj.AddComponent<TextMeshProUGUI>();
            textTMP.fontSize = 15; textTMP.color = valColor; textTMP.alignment = TextAlignmentOptions.MidlineLeft;

            var phObj = CreateUIObject("Placeholder", textArea.transform);
            SetAnchorsStretch(phObj);
            var phTMP = phObj.AddComponent<TextMeshProUGUI>();
            phTMP.text = "0"; phTMP.fontSize = 15; phTMP.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            phTMP.alignment = TextAlignmentOptions.MidlineLeft;
            phTMP.fontStyle = FontStyles.Italic;

            var inputField = fieldObj.AddComponent<TMP_InputField>();
            inputField.textViewport = textArea.GetComponent<RectTransform>();
            inputField.textComponent = textTMP;
            inputField.placeholder = phTMP;
            inputField.text = defaultVal;
            inputField.contentType = contentType;
            inputField.pointSize = 15;

            return inputField;
        }

        var inArmor = MakeInputField("Armure", "0", TMP_InputField.ContentType.IntegerNumber);
        var inDamage = MakeInputField("Dégâts", "0", TMP_InputField.ContentType.IntegerNumber);

        // Feedback label
        var fbObj = CreateUIObject("Feedback", rightPanel.transform);
        fbObj.AddComponent<LayoutElement>().preferredHeight = 25;
        var fbTMP = fbObj.AddComponent<TextMeshProUGUI>();
        fbTMP.text = ""; fbTMP.fontSize = 14; fbTMP.alignment = TextAlignmentOptions.Center;
        fbTMP.color = new Color(0.3f, 0.8f, 0.9f);

        // Valider button
        var validateBtn = CreateMedievalButton("Valider", rightPanel.transform, 32);
        validateBtn.GetComponent<LayoutElement>().preferredHeight = 45;

        // === NAVIGATION (bottom) ===
        var navContainer = CreateUIObject("ItemNav", canvasObj.transform);
        var navRect = navContainer.GetComponent<RectTransform>();
        navRect.anchorMin = new Vector2(0.5f, 0); navRect.anchorMax = new Vector2(0.5f, 0);
        navRect.pivot = new Vector2(0.5f, 0);
        navRect.anchoredPosition = new Vector2(0, 50); navRect.sizeDelta = new Vector2(600, 45);
        var navHLG = navContainer.AddComponent<HorizontalLayoutGroup>();
        navHLG.spacing = 15; navHLG.childAlignment = TextAnchor.MiddleCenter;
        navHLG.childControlWidth = true; navHLG.childControlHeight = true; navHLG.childForceExpandWidth = false;

        var prevBtn = CreateMedievalButton("<", navContainer.transform, 45);
        prevBtn.GetComponent<LayoutElement>().preferredWidth = 60;
        var navLbl = CreateUIObject("NavLabel", navContainer.transform);
        navLbl.AddComponent<LayoutElement>().preferredWidth = 400;
        navLbl.AddComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        var nextBtn = CreateMedievalButton(">", navContainer.transform, 45);
        nextBtn.GetComponent<LayoutElement>().preferredWidth = 60;

        var returnBtn = CreateMedievalButton("Retour", canvasObj.transform, 38);
        var retRect = returnBtn.GetComponent<RectTransform>();
        retRect.anchorMin = new Vector2(0.5f, 0); retRect.anchorMax = new Vector2(0.5f, 0);
        retRect.pivot = new Vector2(0.5f, 0);
        retRect.anchoredPosition = new Vector2(0, 8); retRect.sizeDelta = new Vector2(180, 38);

        // === Load items ===
        var allGuids = new System.Collections.Generic.List<string>();
        if (AssetDatabase.IsValidFolder("Assets/_Project/Configs/Armor"))
            allGuids.AddRange(AssetDatabase.FindAssets("t:EquipmentData", new[] { "Assets/_Project/Configs/Armor" }));
        if (AssetDatabase.IsValidFolder("Assets/_Project/Configs/Weapons"))
            allGuids.AddRange(AssetDatabase.FindAssets("t:EquipmentData", new[] { "Assets/_Project/Configs/Weapons" }));

        var itemsList = new System.Collections.Generic.List<EquipmentData>();
        foreach (var guid in allGuids)
        {
            var eq = AssetDatabase.LoadAssetAtPath<EquipmentData>(AssetDatabase.GUIDToAssetPath(guid));
            if (eq != null) itemsList.Add(eq);
        }

        // === Build prefab lookup for 3D preview ===
        // Load all Non-Skinned armor prefabs into a name→prefab dict
        var prefabLookup = new System.Collections.Generic.Dictionary<string, GameObject>();
        var armorPrefabFolders = new[] {
            "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/Non-Skinned Mesh Parts/Armor Parts",
            "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/Non-Skinned Mesh Parts/Armor Parts 1.1"
        };
        foreach (var folder in armorPrefabFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab == null) continue;
                string pn = prefab.name;
                if (pn.EndsWith(" Part")) pn = pn.Substring(0, pn.Length - 5);
                prefabLookup[pn] = prefab;
            }
        }

        // Match each item to its prefab
        var itemPrefabsList = new System.Collections.Generic.List<GameObject>();
        foreach (var item in itemsList)
        {
            GameObject prefab = null;
            if (item.meshPrefab != null)
                prefab = item.meshPrefab; // Weapons already have meshPrefab
            else if (!string.IsNullOrEmpty(item.armorPartName))
                prefabLookup.TryGetValue(item.armorPartName, out prefab);
            itemPrefabsList.Add(prefab); // May be null if not found
        }

        // === Wire controller ===
        var controller = canvasObj.AddComponent<ItemEditorController>();
        var ctrlSO = new SerializedObject(controller);

        var itemsProp = ctrlSO.FindProperty("items");
        itemsProp.arraySize = itemsList.Count;
        for (int i = 0; i < itemsList.Count; i++)
            itemsProp.GetArrayElementAtIndex(i).objectReferenceValue = itemsList[i];

        var prefabsProp = ctrlSO.FindProperty("itemPrefabs");
        prefabsProp.arraySize = itemPrefabsList.Count;
        for (int i = 0; i < itemPrefabsList.Count; i++)
            prefabsProp.GetArrayElementAtIndex(i).objectReferenceValue = itemPrefabsList[i];

        ctrlSO.FindProperty("renderCamera").objectReferenceValue = renderCam;
        ctrlSO.FindProperty("sceneLight").objectReferenceValue = light;
        ctrlSO.FindProperty("previewImage").objectReferenceValue = rawImg;
        ctrlSO.FindProperty("itemNameLabel").objectReferenceValue = nameTMP;
        ctrlSO.FindProperty("slotLabel").objectReferenceValue = slotTMP;
        ctrlSO.FindProperty("dropRarity").objectReferenceValue = dropRarity;
        ctrlSO.FindProperty("dropWeight").objectReferenceValue = dropWeight;
        ctrlSO.FindProperty("dropWeaponType").objectReferenceValue = dropWeaponType;
        ctrlSO.FindProperty("dropArmorMaterial").objectReferenceValue = dropArmorMat;
        ctrlSO.FindProperty("dropHandling").objectReferenceValue = dropHandling;
        ctrlSO.FindProperty("dropElement").objectReferenceValue = dropElement;
        // Wire row GameObjects for show/hide
        ctrlSO.FindProperty("rowWeaponType").objectReferenceValue = dropWeaponType.transform.parent.gameObject;
        ctrlSO.FindProperty("rowArmorMaterial").objectReferenceValue = dropArmorMat.transform.parent.gameObject;
        ctrlSO.FindProperty("inputArmor").objectReferenceValue = inArmor;
        ctrlSO.FindProperty("inputDamage").objectReferenceValue = inDamage;
        ctrlSO.FindProperty("feedbackLabel").objectReferenceValue = fbTMP;
        ctrlSO.ApplyModifiedPropertiesWithoutUndo();

        // Wire buttons
        UnityEventTools.AddPersistentListener(prevBtn.GetComponent<Button>().onClick, controller.PrevItem);
        UnityEventTools.AddPersistentListener(nextBtn.GetComponent<Button>().onClick, controller.NextItem);
        UnityEventTools.AddPersistentListener(validateBtn.GetComponent<Button>().onClick, controller.ValidateItem);
        UnityEventTools.AddPersistentListener(returnBtn.GetComponent<Button>().onClick, controller.ReturnToMenu);

        string scenePath = "Assets/_Project/Scenes/ItemEditor.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"[DonGeonMaster] Item Editor scene created with {itemsList.Count} items.");
    }

    private static void SetupBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/_Project/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/_Project/Scenes/Hub.unity", true),
            new EditorBuildSettingsScene("Assets/_Project/Scenes/AnimationPreview.unity", true),
            new EditorBuildSettingsScene("Assets/_Project/Scenes/ScreenManager.unity", true),
            new EditorBuildSettingsScene("Assets/_Project/Scenes/ItemEditor.unity", true)
        };
        Debug.Log("[DonGeonMaster] Build Settings configured.");
    }

    // =========================================================================
    // UI HELPERS
    // =========================================================================

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static void SetAnchorsStretch(GameObject obj)
    {
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void CreateTitle(Transform parent)
    {
        // Title shadow (offset dark text)
        var shadowObj = CreateUIObject("TitleShadow", parent);
        var shadowRect = shadowObj.GetComponent<RectTransform>();
        shadowRect.anchorMin = new Vector2(0.5f, 1f);
        shadowRect.anchorMax = new Vector2(0.5f, 1f);
        shadowRect.pivot = new Vector2(0.5f, 1f);
        shadowRect.anchoredPosition = new Vector2(3, -78);
        shadowRect.sizeDelta = new Vector2(900, 140);

        var shadowTMP = shadowObj.AddComponent<TextMeshProUGUI>();
        shadowTMP.text = "DonGeon Master";
        shadowTMP.fontSize = 80;
        shadowTMP.alignment = TextAlignmentOptions.Center;
        shadowTMP.color = new Color(0.05f, 0.02f, 0f, 0.6f);
        shadowTMP.fontStyle = FontStyles.Bold;

        // Title main text
        var titleObj = CreateUIObject("Title", parent);
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -80);
        titleRect.sizeDelta = new Vector2(900, 140);

        var titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "DonGeon Master";
        titleTMP.fontSize = 80;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(0.95f, 0.78f, 0.25f); // Gold
        titleTMP.fontStyle = FontStyles.Bold;

        // Subtitle
        var subObj = CreateUIObject("Subtitle", parent);
        var subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 1f);
        subRect.anchorMax = new Vector2(0.5f, 1f);
        subRect.pivot = new Vector2(0.5f, 1f);
        subRect.anchoredPosition = new Vector2(0, -200);
        subRect.sizeDelta = new Vector2(600, 40);

        var subTMP = subObj.AddComponent<TextMeshProUGUI>();
        subTMP.text = "~ Entrez dans les profondeurs ~";
        subTMP.fontSize = 22;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.color = new Color(0.7f, 0.6f, 0.4f);
        subTMP.fontStyle = FontStyles.Italic;
    }

    private static GameObject CreateMedievalButton(string text, Transform parent, float height)
    {
        var btnObj = CreateUIObject("Btn_" + text, parent);
        var btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(380, height);

        var btnImage = btnObj.AddComponent<Image>();
        if (woodSprite != null)
        {
            btnImage.sprite = woodSprite;
            btnImage.type = Image.Type.Simple;
        }
        else
        {
            btnImage.color = new Color(0.35f, 0.22f, 0.1f);
        }

        var button = btnObj.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color(0.85f, 0.75f, 0.65f);
        colors.highlightedColor = new Color(1f, 0.9f, 0.75f);
        colors.pressedColor = new Color(0.65f, 0.55f, 0.45f);
        colors.selectedColor = new Color(0.9f, 0.8f, 0.7f);
        colors.fadeDuration = 0.15f;
        button.colors = colors;

        // Button text
        var textObj = CreateUIObject("Text", btnObj.transform);
        SetAnchorsStretch(textObj);

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 30;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.95f, 0.9f, 0.8f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = new Color32(30, 15, 5, 180);

        var le = btnObj.AddComponent<LayoutElement>();
        le.preferredHeight = height;

        return btnObj;
    }

    private struct BindingRowResult
    {
        public Button bindButton;
        public TextMeshProUGUI bindLabel;
    }

    private static BindingRowResult CreateParchmentBindingRow(string actionName, string defaultKey, Transform parent)
    {
        var row = CreateUIObject("Row_" + actionName, parent);
        row.GetComponent<RectTransform>().sizeDelta = new Vector2(420, 50);

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;

        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 50;

        // Action label (dark ink on parchment)
        var labelObj = CreateUIObject("Label", row.transform);
        labelObj.AddComponent<LayoutElement>().preferredWidth = 200;

        var labelTMP = labelObj.AddComponent<TextMeshProUGUI>();
        labelTMP.text = actionName;
        labelTMP.fontSize = 26;
        labelTMP.alignment = TextAlignmentOptions.MidlineRight;
        labelTMP.color = new Color(0.2f, 0.12f, 0.05f); // Dark brown ink
        labelTMP.fontStyle = FontStyles.Bold;

        // Key button (wood style)
        var btnObj = CreateUIObject("KeyBtn", row.transform);
        var btnLE = btnObj.AddComponent<LayoutElement>();
        btnLE.preferredWidth = 130;
        btnLE.preferredHeight = 45;

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.3f, 0.2f, 0.1f, 0.8f);

        var button = btnObj.AddComponent<Button>();
        var bColors = button.colors;
        bColors.normalColor = new Color(0.85f, 0.75f, 0.6f);
        bColors.highlightedColor = new Color(1f, 0.9f, 0.7f);
        bColors.pressedColor = new Color(0.6f, 0.5f, 0.4f);
        button.colors = bColors;

        var keyTextObj = CreateUIObject("KeyText", btnObj.transform);
        SetAnchorsStretch(keyTextObj);

        var keyTMP = keyTextObj.AddComponent<TextMeshProUGUI>();
        keyTMP.text = defaultKey;
        keyTMP.fontSize = 26;
        keyTMP.alignment = TextAlignmentOptions.Center;
        keyTMP.color = new Color(0.2f, 0.1f, 0.02f);
        keyTMP.fontStyle = FontStyles.Bold;

        return new BindingRowResult { bindButton = button, bindLabel = keyTMP };
    }

    // =========================================================================
    // 3D HELPERS
    // =========================================================================
    private static void SetMaterial(GameObject obj, Material mat)
    {
        if (mat != null)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = mat;
        }
    }

    private static void SetUVTiling(GameObject obj, float tilingX, float tilingY)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            // Clone material to set unique tiling
            var mat = new Material(renderer.sharedMaterial);
            mat.mainTextureScale = new Vector2(tilingX, tilingY);
            renderer.sharedMaterial = mat;
        }
    }
}
