using UnityEngine;
using UnityEditor;
using System.IO;

public static class ProceduralTextures
{
    private const string TexturePath = "Assets/_Project/Art/Textures";
    private const string MaterialPath = "Assets/_Project/Materials";

    public static void EnsureDirectories()
    {
        if (!Directory.Exists(TexturePath)) Directory.CreateDirectory(TexturePath);
        if (!Directory.Exists(MaterialPath)) Directory.CreateDirectory(MaterialPath);
    }

    // =========================================================================
    // STONE TEXTURE
    // =========================================================================
    public static Texture2D GenerateStoneTexture(int width = 512, int height = 512)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        float scale = 6f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (float)x / width;
                float ny = (float)y / height;

                // Multi-octave Perlin noise for stone-like pattern
                float n1 = Mathf.PerlinNoise(nx * scale, ny * scale);
                float n2 = Mathf.PerlinNoise(nx * scale * 2.5f + 50f, ny * scale * 2.5f + 50f) * 0.5f;
                float n3 = Mathf.PerlinNoise(nx * scale * 5f + 100f, ny * scale * 5f + 100f) * 0.25f;
                float noise = (n1 + n2 + n3) / 1.75f;

                // Stone block pattern - create mortar lines
                float blockX = Mathf.Abs(Mathf.Sin(nx * Mathf.PI * 4f));
                float blockY = Mathf.Abs(Mathf.Sin(ny * Mathf.PI * 6f));
                float offsetRow = (int)(ny * 6f) % 2 == 0 ? 0.5f : 0f;
                float blockXShifted = Mathf.Abs(Mathf.Sin((nx + offsetRow) * Mathf.PI * 4f));

                float mortar = Mathf.Min(blockXShifted, blockY);
                mortar = Mathf.Pow(mortar, 0.15f);

                // Base stone colors (grey-brown)
                float baseR = Mathf.Lerp(0.25f, 0.4f, noise);
                float baseG = Mathf.Lerp(0.22f, 0.35f, noise);
                float baseB = Mathf.Lerp(0.2f, 0.3f, noise);

                // Darken mortar lines
                float mortarDarken = Mathf.Lerp(0.4f, 1f, mortar);
                baseR *= mortarDarken;
                baseG *= mortarDarken;
                baseB *= mortarDarken;

                // Add subtle color variation
                float colorVar = Mathf.PerlinNoise(nx * 12f + 200f, ny * 12f + 200f) * 0.08f;
                baseR += colorVar;
                baseG += colorVar * 0.7f;

                tex.SetPixel(x, y, new Color(
                    Mathf.Clamp01(baseR),
                    Mathf.Clamp01(baseG),
                    Mathf.Clamp01(baseB),
                    1f));
            }
        }

        tex.Apply();
        return tex;
    }

    // =========================================================================
    // WOOD TEXTURE
    // =========================================================================
    public static Texture2D GenerateWoodTexture(int width = 512, int height = 128)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        float grainScale = 20f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (float)x / width;
                float ny = (float)y / height;

                // Horizontal wood grain
                float grain = Mathf.PerlinNoise(nx * grainScale, ny * 2f);
                float fineGrain = Mathf.PerlinNoise(nx * grainScale * 3f + 30f, ny * 4f + 30f) * 0.3f;
                float combined = grain + fineGrain;

                // Wood ring effect
                float ring = Mathf.Sin(ny * Mathf.PI * 8f + combined * 3f) * 0.5f + 0.5f;

                // Base wood colors (warm brown)
                float baseVal = Mathf.Lerp(0.3f, 0.55f, combined * 0.7f + ring * 0.3f);
                float r = baseVal * 1.1f;
                float g = baseVal * 0.75f;
                float b = baseVal * 0.45f;

                // Edge darkening (plank borders)
                float edgeY = 1f - Mathf.Pow(Mathf.Abs(ny - 0.5f) * 2f, 4f) * 0.3f;
                r *= edgeY;
                g *= edgeY;
                b *= edgeY;

                // Subtle knots
                float knotNoise = Mathf.PerlinNoise(nx * 3f + 70f, ny * 3f + 70f);
                if (knotNoise > 0.75f)
                {
                    float knotStrength = (knotNoise - 0.75f) * 4f;
                    r = Mathf.Lerp(r, r * 0.5f, knotStrength * 0.5f);
                    g = Mathf.Lerp(g, g * 0.4f, knotStrength * 0.5f);
                    b = Mathf.Lerp(b, b * 0.3f, knotStrength * 0.5f);
                }

                tex.SetPixel(x, y, new Color(
                    Mathf.Clamp01(r),
                    Mathf.Clamp01(g),
                    Mathf.Clamp01(b),
                    1f));
            }
        }

        tex.Apply();
        return tex;
    }

    // =========================================================================
    // PARCHMENT TEXTURE
    // =========================================================================
    public static Texture2D GenerateParchmentTexture(int width = 512, int height = 512)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, true);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (float)x / width;
                float ny = (float)y / height;

                // Base parchment color with noise
                float n1 = Mathf.PerlinNoise(nx * 8f, ny * 8f);
                float n2 = Mathf.PerlinNoise(nx * 16f + 40f, ny * 16f + 40f) * 0.3f;
                float n3 = Mathf.PerlinNoise(nx * 32f + 80f, ny * 32f + 80f) * 0.15f;
                float noise = (n1 + n2 + n3) / 1.45f;

                // Warm parchment base
                float r = Mathf.Lerp(0.72f, 0.85f, noise);
                float g = Mathf.Lerp(0.62f, 0.75f, noise);
                float b = Mathf.Lerp(0.42f, 0.55f, noise);

                // Age stains
                float stain = Mathf.PerlinNoise(nx * 4f + 120f, ny * 4f + 120f);
                if (stain > 0.6f)
                {
                    float stainStrength = (stain - 0.6f) * 2.5f * 0.15f;
                    r -= stainStrength;
                    g -= stainStrength * 1.2f;
                    b -= stainStrength * 1.5f;
                }

                // Edge darkening (burnt edges effect)
                float edgeDist = Mathf.Min(
                    Mathf.Min(nx, 1f - nx),
                    Mathf.Min(ny, 1f - ny));
                float edgeDarken = Mathf.SmoothStep(0f, 0.15f, edgeDist);
                r *= Mathf.Lerp(0.5f, 1f, edgeDarken);
                g *= Mathf.Lerp(0.4f, 1f, edgeDarken);
                b *= Mathf.Lerp(0.3f, 1f, edgeDarken);

                tex.SetPixel(x, y, new Color(
                    Mathf.Clamp01(r),
                    Mathf.Clamp01(g),
                    Mathf.Clamp01(b),
                    1f));
            }
        }

        tex.Apply();
        return tex;
    }

    // =========================================================================
    // FLOOR STONE TEXTURE (darker, larger blocks)
    // =========================================================================
    public static Texture2D GenerateFloorTexture(int width = 512, int height = 512)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        float scale = 4f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (float)x / width;
                float ny = (float)y / height;

                float n1 = Mathf.PerlinNoise(nx * scale * 3f, ny * scale * 3f);
                float n2 = Mathf.PerlinNoise(nx * scale * 6f + 60f, ny * scale * 6f + 60f) * 0.4f;
                float noise = (n1 + n2) / 1.4f;

                // Grid pattern for floor tiles
                float tileX = Mathf.Abs(Mathf.Sin(nx * Mathf.PI * scale));
                float tileY = Mathf.Abs(Mathf.Sin(ny * Mathf.PI * scale));
                float tile = Mathf.Min(tileX, tileY);
                tile = Mathf.Pow(tile, 0.1f);

                // Darker stone floor
                float baseVal = Mathf.Lerp(0.15f, 0.28f, noise) * Mathf.Lerp(0.5f, 1f, tile);
                float r = baseVal * 1.0f;
                float g = baseVal * 0.95f;
                float b = baseVal * 0.9f;

                tex.SetPixel(x, y, new Color(
                    Mathf.Clamp01(r),
                    Mathf.Clamp01(g),
                    Mathf.Clamp01(b),
                    1f));
            }
        }

        tex.Apply();
        return tex;
    }

    // =========================================================================
    // SAVE HELPERS
    // =========================================================================
    public static string SaveTexture(Texture2D tex, string name)
    {
        EnsureDirectories();
        byte[] png = tex.EncodeToPNG();
        string path = $"{TexturePath}/{name}.png";
        File.WriteAllBytes(path, png);
        AssetDatabase.ImportAsset(path);

        // Set texture import settings
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        return path;
    }

    public static string SaveTextureAsSprite(Texture2D tex, string name)
    {
        EnsureDirectories();
        byte[] png = tex.EncodeToPNG();
        string path = $"{TexturePath}/{name}.png";
        File.WriteAllBytes(path, png);
        AssetDatabase.ImportAsset(path);

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        return path;
    }

    public static Material CreateUnlitMaterial(string texturePath, string matName)
    {
        EnsureDirectories();

        // Use URP Unlit shader
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Texture");

        var mat = new Material(shader);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (tex != null)
        {
            mat.mainTexture = tex;
        }

        string matPath = $"{MaterialPath}/{matName}.mat";
        AssetDatabase.CreateAsset(mat, matPath);
        return mat;
    }

    public static Material CreateLitMaterial(string texturePath, string matName, float smoothness = 0.1f)
    {
        EnsureDirectories();

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (tex != null)
        {
            mat.mainTexture = tex;
        }
        mat.SetFloat("_Smoothness", smoothness);

        string matPath = $"{MaterialPath}/{matName}.mat";
        AssetDatabase.CreateAsset(mat, matPath);
        return mat;
    }

    // =========================================================================
    // PROCEDURAL ICONS
    // =========================================================================
    private const string IconPath = "Assets/_Project/Art/Textures/Icons";

    private static void DrawCircle(Color[] px, int w, int cx, int cy, int r, Color c)
    {
        for (int y = -r; y <= r; y++)
            for (int x = -r; x <= r; x++)
                if (x*x + y*y <= r*r) SetPx(px, w, cx+x, cy+y, c);
    }
    private static void DrawRing(Color[] px, int w, int cx, int cy, int r, int thick, Color c)
    {
        int r2 = r - thick;
        for (int y = -r; y <= r; y++)
            for (int x = -r; x <= r; x++)
            { int d = x*x+y*y; if (d <= r*r && d >= r2*r2) SetPx(px, w, cx+x, cy+y, c); }
    }
    private static void DrawRect(Color[] px, int w, int x1, int y1, int x2, int y2, Color c)
    {
        for (int y = y1; y <= y2; y++)
            for (int x = x1; x <= x2; x++) SetPx(px, w, x, y, c);
    }
    private static void DrawLine(Color[] px, int w, int x1, int y1, int x2, int y2, int thick, Color c)
    {
        int dx = Mathf.Abs(x2-x1), dy = Mathf.Abs(y2-y1);
        int steps = Mathf.Max(dx, dy);
        if (steps == 0) { SetPx(px, w, x1, y1, c); return; }
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i/steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(x1, x2, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(y1, y2, t));
            for (int ty = -thick; ty <= thick; ty++)
                for (int tx = -thick; tx <= thick; tx++)
                    SetPx(px, w, x+tx, y+ty, c);
        }
    }
    private static void SetPx(Color[] px, int w, int x, int y, Color c)
    {
        if (x >= 0 && x < w && y >= 0 && y < w) px[y*w+x] = c;
    }
    private static Texture2D MakeIcon(int size, System.Action<Color[], int> draw)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size]; // All transparent
        draw(px, size);
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }
    private static string SaveIcon(Texture2D tex, string name)
    {
        Directory.CreateDirectory(IconPath);
        return SaveTextureAsSprite(tex, "Icons/" + name);
    }

    // Equipment slot icons (64x64, silhouette on transparent)
    public static Sprite GenerateSlotIcon(string slotName)
    {
        Color fg = new Color(0.5f, 0.45f, 0.35f, 0.6f); // Subtle warm gray
        int S = 64;

        var tex = MakeIcon(S, (px, w) => {
            switch (slotName)
            {
                case "Head": // Helmet
                    DrawCircle(px, w, 32, 38, 16, fg);
                    DrawRect(px, w, 16, 20, 48, 34, fg);
                    DrawRect(px, w, 22, 16, 42, 20, fg); // visor
                    break;
                case "Chest": // Chestplate
                    DrawRect(px, w, 18, 16, 46, 50, fg);
                    DrawRect(px, w, 10, 40, 18, 50, fg); // shoulder L
                    DrawRect(px, w, 46, 40, 54, 50, fg); // shoulder R
                    DrawLine(px, w, 32, 50, 32, 20, 1, fg);
                    break;
                case "Legs": // Leg armor
                    DrawRect(px, w, 18, 14, 28, 52, fg);
                    DrawRect(px, w, 36, 14, 46, 52, fg);
                    DrawRect(px, w, 16, 30, 30, 36, fg); // knee L
                    DrawRect(px, w, 34, 30, 48, 36, fg); // knee R
                    break;
                case "Feet": // Boot
                    DrawRect(px, w, 20, 20, 34, 50, fg); // shaft
                    DrawRect(px, w, 18, 14, 44, 22, fg); // sole
                    DrawRect(px, w, 34, 14, 44, 30, fg); // toe
                    break;
                case "Weapon": // Sword
                    DrawLine(px, w, 32, 8, 32, 48, 2, fg); // blade
                    DrawRect(px, w, 22, 44, 42, 48, fg); // guard
                    DrawRect(px, w, 29, 48, 35, 56, fg); // grip
                    DrawCircle(px, w, 32, 58, 3, fg); // pommel
                    break;
                case "Shield": // Shield
                    DrawCircle(px, w, 32, 34, 18, fg);
                    DrawCircle(px, w, 32, 34, 12, new Color(fg.r, fg.g, fg.b, fg.a*0.5f));
                    DrawCircle(px, w, 32, 34, 5, fg);
                    break;
                case "Back": // Cape
                    DrawRect(px, w, 20, 42, 44, 50, fg); // collar
                    for (int y = 10; y < 42; y++)
                    {
                        float t = (42f - y) / 32f;
                        int half = (int)(10 + t * 12);
                        DrawRect(px, w, 32-half, y, 32+half, y, fg);
                    }
                    break;
                case "Amulet": // Pendant
                    DrawLine(px, w, 20, 52, 32, 44, 1, fg); // chain L
                    DrawLine(px, w, 44, 52, 32, 44, 1, fg); // chain R
                    DrawCircle(px, w, 32, 32, 10, fg); // gem
                    DrawCircle(px, w, 32, 32, 6, new Color(0.3f, 0.7f, 0.8f, 0.5f)); // glow
                    break;
                case "Accessory": // Ring
                    DrawRing(px, w, 32, 32, 16, 4, fg);
                    DrawCircle(px, w, 32, 48, 5, new Color(0.6f, 0.3f, 0.8f, 0.5f)); // gem
                    break;
            }
        });

        string path = SaveIcon(tex, "Slot_" + slotName);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // Stat icons (32x32, colored)
    public static Sprite GenerateStatIcon(string statName)
    {
        int S = 32;
        Color c;
        var tex = MakeIcon(S, (px, w) => {
            switch (statName)
            {
                case "ATK":
                    c = new Color(0.9f, 0.2f, 0.15f, 0.9f);
                    DrawLine(px, w, 16, 4, 16, 24, 1, c);
                    DrawRect(px, w, 10, 22, 22, 24, c);
                    DrawRect(px, w, 14, 24, 18, 28, c);
                    break;
                case "DEF":
                    c = new Color(0.2f, 0.4f, 0.9f, 0.9f);
                    DrawCircle(px, w, 16, 16, 10, c);
                    DrawCircle(px, w, 16, 16, 6, new Color(c.r, c.g, c.b, 0.4f));
                    break;
                case "MAG":
                    c = new Color(0.6f, 0.2f, 0.9f, 0.9f);
                    DrawCircle(px, w, 16, 16, 6, c);
                    DrawLine(px, w, 16, 4, 16, 28, 1, c);
                    DrawLine(px, w, 4, 16, 28, 16, 1, c);
                    DrawLine(px, w, 8, 8, 24, 24, 1, c);
                    DrawLine(px, w, 24, 8, 8, 24, 1, c);
                    break;
                case "SPD":
                    c = new Color(0.9f, 0.8f, 0.1f, 0.9f);
                    DrawLine(px, w, 20, 28, 12, 18, 1, c);
                    DrawLine(px, w, 12, 18, 20, 14, 1, c);
                    DrawLine(px, w, 20, 14, 12, 4, 1, c);
                    break;
                case "CRT":
                    c = new Color(0.9f, 0.5f, 0.1f, 0.9f);
                    DrawRing(px, w, 16, 16, 10, 2, c);
                    DrawRing(px, w, 16, 16, 5, 2, c);
                    DrawCircle(px, w, 16, 16, 2, c);
                    break;
                case "VIT":
                    c = new Color(0.9f, 0.15f, 0.3f, 0.9f);
                    DrawCircle(px, w, 11, 20, 6, c);
                    DrawCircle(px, w, 21, 20, 6, c);
                    for (int y = 6; y < 18; y++)
                    {
                        float t = (float)(y - 6) / 12f;
                        int half = (int)(2 + t * 10);
                        DrawRect(px, w, 16-half, y, 16+half, y, c);
                    }
                    break;
            }
        });

        string path = SaveIcon(tex, "Stat_" + statName);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // Weapon icons (64x64) — unique silhouette per weapon + rarity tint
    public static Sprite GenerateWeaponIcon(string weaponName, Color rarityColor)
    {
        int S = 64;
        Color fg = new Color(Mathf.Min(1, rarityColor.r * 0.6f + 0.5f), Mathf.Min(1, rarityColor.g * 0.6f + 0.5f), Mathf.Min(1, rarityColor.b * 0.6f + 0.5f), 1f);
        Color hi = new Color(Mathf.Min(1, fg.r + 0.2f), Mathf.Min(1, fg.g + 0.2f), Mathf.Min(1, fg.b + 0.2f), 1f);
        Color dk = new Color(fg.r * 0.6f + 0.1f, fg.g * 0.6f + 0.1f, fg.b * 0.6f + 0.1f, 1f);

        // Background color: dark tinted version of rarity color
        Color bg = new Color(rarityColor.r * 0.15f + 0.05f, rarityColor.g * 0.15f + 0.05f, rarityColor.b * 0.15f + 0.05f, 1f);

        var tex = MakeIcon(S, (px, w) => {
            // Fill opaque background with rarity-tinted dark color
            for (int i = 0; i < px.Length; i++) px[i] = bg;

            // Subtle border in rarity color
            Color border = new Color(rarityColor.r * 0.4f + 0.1f, rarityColor.g * 0.4f + 0.1f, rarityColor.b * 0.4f + 0.1f, 1f);
            for (int x = 0; x < w; x++) { px[x] = border; px[(w-1)*w+x] = border; }
            for (int y = 0; y < w; y++) { px[y*w] = border; px[y*w+w-1] = border; }

            switch (weaponName)
            {
                // === SWORDS ===
                case "Rusty Shiv": // Short, chipped
                    DrawLine(px,w, 32,8, 32,38, 2, fg);
                    DrawLine(px,w, 30,10, 34,10, 1, hi); // chip
                    DrawRect(px,w, 26,38, 38,42, dk); // guard
                    DrawRect(px,w, 30,42, 34,54, dk); // grip
                    break;
                case "Iron Shortsword": // Clean short blade
                    DrawLine(px,w, 32,6, 32,36, 3, fg);
                    DrawLine(px,w, 33,6, 33,36, 1, hi);
                    DrawRect(px,w, 24,36, 40,40, fg); // guard
                    DrawRect(px,w, 29,40, 35,52, dk);
                    DrawCircle(px,w, 32,54, 3, dk);
                    break;
                case "Dungeon Longsword": // Long straight blade
                    DrawLine(px,w, 32,4, 32,40, 3, fg);
                    DrawLine(px,w, 34,4, 34,40, 1, hi);
                    DrawRect(px,w, 22,40, 42,44, fg);
                    DrawRect(px,w, 29,44, 35,56, dk);
                    DrawCircle(px,w, 32,58, 3, dk);
                    break;
                case "Rune Sabre": // Curved blade with rune marks
                    for(int y=6;y<40;y++) { int x=32+(int)((y-6)*0.15f); DrawLine(px,w,x-2,y,x+2,y,1,fg); }
                    DrawCircle(px,w, 34,12, 2, hi); // rune
                    DrawCircle(px,w, 35,24, 2, hi); // rune
                    DrawRect(px,w, 24,40, 40,44, fg);
                    DrawRect(px,w, 29,44, 35,56, dk);
                    break;
                case "Crescent Falchion": // Wide curved blade
                    for(int y=6;y<38;y++) { float t=(y-6f)/32f; int half=2+(int)(t*6); DrawRect(px,w,30,y,30+half,y,fg); }
                    DrawLine(px,w, 31,6, 31,38, 1, hi);
                    DrawRect(px,w, 24,38, 40,42, fg);
                    DrawRect(px,w, 29,42, 35,54, dk);
                    break;
                case "Cursed Estoc": // Thin, long, dark
                    DrawLine(px,w, 32,2, 32,42, 1, fg);
                    DrawLine(px,w, 33,2, 33,42, 1, fg);
                    DrawRect(px,w, 26,42, 38,45, fg);
                    DrawRect(px,w, 30,45, 34,56, dk);
                    DrawCircle(px,w, 32,4, 2, hi); // glow tip
                    break;
                case "Crystal Arming Sword": // Crystal blade
                    DrawLine(px,w, 32,4, 32,38, 3, fg);
                    DrawLine(px,w, 28,12, 32,4, 1, hi); // crystal facet
                    DrawLine(px,w, 36,12, 32,4, 1, hi);
                    DrawCircle(px,w, 32,20, 3, hi); // crystal glow
                    DrawRect(px,w, 24,38, 40,42, fg);
                    DrawRect(px,w, 29,42, 35,54, dk);
                    break;
                case "Soulreaver Blade": // Epic wide blade with souls
                    DrawLine(px,w, 32,2, 32,40, 4, fg);
                    DrawLine(px,w, 34,2, 34,40, 1, hi);
                    DrawCircle(px,w, 28,14, 2, hi); // soul
                    DrawCircle(px,w, 36,22, 2, hi); // soul
                    DrawCircle(px,w, 30,30, 2, hi); // soul
                    DrawRect(px,w, 20,40, 44,44, fg);
                    DrawRect(px,w, 28,44, 36,56, dk);
                    DrawCircle(px,w, 32,58, 4, fg);
                    break;

                // === AXES ===
                case "Woodcutter's Hatchet": // Small hatchet
                    DrawRect(px,w, 30,16, 34,54, dk); // handle
                    DrawRect(px,w, 18,8, 32,22, fg); // head
                    DrawLine(px,w, 18,8, 18,22, 1, hi);
                    break;
                case "Battle Axe": // Standard battle axe
                    DrawRect(px,w, 30,20, 34,56, dk);
                    DrawRect(px,w, 14,6, 32,24, fg);
                    DrawLine(px,w, 14,6, 14,24, 2, hi);
                    break;
                case "Bonecleaver": // Bone head
                    DrawRect(px,w, 30,18, 34,56, dk);
                    for(int y=4;y<22;y++){int half=6+(int)((y-4)*0.5f); DrawRect(px,w,32-half,y,32,y,fg);}
                    DrawLine(px,w, 22,8, 22,18, 1, hi);
                    break;
                case "Obsidian Greataxe": // Huge double-headed
                    DrawRect(px,w, 30,22, 34,58, dk);
                    DrawRect(px,w, 12,4, 30,22, fg); // left head
                    DrawRect(px,w, 34,4, 52,22, fg); // right head
                    DrawLine(px,w, 12,4, 12,22, 2, hi);
                    DrawLine(px,w, 52,4, 52,22, 2, hi);
                    break;
                case "Demonrend Axe": // Demon axe with veins
                    DrawRect(px,w, 30,18, 34,56, dk);
                    DrawRect(px,w, 14,4, 32,22, fg);
                    DrawLine(px,w, 20,8, 28,16, 1, hi); // vein
                    DrawLine(px,w, 18,14, 26,10, 1, hi); // vein
                    DrawCircle(px,w, 22,12, 2, hi); // glow
                    break;
                case "Abyssal Executioner": // Massive execution axe
                    DrawRect(px,w, 30,24, 34,60, dk);
                    for(int y=2;y<24;y++){int half=4+(int)((24-y)*0.8f); DrawRect(px,w,32-half,y,32+half,y,fg);}
                    DrawLine(px,w, 20,2, 20,24, 2, hi);
                    DrawLine(px,w, 44,2, 44,24, 2, hi);
                    break;

                // === DAGGERS ===
                case "Thief's Knife": // Small knife
                    DrawLine(px,w, 32,14, 32,36, 2, fg);
                    DrawRect(px,w, 28,36, 36,39, dk);
                    DrawRect(px,w, 30,39, 34,50, dk);
                    break;
                case "Poisoned Stiletto": // Thin stiletto
                    DrawLine(px,w, 32,10, 32,38, 1, fg);
                    DrawLine(px,w, 33,10, 33,38, 1, new Color(0.2f,0.8f,0.2f,0.7f)); // poison
                    DrawRect(px,w, 28,38, 36,41, dk);
                    DrawRect(px,w, 30,41, 34,52, dk);
                    break;
                case "Shadow Fang": // Dark curved dagger
                    for(int y=12;y<38;y++){int x=32+(int)((y-12)*0.12f); SetPx(px,w,x-1,y,fg);SetPx(px,w,x,y,fg);SetPx(px,w,x+1,y,fg);}
                    DrawRect(px,w, 28,38, 36,41, dk);
                    DrawRect(px,w, 30,41, 34,50, dk);
                    break;
                case "Voidtouched Kris": // Wavy kris blade
                    for(int y=10;y<38;y++){int x=32+(int)(Mathf.Sin(y*0.5f)*3); SetPx(px,w,x-1,y,fg);SetPx(px,w,x,y,fg);SetPx(px,w,x+1,y,fg);}
                    DrawRect(px,w, 28,38, 36,41, dk);
                    DrawRect(px,w, 30,41, 34,50, dk);
                    DrawCircle(px,w, 32,25, 2, hi); // void glow
                    break;
                case "Phantom Tanto": // Short wide tanto
                    DrawRect(px,w, 28,14, 36,36, fg);
                    DrawLine(px,w, 28,14, 36,14, 1, hi); // edge
                    DrawRect(px,w, 28,36, 36,39, dk);
                    DrawRect(px,w, 30,39, 34,50, dk);
                    break;
                case "Soulsplitter Dagger": // Spectral dagger
                    DrawLine(px,w, 32,8, 32,38, 2, fg);
                    DrawLine(px,w, 28,16, 32,8, 1, hi); // spectral edge
                    DrawLine(px,w, 36,16, 32,8, 1, hi);
                    DrawCircle(px,w, 28,20, 2, hi); // soul
                    DrawCircle(px,w, 36,28, 2, hi); // soul
                    DrawRect(px,w, 26,38, 38,41, dk);
                    DrawRect(px,w, 29,41, 35,52, dk);
                    break;

                default: // Fallback generic blade
                    DrawLine(px,w, 32,8, 32,40, 2, fg);
                    DrawRect(px,w, 26,40, 38,44, dk);
                    DrawRect(px,w, 29,44, 35,54, dk);
                    break;
            }
        });

        string safeName = weaponName.Replace(" ", "_").Replace("'", "");
        string path = SaveIcon(tex, "Weapon_" + safeName);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // =========================================================================
    // UI SLOT TEXTURES
    // =========================================================================

    /// <summary>
    /// Generates a 128x128 medieval stone/iron slot frame with beveled edges,
    /// inner shadow, and subtle noise. Used as background for inventory slots.
    /// </summary>
    public static Sprite GenerateSlotFrame()
    {
        int S = 128;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        var px = new Color[S * S];

        int border = 5;
        int edgeLine = 1;
        int innerShadow = 14;
        int cornerR = 8;

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                // Corner rounding
                bool clipped = false;
                int[][] corners = { new[]{cornerR, cornerR}, new[]{S-1-cornerR, cornerR},
                                    new[]{cornerR, S-1-cornerR}, new[]{S-1-cornerR, S-1-cornerR} };
                foreach (var c in corners)
                {
                    bool inZone = (c[0] <= cornerR ? x < cornerR : x > S-1-cornerR) &&
                                  (c[1] <= cornerR ? y < cornerR : y > S-1-cornerR);
                    if (inZone)
                    {
                        int dx = x - c[0], dy = y - c[1];
                        if (dx * dx + dy * dy > cornerR * cornerR)
                        { clipped = true; break; }
                    }
                }
                if (clipped) { px[y * S + x] = Color.clear; continue; }

                float nx = (float)x / S;
                float ny = (float)y / S;
                int dLeft = x, dRight = S - 1 - x, dBottom = y, dTop = S - 1 - y;
                int dEdge = Mathf.Min(dLeft, dRight, dBottom, dTop);

                // Stone noise
                float n1 = Mathf.PerlinNoise(nx * 20f + 42f, ny * 20f + 42f);
                float n2 = Mathf.PerlinNoise(nx * 40f + 100f, ny * 40f + 100f) * 0.3f;
                float noise = n1 + n2;

                if (dEdge < border)
                {
                    // Beveled stone frame
                    float highlight = 0f;
                    if (dTop < border)    highlight += (1f - (float)dTop / border) * 0.45f;
                    if (dLeft < border)   highlight += (1f - (float)dLeft / border) * 0.3f;
                    if (dBottom < border) highlight -= (1f - (float)dBottom / border) * 0.45f;
                    if (dRight < border)  highlight -= (1f - (float)dRight / border) * 0.3f;

                    float v = 0.22f + highlight * 0.14f;
                    v *= (0.85f + noise * 0.18f);
                    px[y * S + x] = new Color(v * 1.08f, v * 0.94f, v * 0.78f, 1f);
                }
                else if (dEdge < border + edgeLine)
                {
                    // Dark inner edge line for depth
                    px[y * S + x] = new Color(0.03f, 0.025f, 0.02f, 1f);
                }
                else
                {
                    // Interior with shadow vignette
                    float sd = Mathf.Max(0, dEdge - border - edgeLine);
                    float t = Mathf.Clamp01(sd / (float)innerShadow);
                    t *= t; // quadratic easing

                    float v = Mathf.Lerp(0.035f, 0.085f, t);
                    v *= (0.92f + noise * 0.1f);
                    px[y * S + x] = new Color(v * 1.0f, v * 0.92f, v * 0.82f, 1f);
                }
            }
        }

        // Corner rivets (decorative dots at corners)
        int rivetOffset = border + 3;
        int[][] rivetPos = { new[]{rivetOffset, rivetOffset}, new[]{S-1-rivetOffset, rivetOffset},
                             new[]{rivetOffset, S-1-rivetOffset}, new[]{S-1-rivetOffset, S-1-rivetOffset} };
        foreach (var rp in rivetPos)
        {
            DrawCircle(px, S, rp[0], rp[1], 2, new Color(0.30f, 0.26f, 0.20f, 0.7f));
            SetPx(px, S, rp[0], rp[1] + 1, new Color(0.38f, 0.34f, 0.26f, 0.5f)); // highlight dot
        }

        tex.SetPixels(px);
        tex.Apply();
        string path = SaveTextureAsSprite(tex, "SlotFrame");
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>
    /// Generates a 128x128 border-only frame (white, transparent inside).
    /// Tinted at runtime with rarity color for inventory slot overlays.
    /// </summary>
    public static Sprite GenerateRarityBorder()
    {
        int S = 128;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        var px = new Color[S * S];

        int inset = 3;
        int thickness = 3;
        int cornerR = 7;

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                // Corner rounding
                bool clipped = false;
                int[][] corners = { new[]{cornerR, cornerR}, new[]{S-1-cornerR, cornerR},
                                    new[]{cornerR, S-1-cornerR}, new[]{S-1-cornerR, S-1-cornerR} };
                foreach (var c in corners)
                {
                    bool inZone = (c[0] <= cornerR ? x < cornerR : x > S-1-cornerR) &&
                                  (c[1] <= cornerR ? y < cornerR : y > S-1-cornerR);
                    if (inZone)
                    {
                        int dx = x - c[0], dy = y - c[1];
                        if (dx * dx + dy * dy > cornerR * cornerR)
                        { clipped = true; break; }
                    }
                }
                if (clipped) continue; // px stays transparent

                int dLeft = x, dRight = S - 1 - x, dBottom = y, dTop = S - 1 - y;
                int dEdge = Mathf.Min(dLeft, dRight, dBottom, dTop);

                if (dEdge >= inset && dEdge < inset + thickness)
                {
                    // Border with soft inner/outer edges
                    float alpha = 1f;
                    if (dEdge == inset) alpha = 0.5f;
                    if (dEdge == inset + thickness - 1) alpha = 0.5f;
                    px[y * S + x] = new Color(1f, 1f, 1f, alpha);
                }
                // else: stays transparent
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        string path = SaveTextureAsSprite(tex, "RarityBorder");
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>
    /// Generates a small 32x32 dark rounded badge for quantity text background.
    /// </summary>
    public static Sprite GenerateQuantityBadge()
    {
        int S = 32;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        var px = new Color[S * S];

        int cornerR = 6;
        Color bg = new Color(0.0f, 0.0f, 0.0f, 0.75f);

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                bool clipped = false;
                int[][] corners = { new[]{cornerR, cornerR}, new[]{S-1-cornerR, cornerR},
                                    new[]{cornerR, S-1-cornerR}, new[]{S-1-cornerR, S-1-cornerR} };
                foreach (var c in corners)
                {
                    bool inZone = (c[0] <= cornerR ? x < cornerR : x > S-1-cornerR) &&
                                  (c[1] <= cornerR ? y < cornerR : y > S-1-cornerR);
                    if (inZone)
                    {
                        int dx = x - c[0], dy = y - c[1];
                        if (dx * dx + dy * dy > cornerR * cornerR)
                        { clipped = true; break; }
                    }
                }
                if (!clipped) px[y * S + x] = bg;
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        string path = SaveTextureAsSprite(tex, "QuantityBadge");
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
