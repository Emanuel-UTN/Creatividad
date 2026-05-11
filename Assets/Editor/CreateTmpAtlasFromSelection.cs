using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CreateTmpAtlasFromSelection
{
    [MenuItem("Assets/Tools/Crear Atlas TMP desde Sprites seleccionados", priority = 2000)]
    public static void PackSelectedSpritesToAtlas()
    {
        Object[] selection = Selection.objects;
        var textures = new List<Texture2D>();
        var names = new List<string>();

        foreach (var obj in selection)
        {
            if (obj is Sprite)
            {
                var s = obj as Sprite;
                textures.Add(s.texture);
                names.Add(s.name);
            }
            else if (obj is Texture2D)
            {
                textures.Add(obj as Texture2D);
                names.Add(obj.name);
            }
        }

        if (textures.Count == 0)
        {
            EditorUtility.DisplayDialog("Crear Atlas TMP", "Selecciona al menos un Sprite o Texture2D en el Project window.", "OK");
            return;
        }

        // Make readable copies to avoid import settings issues
        var readableTextures = new List<Texture2D>();
        foreach (var t in textures)
        {
            readableTextures.Add(MakeTextureReadable(t));
        }

        int maxAtlasSize = 2048;
        var atlas = new Texture2D(maxAtlasSize, maxAtlasSize, TextureFormat.RGBA32, false);
        Rect[] rects = atlas.PackTextures(readableTextures.ToArray(), 2, maxAtlasSize);

        // Save atlas PNG
        string folder = "Assets/GeneratedAtlases";
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "tmp_atlas_" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".png");
        File.WriteAllBytes(path, atlas.EncodeToPNG());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        // Configure the imported texture to be Multiple sprites and set spritesheet metadata
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;

            int w = atlas.width;
            int h = atlas.height;

            var metas = new SpriteMetaData[rects.Length];
            for (int i = 0; i < rects.Length; i++)
            {
                Rect r = rects[i];
                Rect pixelRect = new Rect(r.x * w, r.y * h, r.width * w, r.height * h);
                var meta = new SpriteMetaData();
                meta.name = (i < names.Count && !string.IsNullOrEmpty(names[i])) ? names[i] : "sprite_" + i;
                meta.rect = pixelRect;
                // Set pivot to X=0, Y=0.25 as requested
                meta.pivot = new Vector2(0f, 0.25f);
                meta.alignment = (int)SpriteAlignment.Custom;
                metas[i] = meta;
            }

            // Use SerializedObject to set sprite sheet data (evita la API obsoleta)
            // Reduce pixels-per-unit to scale sprites up by 2x (halve PPU)
            try
            {
                int currentPPU = (int)importer.spritePixelsPerUnit;
                if (currentPPU <= 0) currentPPU = 100; // fallback default
                importer.spritePixelsPerUnit = Mathf.Max(1, Mathf.RoundToInt(currentPPU / 2f));
            }
            catch
            {
                // ignore if property not supported in this Unity version
            }

            var so = new SerializedObject(importer);
            var spritesProp = so.FindProperty("m_SpriteSheet.m_Sprites");
            if (spritesProp != null)
            {
                spritesProp.arraySize = metas.Length;
                for (int i = 0; i < metas.Length; i++)
                {
                    var elem = spritesProp.GetArrayElementAtIndex(i);
                    var nameProp = elem.FindPropertyRelative("m_Name");
                    if (nameProp != null) nameProp.stringValue = metas[i].name;

                    var rectProp = elem.FindPropertyRelative("m_Rect");
                    if (rectProp != null) rectProp.rectValue = metas[i].rect;

                    var pivotProp = elem.FindPropertyRelative("m_Pivot");
                    if (pivotProp != null) pivotProp.vector2Value = metas[i].pivot;

                    var alignProp = elem.FindPropertyRelative("m_Alignment");
                    if (alignProp != null) alignProp.intValue = metas[i].alignment;
                }

                so.ApplyModifiedProperties();
                importer.SaveAndReimport();
            }
            else
            {
                // Fallback: si la propiedad interna cambió en tu versión de Unity
                importer.SaveAndReimport();
            }
        }

        // Cleanup readable copies
        foreach (var rt in readableTextures)
        {
            if (rt != null) Object.DestroyImmediate(rt);
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Crear Atlas TMP", "Atlas creado en: " + path + "\nAhora crea el TMP Sprite Asset y su material manualmente desde TextMeshPro > Sprite Asset Creator.", "OK");
    }

    static Texture2D MakeTextureReadable(Texture2D source)
    {
        var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        readable.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return readable;
    }
}
