using SpriteAtlasTools.Runtime;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace SpriteAtlasTools.Editor
{
    public class SpriteAtlasPacker
    {
        private static string filePathPrefix = "Assets/GameAssets";
        private static string packPropertyName = "AtlasPackProperty";
        private static string defaultPackProperty = string.Format("{1}/Sprites/{0}.asset", packPropertyName, filePathPrefix);
        private static string defaultStaticPackProperty = string.Format("{1}/Sprites/Static/{0}.asset", packPropertyName, filePathPrefix);
        private static string assetsBundleSuffix = ".unity3d";

        private static Dictionary<string, string> spritePathAtlasNameMap = new Dictionary<string, string>();

        [MenuItem("SpriteAtlas/CreateSpriteFolder")]
        public static void CreateSpriteFolder()
        {
            int divideIndex = filePathPrefix.IndexOf("/");
            if (divideIndex != -1)
            {
                string rootPath = Application.dataPath + "/" + filePathPrefix.Substring(divideIndex + 1);
                if (!Directory.Exists(rootPath))
                {
                    Directory.CreateDirectory(rootPath);
                }
            }
            
            if (!AssetDatabase.IsValidFolder(filePathPrefix + "/Sprites"))
            {
                AssetDatabase.CreateFolder(filePathPrefix, "Sprites");
            }
            if (!AssetDatabase.IsValidFolder(filePathPrefix + "/Sprites/Static"))
            {
                AssetDatabase.CreateFolder(filePathPrefix + "/Sprites", "Static");
            }
            if (!AssetDatabase.IsValidFolder(filePathPrefix + "/Sprites/Dynamic"))
            {
                AssetDatabase.CreateFolder(filePathPrefix + "/Sprites", "Dynamic");
            }

            AtlasPackProperty defaultProperty = AssetDatabase.LoadAssetAtPath<AtlasPackProperty>(defaultPackProperty);
            if (defaultProperty == null)
            {
                defaultProperty = ScriptableObject.CreateInstance<AtlasPackProperty>();
                defaultProperty.enabled = false;
                AssetDatabase.CreateAsset(defaultProperty, defaultPackProperty);
            }

            AtlasPackProperty defaultStaticProperty = AssetDatabase.LoadAssetAtPath<AtlasPackProperty>(defaultStaticPackProperty);
            if (defaultStaticProperty == null)
            {
                defaultStaticProperty = ScriptableObject.CreateInstance<AtlasPackProperty>();
                defaultStaticProperty.packUnit = -1;
                defaultStaticProperty.ignoreSize = new Vector2(4096, 4096);
                AssetDatabase.CreateAsset(defaultStaticProperty, defaultStaticPackProperty);
            }

            if (!AssetDatabase.IsValidFolder("Assets/Scripts"))
            {
                AssetDatabase.CreateFolder("Assets", "Scripts");
            }
            
            AssetDatabase.Refresh();
        }

        [MenuItem("SpriteAtlas/Pack")]
        public static void Pack()
        {
            spritePathAtlasNameMap.Clear();
            CreateSpriteFolder();
            PackFolder(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("/") + 1) + filePathPrefix + "/Sprites");
            GenerateConfigFile();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        [MenuItem("SpriteAtlas/ClearAssetBundleName")]
        public static void ClearAssetBundleName()
        {
            ClearAssetBundleName(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("/") + 1) + filePathPrefix + "/Sprites");
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        [MenuItem("SpriteAtlas/ClearStreamingAssets")]
        private static void ClearStreamingAssets()
        {
            string packagePath = Application.dataPath + "/StreamingAssets";
            if (Directory.Exists(packagePath))
                Directory.Delete(packagePath, true);
            AssetDatabase.Refresh();
        }

        [MenuItem("SpriteAtlas/PackAssetBundle")]
        private static void PackageBuddle()
        {
            string packagePath = Application.dataPath + "/StreamingAssets";
            if (!AssetDatabase.IsValidFolder("Assets/StreamingAssets"))
            {
                AssetDatabase.CreateFolder("Assets", "StreamingAssets");
            }

            BuildPipeline.BuildAssetBundles(packagePath, BuildAssetBundleOptions.None, BuildTarget.Android);

            AssetDatabase.Refresh();
        }


        private static void PackFolder(string path)
        {
            path = path.Replace("\\", "/");

            AssetDatabase.Refresh();
            string[] fileList = Directory.GetFiles(path);
            string releativePath = path.Substring(path.IndexOf(filePathPrefix));

            AtlasPackProperty property = GetPackProperty(releativePath);
            for (int index = 0; index < fileList.Length; index++)
            {
                FileInfo fileInfo = new FileInfo(fileList[index]);
                if (fileInfo.Name.EndsWith(".spriteatlas"))
                {
                    // 删除存在图集
                    string fileFullName = releativePath + "/" + fileInfo.Name;
                    AssetDatabase.DeleteAsset(fileFullName);
                }
            }
            Vector2 ignoreSpriteSize = property.ignoreSize;
            int spriteNoOfAtlas = 0;
            int atlasNo = 0;
            Object[] list = new Object[property.packUnit > 0 ? property.packUnit : fileList.Length];
            for (int index = 0; index < fileList.Length; index++)
            {
                FileInfo fileInfo = new FileInfo(fileList[index]);
                if (CheckFileValid(fileInfo))
                {
                    string fileFullName = releativePath + "/" + fileInfo.Name;
                    AssetImporter assetImporter = AssetImporter.GetAtPath(fileFullName);
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(fileFullName);
                    if (property.enabled && sprite.texture.width * sprite.texture.height < ignoreSpriteSize.x * ignoreSpriteSize.y && property.packUnit != 0)
                    {
                        string atlasName = null;

                        // 启用图集
                        if (property.packUnit > 0)
                        {
                            if (spriteNoOfAtlas < property.packUnit)
                                spriteNoOfAtlas = spriteNoOfAtlas + 1;
                            else
                            {
                                spriteNoOfAtlas = 1;
                                atlasNo = atlasNo + 1;
                                list = new Object[property.packUnit];
                            }
                            atlasName = string.Format("atlas@{0}", atlasNo);
                        }
                        else if (property.packUnit == -1)
                        {
                            spriteNoOfAtlas = spriteNoOfAtlas + 1;
                            atlasName = "atlas";
                        }

                        // 还没有创建过图集
                        SpriteAtlas spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(releativePath + "/" + atlasName);
                        if (spriteAtlas == null)
                        {
                            spriteAtlas = new SpriteAtlas();
                            AssetDatabase.CreateAsset(spriteAtlas, releativePath + "/" + atlasName + ".spriteatlas");
                        }

                        string abName = releativePath.Substring(filePathPrefix.Length + 1) + "/" + atlasName + assetsBundleSuffix;
                        spritePathAtlasNameMap.Add(releativePath.Substring(filePathPrefix.Length + 1) + "/" + fileInfo.Name.Substring(0, fileInfo.Name.LastIndexOf(".")), abName);
                        assetImporter.assetBundleName = abName;

                        // sprite加入图集
                        list[spriteNoOfAtlas - 1] = sprite;
                        spriteAtlas.Add(list);

                        SpriteAtlasPackingSettings saPackSetting = new SpriteAtlasPackingSettings();
                        saPackSetting.enableRotation = false;
                        saPackSetting.enableTightPacking = false;
                        saPackSetting.padding = 4;
                        spriteAtlas.SetPackingSettings(saPackSetting);

                        TextureImporterPlatformSettings texImpPlatSettings = spriteAtlas.GetPlatformSettings("iPhone");
                        texImpPlatSettings.overridden = true;
                        texImpPlatSettings.format = TextureImporterFormat.ASTC_RGBA_8x8;
                        spriteAtlas.SetPlatformSettings(texImpPlatSettings);
                        EditorUtility.DisplayProgressBar(string.Format("Packing: {0}", releativePath), string.Format("Atlas: {0}  Progress: {1}/{2}  PackUnit: {3}", property.enabled, index, fileList.Length, property.packUnit), (float)index / (float)fileList.Length);
                    }
                    else
                    {
                        //禁用图集
                        assetImporter.assetBundleName = releativePath.Substring(filePathPrefix.Length + 1) + "/" + fileInfo.Name.Substring(0, fileInfo.Name.LastIndexOf(".")) + assetsBundleSuffix;
                        EditorUtility.DisplayProgressBar(string.Format("Packing: {0}", releativePath), string.Format("Atlas: {0}  Progress: {1}/{2}", property.enabled, index, fileList.Length), (float)index / (float)fileList.Length);
                    }
                }
            }
            AssetDatabase.SaveAssets();
            string[] dirList = Directory.GetDirectories(path);
            for (int index = 0; index < dirList.Length; index++)
            {
                PackFolder(dirList[index]);
            }

        }

        private static void ClearAssetBundleName(string path)
        {
            path = path.Replace("\\", "/");
            string[] fileList = Directory.GetFiles(path);
            string releativePath = path.Substring(path.IndexOf(filePathPrefix));
            for (int index = 0; index < fileList.Length; index++)
            {
                FileInfo fileInfo = new FileInfo(fileList[index]);
                if (CheckFileValid(fileInfo))
                {
                    string fileFullName = releativePath + "/" + fileInfo.Name;
                    AssetImporter assetImporter = AssetImporter.GetAtPath(fileFullName);
                    assetImporter.assetBundleName = null;
                    EditorUtility.DisplayProgressBar(string.Format("Clear AssetBundleName: {0}", releativePath.Substring(filePathPrefix.Length + 1)), fileFullName, (float)index / (float)fileList.Length);
                }
            }
            string[] dirList = Directory.GetDirectories(path);
            for (int index = 0; index < dirList.Length; index++)
            {
                ClearAssetBundleName(dirList[index]);
            }

        }

        private static bool CheckFileValid(FileInfo fileInfo)
        {
            if ((fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                return false;

            if (fileInfo.Name.EndsWith(".meta"))
                return false;

            if (fileInfo.Name.EndsWith(".spriteatlas"))
                return false;

            if (fileInfo.Name.Contains(packPropertyName))
                return false;

            return true;
        }

        private static AtlasPackProperty GetPackProperty(string path)
        {
            AtlasPackProperty property = AssetDatabase.LoadAssetAtPath<AtlasPackProperty>(string.Format("{0}/{1}.asset", path, packPropertyName));
            if (property == null)
            {
                int index = path.LastIndexOf("/");
                if (index == -1)
                    return null;
                else
                    return GetPackProperty(path.Substring(0, index));
            }
            else
                return property;
        }

        private static void GenerateConfigFile()
        {
           
            if (!AssetDatabase.IsValidFolder("Assets/StreamingAssets"))
            {
                AssetDatabase.CreateFolder("Assets", "StreamingAssets");
            }

            string jsonFileName = Application.streamingAssetsPath + "/AtlasConfig.json";
            FileStream jsonFileStream = File.Open(jsonFileName, FileMode.Create);
            StreamWriter jsonFileSW = new StreamWriter(jsonFileStream);
            jsonFileSW.Write(JsonUtility.ToJson(new Serialization<string, string>(spritePathAtlasNameMap)));
            jsonFileSW.Close();
            jsonFileStream.Close();

            AssetDatabase.Refresh();
        }
    }
}

