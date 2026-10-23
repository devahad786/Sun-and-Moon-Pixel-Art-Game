using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;

namespace SunAndMoon.Editor
{
    public static class SunMoonWebBuild
    {
        [MenuItem("Sun & Moon/Configure WebGL")]
        public static void Configure()
        {
            PlayerSettings.productName="Sun & Moon";
            PlayerSettings.colorSpace=ColorSpace.Gamma;
            PlayerSettings.defaultWebScreenWidth=1280;
            PlayerSettings.defaultWebScreenHeight=800;
            PlayerSettings.runInBackground=false;
            PlayerSettings.resizableWindow=true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL,ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.template="PROJECT:SunAndMoon";
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback=true;
            PlayerSettings.WebGL.dataCaching=true;
            PlayerSettings.WebGL.initialMemorySize=128;
            PlayerSettings.WebGL.maximumMemorySize=512;
            // The game uses immediate-mode pixel art and does not need a 3D render pipeline.
            GraphicsSettings.defaultRenderPipeline=null;
            int quality=QualitySettings.GetQualityLevel();
            for(int i=0;i<QualitySettings.names.Length;i++) { QualitySettings.SetQualityLevel(i,false); QualitySettings.renderPipeline=null; }
            QualitySettings.SetQualityLevel(quality,false);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/SunAndMoon.unity",true)};
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Sun & Moon/Build WebGL")]
        public static void Build()
        {
            Configure();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{"Assets/Scenes/SunAndMoon.unity"},locationPathName="Builds/WebGL",target=BuildTarget.WebGL,options=BuildOptions.None});
            System.IO.File.WriteAllText("WebGL-build-result.txt",report.summary.result+"; errors="+report.summary.totalErrors+"; warnings="+report.summary.totalWarnings);
            if(report.summary.result!=BuildResult.Succeeded) throw new System.Exception("WebGL build failed: "+report.summary.result);
            Debug.Log("WebGL build succeeded. Serve Builds/WebGL over HTTP or HTTPS.");
        }
    }
}
