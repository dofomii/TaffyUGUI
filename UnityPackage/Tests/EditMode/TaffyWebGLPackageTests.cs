using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyWebGLPackageTests
    {
        private const string ArchiveSuffix = "/Plugins/WebGL/libtaffy_ugui.a";

        [Test]
        public void WebArchive_IsPresentAndImportedOnlyForWebGLPlayer()
        {
            string assetPath = FindWebArchiveAssetPath();
            Assert.That(File.Exists(assetPath), Is.True, $"Missing packaged Web archive at {assetPath}.");

            PluginImporter importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
            Assert.That(importer, Is.Not.Null, $"Expected PluginImporter for {assetPath}.");
            Assert.That(importer.GetCompatibleWithAnyPlatform(), Is.False, "Web archive must not use Any Platform.");
            Assert.That(importer.GetCompatibleWithEditor(), Is.False, "Editor must never attempt to load the Web archive.");
            Assert.That(importer.GetCompatibleWithPlatform(BuildTarget.WebGL), Is.True, "WebGL Player must include the Web archive.");

            BuildTarget[] nonWebTargets =
            {
                BuildTarget.StandaloneWindows,
                BuildTarget.StandaloneWindows64,
                BuildTarget.StandaloneOSX,
                BuildTarget.StandaloneLinux64,
                BuildTarget.Android,
                BuildTarget.iOS,
                BuildTarget.WSAPlayer,
            };

            foreach (BuildTarget target in nonWebTargets)
            {
                Assert.That(
                    importer.GetCompatibleWithPlatform(target),
                    Is.False,
                    $"Web archive must be disabled for non-Web target {target}.");
            }
        }

        [Test]
        public void WebArchive_HasCheckedInMetaFile()
        {
            string assetPath = FindWebArchiveAssetPath();
            Assert.That(File.Exists(assetPath + ".meta"), Is.True, "Web archive .meta must ship with the UPM/Git package.");
        }

        private static string FindWebArchiveAssetPath()
        {
            string path = AssetDatabase.FindAssets("libtaffy_ugui")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(candidate => candidate.EndsWith(ArchiveSuffix, StringComparison.Ordinal));

            Assert.That(path, Is.Not.Null.And.Not.Empty, "Could not locate Plugins/WebGL/libtaffy_ugui.a in the imported package.");
            return path;
        }
    }
}
