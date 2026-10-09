using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using System;
using System.IO;
public static class RepairBuild {
 public static void Run(){
  string evidence=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"RepairEvidence");Directory.CreateDirectory(evidence);
  foreach(var path in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Animations"})){var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(path));if(importer.npotScale!=TextureImporterNPOTScale.None||importer.wrapMode!=TextureWrapMode.Clamp||importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.maxTextureSize!=4096||importer.mipmapEnabled){importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();}}
  foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Art"})){string path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(importer.npotScale!=TextureImporterNPOTScale.None||importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.mipmapEnabled||!importer.alphaIsTransparency||importer.wrapMode!=TextureWrapMode.Clamp||importer.maxTextureSize!=4096){importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.SaveAndReimport();}}
  QualitySettings.vSyncCount=0;PlayerSettings.defaultScreenWidth=540;PlayerSettings.defaultScreenHeight=1320;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
  PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
  string build=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Builds/FishSort Local Prototype.app");Directory.CreateDirectory(Path.GetDirectoryName(build));
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/FishSort.unity"},locationPathName=build,target=BuildTarget.StandaloneOSX,options=BuildOptions.None});
  File.WriteAllText(Path.Combine(evidence,"build-result.txt"),"Unity "+Application.unityVersion+"\nResult: "+report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\nBytes: "+report.summary.totalSize+"\nDuration: "+report.summary.totalTime);
  if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Repair build failed");
  Debug.Log("REPAIR_BUILD_PASS");
 }
}
