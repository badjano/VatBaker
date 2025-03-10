﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using VatBaker;
using Random = UnityEngine.Random;

public static class VatBakerCore
{
    public static readonly int MainTex = Shader.PropertyToID("_MainTex");
    public static readonly int NormalTex = Shader.PropertyToID("_NormalTex");

    private static readonly int BaseShaderBumpMap = Shader.PropertyToID("_BumpMap");


    public static (Texture2D, Texture2D) BakeClip(string name, GameObject gameObject, SkinnedMeshRenderer skin,
        AnimationClip clip, int textureWidth, float fps, Space space)
    {
        var vertexCount = skin.sharedMesh.vertexCount;
        var frameCount = Mathf.FloorToInt(clip.length * fps) + 1;
        var blockCount = Mathf.CeilToInt((float)vertexCount / textureWidth);
        var textureHeight = blockCount * frameCount;
        var blockHeight = frameCount;

        Debug.Log($"textureWidth: {textureWidth}, textureHeight: {textureHeight} " +
                  $"vertexCount: {vertexCount}, frameCount: {frameCount}, blockCount: {blockCount}");

        var posTex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAHalf, false, true)
        {
            name = $"{name}.posTex",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Repeat
        };

        var normTex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAHalf, false, true)
        {
            name = $"{name}.normTex",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Repeat
        };

        using var poolVtx0 = ListPool<Vector3>.Get(out var tmpVertexList);
        using var poolVtx1 = ListPool<Vector3>.Get(out var localVertices);

        using var poolNorm0 = ListPool<Vector3>.Get(out var tmpNormalList);
        using var poolNorm1 = ListPool<Vector3>.Get(out var localNormals);

        // SkinnedMeshRenderer.BakeMesh() uses the transform
        // but is not used in the actual display, so it is reset during Bake
        using var tranScope = TransformCacheScope.ResetScope(skin.transform);

        var mesh = new Mesh();
        var dt = 1f / fps;
        for (var i = 0; i < frameCount; i++)
        {
            clip.SampleAnimation(gameObject, dt * i);
            skin.BakeMesh(mesh);

            mesh.GetVertices(tmpVertexList);
            mesh.GetNormals(tmpNormalList);

            var gap = blockCount * textureWidth - tmpVertexList.Count;
            if (gap > 0)
            {
                tmpVertexList = tmpVertexList.Concat(Enumerable.Repeat(Random.rotation * Vector3.one, gap)).ToList();
                tmpNormalList = tmpNormalList.Concat(Enumerable.Repeat(Random.rotation * Vector3.one, gap)).ToList();
            }

            localVertices.AddRange(tmpVertexList);
            localNormals.AddRange(tmpNormalList);
        }

        var trans = gameObject.transform;
        var (vertices, normals) = space switch
        {
            Space.Self => (
                localVertices.Select(vtx => trans.InverseTransformPoint(vtx)),
                localNormals.Select(norm => trans.InverseTransformDirection(norm))
            ),
            Space.World => (localVertices, localNormals),

            _ => throw new ArgumentOutOfRangeException(nameof(space), space, null)
        };

        var blockSize = (blockCount * textureWidth);
        for (int i = 0; i < frameCount; i++)
        {
            for (int block = 0; block < blockCount; block++)
            {
                int pixelX = 0;
                int pixelY = i + block * blockHeight;
                var index = block * textureWidth + i * blockSize;

                var blockVertices = vertices.Skip(index).Take(textureWidth);
                var blockNormals = normals.Skip(index).Take(textureWidth);

                Debug.Log($"x: {pixelX}, y: {pixelY}, start: {index}, length: {textureWidth}, count: {blockVertices.Count()}");
                posTex.SetPixels(pixelX, pixelY, textureWidth, 1, ListToColorArray(blockVertices));
                normTex.SetPixels(pixelX, pixelY, textureWidth, 1, ListToColorArray(blockNormals));
            }
        }

        return (posTex, normTex);

        static Color[] ListToColorArray(IEnumerable<Vector3> list) =>
            list.Select(v3 => new Color(v3.x, v3.y, v3.z)).ToArray();
    }

    public static void GenerateAssets(string name, SkinnedMeshRenderer skin, float fps, float animLength,
        Shader shader, Texture posTex, Texture normTex)
    {
        const string folderName = "VatBakerOutput";

        var folderPath = CombinePathAndCreateFolderIfNotExist("Assets", folderName, false);
        var subFolderPath = CombinePathAndCreateFolderIfNotExist(folderPath, name);

        var mat = new Material(shader)
        {
            enableInstancing = true
        };

        mat.SetTexture(MainTex, skin.sharedMaterial.mainTexture);
        var normalTex = skin.sharedMaterial.GetTexture(BaseShaderBumpMap);
        if (normalTex != null)
        {
            mat.SetTexture(NormalTex, normalTex);
        }

        mat.SetTexture(VatShaderProperty.VatPositionTex, posTex);
        mat.SetTexture(VatShaderProperty.VatNormalTex, normTex);
        mat.SetFloat(VatShaderProperty.VatAnimFps, fps);
        mat.SetFloat(VatShaderProperty.VatAnimLength, Mathf.Floor(animLength * fps) / fps);

        var go = new GameObject(name);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        go.AddComponent<MeshFilter>().sharedMesh = skin.sharedMesh;

        AssetDatabase.CreateAsset(posTex, CreatePath(subFolderPath, posTex.name, "asset"));
        AssetDatabase.CreateAsset(normTex, CreatePath(subFolderPath, normTex.name, "asset"));
        AssetDatabase.CreateAsset(mat, CreatePath(subFolderPath, name, "mat"));
        var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(go,
            CreatePath(subFolderPath, go.name, "prefab"),
            InteractionMode.AutomatedAction);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorGUIUtility.PingObject(prefab);

        static string CreatePath(string folder, string file, string extension)
            => Path.Combine(folder, $"{ReplaceInvalidPathChar(file)}.{extension}");
    }


    static string CombinePathAndCreateFolderIfNotExist(string parent, string folderName, bool unique = true)
    {
        parent = ReplaceInvalidPathChar(parent);
        folderName = ReplaceInvalidPathChar(folderName);

        var path = Path.Combine(parent, folderName);

        if (unique)
        {
            path = AssetDatabase.GenerateUniqueAssetPath(path);
        }

        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }

        return path;
    }

    static readonly string InvalidChars = new string(Path.GetInvalidPathChars());

    static string ReplaceInvalidPathChar(string path)
    {
        return Regex.Replace(path, $"[{InvalidChars}]", "_");
    }
}