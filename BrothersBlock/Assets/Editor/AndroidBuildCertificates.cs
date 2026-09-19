#if UNITY_ANDROID
using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace BrothersBlockEditor
{
    // Optional machine-local trust store; it is never included in the APK.
    public sealed class AndroidBuildCertificates : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder { get { return 100; } }

        public void OnPostGenerateGradleAndroidProject(string unityLibraryPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string trustStore = Path.Combine(projectRoot, ".validation", "android-cacerts.p12");
            if (!File.Exists(trustStore)) return;
            string gradleRoot = Directory.GetParent(unityLibraryPath).FullName;
            string properties = Path.Combine(gradleRoot, "gradle.properties");
            string text = File.ReadAllText(properties);
            if (text.Contains("systemProp.javax.net.ssl.trustStore=")) return;
            File.AppendAllText(properties,
                "\nsystemProp.javax.net.ssl.trustStore=" + trustStore.Replace('\\', '/') +
                "\nsystemProp.javax.net.ssl.trustStorePassword=changeit\n");
        }
    }
}
#endif
