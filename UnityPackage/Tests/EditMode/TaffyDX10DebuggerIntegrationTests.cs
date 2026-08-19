using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TaffyUGUI.Editor;
using UnityEngine;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyDX10DebuggerIntegrationTests
    {
        [Test]
        public void DebuggerDataUsesSameComputedSnapshotAndDiagnosticResultsAsInspectorServices()
        {
            GameObject go = new GameObject("Debugger", typeof(RectTransform), typeof(TaffyLayoutGroup));
            try
            {
                RectTransform rect = (RectTransform)go.transform;
                rect.sizeDelta = new Vector2(360f, 180f);
                TaffyLayoutGroup group = go.GetComponent<TaffyLayoutGroup>();
                group.responsiveProfiles.Add(new TaffyResponsiveProfile
                {
                    name = "invalid",
                    minWidth = 500f,
                    maxWidth = 100f,
                });

                Assembly assembly = typeof(TaffyLayoutGroupEditor).Assembly;
                Type debuggerDataType = assembly.GetType("TaffyUGUI.Editor.TaffyDebuggerData");
                Type computedType = assembly.GetType("TaffyUGUI.Editor.TaffyComputedLayoutSnapshot");
                Type healthType = assembly.GetType("TaffyUGUI.Editor.TaffyLayoutHealth");
                Assert.That(debuggerDataType, Is.Not.Null);
                Assert.That(computedType, Is.Not.Null);
                Assert.That(healthType, Is.Not.Null);

                MethodInfo debuggerFrom = debuggerDataType.GetMethod("From", BindingFlags.Static | BindingFlags.NonPublic);
                object debuggerData = debuggerFrom.Invoke(null, new object[] { group });
                object debuggerComputed = debuggerDataType.GetProperty("Computed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(debuggerData);
                object debuggerHealth = debuggerDataType.GetProperty("Health", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(debuggerData);

                MethodInfo computedFrom = computedType.GetMethod("From", BindingFlags.Static | BindingFlags.NonPublic);
                object inspectorComputed = computedFrom.Invoke(null, new object[] { group });
                Vector2 debuggerSize = (Vector2)computedType.GetProperty("Size", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(debuggerComputed);
                Vector2 inspectorSize = (Vector2)computedType.GetProperty("Size", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(inspectorComputed);
                Assert.That(debuggerSize, Is.EqualTo(inspectorSize));

                MethodInfo evaluate = healthType.GetMethod("Evaluate", BindingFlags.Static | BindingFlags.NonPublic);
                object inspectorHealth = evaluate.Invoke(null, new object[] { new UnityEngine.Object[] { group } });
                string[] debuggerIds = ResultIds(healthType, debuggerHealth);
                string[] inspectorIds = ResultIds(healthType, inspectorHealth);
                Assert.That(debuggerIds, Is.Not.Empty);
                Assert.That(debuggerIds, Is.EqualTo(inspectorIds));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static string[] ResultIds(Type healthType, object health)
        {
            object resultsObject = healthType.GetProperty("Results", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(health);
            var ids = new List<string>();
            foreach (object result in (IEnumerable)resultsObject)
            {
                PropertyInfo id = result.GetType().GetProperty("Id", BindingFlags.Instance | BindingFlags.NonPublic);
                ids.Add((string)id.GetValue(result));
            }
            return ids.ToArray();
        }
    }
}
