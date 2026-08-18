using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI.Editor
{
    internal enum TaffyDiagnosticSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }

    internal sealed class TaffyDiagnosticFix
    {
        internal TaffyDiagnosticFix(string id, string label, Action apply)
        {
            Id = id;
            Label = label;
            Apply = apply;
        }

        internal string Id { get; }
        internal string Label { get; }
        internal Action Apply { get; }

        internal void Invoke()
        {
            Apply?.Invoke();
        }
    }

    internal sealed class TaffyDiagnosticResult
    {
        internal TaffyDiagnosticResult(
            string id,
            string title,
            string message,
            TaffyDiagnosticSeverity severity,
            UnityEngine.Object target,
            string documentationUrl = null,
            params TaffyDiagnosticFix[] fixes)
        {
            Id = id;
            Title = title;
            Message = message;
            Severity = severity;
            Target = target;
            DocumentationUrl = documentationUrl;
            Fixes = fixes ?? Array.Empty<TaffyDiagnosticFix>();
        }

        internal string Id { get; }
        internal string Title { get; }
        internal string Message { get; }
        internal TaffyDiagnosticSeverity Severity { get; }
        internal UnityEngine.Object Target { get; }
        internal string DocumentationUrl { get; }
        internal IReadOnlyList<TaffyDiagnosticFix> Fixes { get; }
    }

    internal sealed class TaffyDiagnosticContext
    {
        internal TaffyDiagnosticContext(UnityEngine.Object target)
        {
            Target = target;
            Group = target as TaffyLayoutGroup;
            Item = target as TaffyLayoutItem;
            ParentGroup = Item ? TaffyItemActions.FindParentGroup(Item) : null;
        }

        internal UnityEngine.Object Target { get; }
        internal TaffyLayoutGroup Group { get; }
        internal TaffyLayoutItem Item { get; }
        internal TaffyLayoutGroup ParentGroup { get; }
    }

    internal abstract class TaffyDiagnosticRule
    {
        internal abstract string Id { get; }
        internal abstract void Evaluate(TaffyDiagnosticContext context, List<TaffyDiagnosticResult> results);
    }

    internal sealed class TaffyLayoutHealth
    {
        private static readonly TaffyDiagnosticRule[] Rules =
        {
            new MissingParentRule(),
            new CompetingUnityLayoutRule(),
            new ContentSizeFitterRule(),
            new AspectRatioFitterRule(),
            new IntegrationWarningsRule(),
            new IntrinsicMeasurementRule(),
            new ResponsiveProfilesRule(),
            new GridValidationRule(),
            new CalcValidationRule(),
            new FixedSizeResponsiveRule(),
            new RebuildSuppressionRule(),
        };

        private TaffyLayoutHealth(List<TaffyDiagnosticResult> results)
        {
            Results = results;
            HighestSeverity = TaffyDiagnosticSeverity.Info;
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].Severity > HighestSeverity)
                    HighestSeverity = results[i].Severity;
            }
        }

        internal IReadOnlyList<TaffyDiagnosticResult> Results { get; }
        internal bool IsHealthy => Results.Count == 0;
        internal TaffyDiagnosticSeverity HighestSeverity { get; }

        internal static TaffyLayoutHealth Evaluate(params UnityEngine.Object[] targets)
        {
            var results = new List<TaffyDiagnosticResult>();
            if (targets != null)
            {
                for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
                {
                    UnityEngine.Object target = targets[targetIndex];
                    if (!target)
                        continue;
                    var context = new TaffyDiagnosticContext(target);
                    for (int ruleIndex = 0; ruleIndex < Rules.Length; ruleIndex++)
                        Rules[ruleIndex].Evaluate(context, results);
                }
            }
            return new TaffyLayoutHealth(results);
        }
    }

    internal static class TaffyDiagnosticFixUtility
    {
        internal static void Record(UnityEngine.Object target, string undoName, Action mutation)
        {
            if (!target || mutation == null)
                return;
            Undo.RecordObject(target, undoName);
            mutation();
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
            if (target is TaffyLayoutGroup group)
                group.SetLayoutDirty();
        }

        internal static TaffyDiagnosticFix DisableBehaviour(string id, string label, Behaviour behaviour)
        {
            return new TaffyDiagnosticFix(id, label, () =>
            {
                if (!behaviour)
                    return;
                Record(behaviour, label, () => behaviour.enabled = false);
            });
        }

        internal static TaffyDiagnosticFix LetTaffyOwnFitterAxis(ContentSizeFitter fitter, bool horizontal)
        {
            string axis = horizontal ? "Horizontal" : "Vertical";
            return new TaffyDiagnosticFix("fitter.taffy." + axis.ToLowerInvariant(), "Let Taffy Own " + axis, () =>
            {
                if (!fitter)
                    return;
                Record(fitter, "Let Taffy own " + axis.ToLowerInvariant() + " axis", () =>
                {
                    if (horizontal)
                        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    else
                        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                });
            });
        }

        internal static TaffyDiagnosticFix LetUnityFitterOwnScrollContent(TaffyLayoutGroup group)
        {
            return new TaffyDiagnosticFix("scroll.unity-owner", "Let Unity Fitter Own Size", () =>
            {
                if (!group)
                    return;
                Record(group, "Let Unity fitter own ScrollRect content size", () =>
                    group.scrollRectContentMode = TaffyScrollRectContentMode.Disabled);
            });
        }

        internal static TaffyDiagnosticFix ResetGridPlacement(TaffyLayoutItem item)
        {
            return new TaffyDiagnosticFix("grid.reset-placement", "Reset Placement to Auto", () =>
            {
                if (!item)
                    return;
                Record(item, "Reset Taffy Grid placement", () =>
                {
                    item.gridRowStart = TaffyGridPlacement.Auto;
                    item.gridRowEnd = TaffyGridPlacement.Auto;
                    item.gridColumnStart = TaffyGridPlacement.Auto;
                    item.gridColumnEnd = TaffyGridPlacement.Auto;
                });
            });
        }
    }

    internal sealed class MissingParentRule : TaffyDiagnosticRule
    {
        internal override string Id => "item.missing-parent";

        internal override void Evaluate(TaffyDiagnosticContext context, List<TaffyDiagnosticResult> results)
        {
            if (!context.Item || context.ParentGroup)
                return;
            results.Add(new TaffyDiagnosticResult(
                Id,
                "No Taffy parent",
                "This TaffyLayoutItem has no TaffyLayoutGroup ancestor, so no Taffy container currently owns its layout.",
                TaffyDiagnosticSeverity.Warning,
                context.Item,
                null,
                new TaffyDiagnosticFix("item.add-parent", "Add Taffy Group to Parent", () => TaffyItemActions.AddGroupToParent(context.Item))));
        }
    }

    internal sealed class CompetingUnityLayoutRule : TaffyDiagnosticRule
    {
        internal override string Id => "group.competing-unity-layout";

        internal override void Evaluate(TaffyDiagnosticContext context, List<TaffyDiagnosticResult> results)
        {
            if (context.Group)
            {
                LayoutGroup[] groups = context.Group.GetComponents<LayoutGroup>();
                for (int i = 0; i < groups.Length; i++)
                {
                    LayoutGroup competing = groups[i];
                    if (!competing || competing == context.Group || !competing.enabled)
                        continue;
                    Add(competing, context.Group, results);
                }
                return;
            }

            if (!context.Item || context.ParentGroup || !context.Item.transform.parent)
                return;

            LayoutGroup parentLayout = context.Item.transform.parent.GetComponent<LayoutGroup>();
            if (parentLayout is HorizontalLayoutGroup || parentLayout is VerticalLayoutGroup || parentLayout is GridLayoutGroup)
                Add(parentLayout, context.Item, results);
        }

        private void Add(LayoutGroup competing, UnityEngine.Object target, List<TaffyDiagnosticResult> results)
        {
            string label = competing.GetType().Name;
            results.Add(new TaffyDiagnosticResult(
                Id + "." + label,
                "Competing Unity layout owner",
                label + " currently owns this item's parent layout, so the TaffyLayoutItem is not participating in a Taffy container. Choose one layout system for that parent.",
                TaffyDiagnosticSeverity.Error,
                target,
                null,
                TaffyDiagnosticFixUtility.DisableBehaviour("layout.disable." + label, "Disable " + label, competing)));
        }
    }

    internal sealed class ContentSizeFitterRule : TaffyDiagnosticRule
    {
        internal override string Id => "group.content-size-fitter";

        internal override void Evaluate(TaffyDiagnosticContext context, List<TaffyDiagnosticResult> results)
        {
            if (!context.Group)
                return;
            ContentSizeFitter fitter = context.Group.GetComponent<ContentSizeFitter>();
            if (!fitter || !fitter.enabled)
                return;

            if (fitter.horizontalFit != ContentSizeFitter.FitMode.Unconstrained)
                AddAxis(true, fitter, context, results);
            if (fitter.verticalFit != ContentSizeFitter.FitMode.Unconstrained)
                AddAxis(false, fitter, context, results);
        }

        private void AddAxis(bool horizontal, ContentSizeFitter fitter, TaffyDiagnosticContext context, List<TaffyDiagnosticResult> results)
        {
            string axis = horizontal ? "Horizontal" : "Vertical";
            results.Add(new TaffyDiagnosticResult(
                Id + "." + axis.ToLowerInvariant(),
                axis + " size has multiple owners",
                "ContentSizeFitter owns the " + axis.ToLowerInvariant() + " size while Taffy may also size this RectTransform. Choose one owner to avoid feedback or unexpected yielding.",
                TaffyDiagnosticSeverity.Warning,
                context.Group,
                null,
                TaffyDiagnosticFixUtility.LetTaffyOwnFitterAxis(fitter, horizontal),
                TaffyDiagnosticFixUtility.LetUnityFitterOwnScrollContent(context.Group)));
        }
    }

    internal sealed class AspectRatioFitterRule : TaffyDiagnosticRule
    {
        internal override string Id => "aspect-ratio-fitter";

        internal override void Evaluate(TaffyDiagnosticContext context, List<TaffyDiagnosticResult> results)
        {
            GameObject gameObject = context.Group ? context.Group.gameObject : context.Item ? context.Item.gameObject : null;
            if (!gameObject)
                return;
            AspectRatioFitter fitter = gameObject.GetComponent<AspectRatioFitter>();
            if (!fitter || !fitter.enabled || fitter.aspectMode == AspectRatioFitter.AspectMode.None)
                return;

            bool unsafeParentMode = fitter.aspectMode == AspectRatioFitter.AspectMode.FitInParent ||
                                    fitter.aspectMode == AspectRatioFitter.AspectMode.EnvelopeParent;
            if (!context.Group && !unsafeParentMode)
                return;

            UnityEngine.Object target = context.Group ? (UnityEngine.Object)context.Group : context.Item;
            results.Add(new TaffyDiagnosticResult(
                Id,
                "AspectRatioFitter also owns geometry",
                "AspectRatioFitter " + fitter.aspectMode + " can write RectTransform size/anchors after Taffy. Prefer Taffy aspect-ratio authoring when Taffy should own the geometry.",
                unsafeParentMode ? TaffyDiagnosticSeverity.Warning : TaffyDiagnosticSeverity.Info,
                target,
                null,
                TaffyDiagnosticFixUtility.DisableBehaviour("aspect.disable", "Disable AspectRatioFitter", fitter)));
        }
    }

    internal sealed class IntegrationWarningsRule : TaffyDiagnosticRule
    {
        internal override string Id => "group.integration";

        internal override void Evaluate(TaffyDiagnosticContext context, List<TaffyDiagnosticResult> results)
        {
            if (!context.Group)
                return;
            string[] warnings = context.Group.GetIntegrationWarnings();
            if (warnings == null)
                return;
            for (int i = 0; i < warnings.Length; i++)
            {
                if (string.IsNullOrEmpty(warnings[i]))
                    continue;
                results.Add(new TaffyDiagnosticResult(
                    Id + "." + i,
                    "Integration ownership",
                    warnings[i],
                    TaffyDiagnosticSeverity.Info,
                    context.Group));
            }
        }
    }

    internal sealed class IntrinsicMeasurementRule : TaffyDiagnosticRule
    {
        internal override string Id => "item.measurement-source";

        internal override void Evaluate(TaffyDiagnosticContext context, List<TaffyDiagnosticResult> results)
        {
            TaffyLayoutItem item = context.Item;
            if (!item || item.measurement == TaffyMeasurementMode.Disabled)
                return;
            if (item.width.unit != TaffyUnit.Auto && item.height.unit != TaffyUnit.Auto)
                return;
            if (HasIntrinsicSource(item.gameObject))
                return;

            results.Add(new TaffyDiagnosticResult(
                Id,
                "No intrinsic measurement source detected",
                "This item uses content-dependent sizing, but no Text, Image, RawImage, TMP text, or ITaffyMeasurementProvider is present. Auto size may therefore resolve only from layout constraints.",
                TaffyDiagnosticSeverity.Info,
                item));
        }

        private static bool HasIntrinsicSource(GameObject gameObject)
        {
            if (!gameObject)
                return false;
            if (gameObject.GetComponent<Text>() || gameObject.GetComponent<Image>() || gameObject.GetComponent<RawImage>())
                return true;

            Component[] components = gameObject.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (!component)
                    continue;
                Type type = component.GetType();
                if (type.FullName != null && type.FullName.StartsWith("TMPro.TMP_Text", StringComparison.Ordinal))
                    return true;
                Type[] interfaces = type.GetInterfaces();
                for (int j = 0; j < interfaces.Length; j++)
                {
                    if (interfaces[j].FullName == "TaffyUGUI.ITaffyMeasurementProvider")
                        return true;
                }
            }
            return false;
        }
    }

    internal sealed class ResponsiveProfilesRule : TaffyDiagnosticRule
    {
        internal override string Id => "group.responsive-profiles";

        internal override void Evaluate(TaffyDiagnosticContext context, List<TaffyDiagnosticResult> results)
        {
            if (!context.Group)
                return;
            if (context.Group.ValidateResponsiveProfiles(out string error))
                return;
            results.Add(new TaffyDiagnosticResult(
                Id,
                "Responsive profile validation failed",
                error,
                TaffyDiagnosticSeverity.Error,
                context.Group));
        }
    }

    internal sealed class GridValidationRule : TaffyDiagnosticRule
    {
        internal override string Id => "grid.validation";

        internal override void Evaluate(TaffyDiagnosticContext context, List<TaffyDiagnosticResult> results)
        {
            if (context.Group && context.Group.containerDisplay == TaffyContainerDisplay.Grid)
            {
                if (!TaffyRuntimeValidationBridge.TryValidateGrid(context.Group, out string error))
                {
                    results.Add(new TaffyDiagnosticResult(Id, "Grid validation failed", error, TaffyDiagnosticSeverity.Error, context.Group));
                }
                return;
            }

            if (context.Item && context.ParentGroup && context.ParentGroup.containerDisplay == TaffyContainerDisplay.Grid &&
                !TaffyRuntimeValidationBridge.TryValidateGridPlacement(context.Item, out string placementError))
            {
                results.Add(new TaffyDiagnosticResult(
                    Id + ".placement",
                    "Grid placement is invalid",
                    placementError,
                    TaffyDiagnosticSeverity.Error,
                    context.Item,
                    null,
                    TaffyDiagnosticFixUtility.ResetGridPlacement(context.Item)));
            }
        }
    }

    internal sealed class CalcValidationRule : TaffyDiagnosticRule
    {
        internal override string Id => "calc.validation";

        internal override void Evaluate(TaffyDiagnosticContext context, List<TaffyDiagnosticResult> results)
        {
            if (!context.Item)
                return;
            if (TaffyRuntimeValidationBridge.TryValidateCalc(context.Item, out string error))
                return;
            results.Add(new TaffyDiagnosticResult(
                Id,
                "Calc validation failed",
                error,
                TaffyDiagnosticSeverity.Error,
                context.Item));
        }
    }

    internal sealed class FixedSizeResponsiveRule : TaffyDiagnosticRule
    {
        internal override string Id => "item.fixed-responsive";

        internal override void Evaluate(TaffyDiagnosticContext context, List<TaffyDiagnosticResult> results)
        {
            TaffyLayoutItem item = context.Item;
            TaffyLayoutGroup parent = context.ParentGroup;
            if (!item || !parent || parent.responsiveProfiles == null)
                return;

            for (int i = 0; i < parent.responsiveProfiles.Count; i++)
            {
                TaffyResponsiveProfile profile = parent.responsiveProfiles[i];
                if (profile == null)
                    continue;
                if (item.width.unit == TaffyUnit.Points && profile.maxWidth > 0f && item.width.value > profile.maxWidth)
                {
                    results.Add(new TaffyDiagnosticResult(
                        Id + ".width." + i,
                        "Fixed width exceeds a responsive breakpoint",
                        item.name + " is fixed at " + item.width.value.ToString("0.##") + " while profile '" + profile.name + "' allows at most " + profile.maxWidth.ToString("0.##") + " width. Verify that overflow is intentional.",
                        TaffyDiagnosticSeverity.Info,
                        item));
                }
            }
        }
    }

    internal sealed class RebuildSuppressionRule : TaffyDiagnosticRule
    {
        internal override string Id => "group.rebuild-suppression";

        internal override void Evaluate(TaffyDiagnosticContext context, List<TaffyDiagnosticResult> results)
        {
            if (!context.Group || context.Group.SuppressedRebuildRequestCount <= 0)
                return;
            int count = context.Group.SuppressedRebuildRequestCount;
            int budget = Mathf.Max(1, context.Group.maxRebuildRequestsPerFrame);
            TaffyDiagnosticSeverity severity = count >= budget * 2 ? TaffyDiagnosticSeverity.Warning : TaffyDiagnosticSeverity.Info;
            results.Add(new TaffyDiagnosticResult(
                Id,
                "Rebuild requests were suppressed",
                count + " rebuild request(s) have been suppressed by the configured per-frame budget. Repeated growth can indicate competing layout ownership or a rebuild loop.",
                severity,
                context.Group,
                null,
                new TaffyDiagnosticFix("rebuild.reset", "Reset Counter", () =>
                    TaffyDiagnosticFixUtility.Record(context.Group, "Reset Taffy rebuild diagnostics", context.Group.ResetRebuildDiagnostics))));
        }
    }

    internal static class TaffyRuntimeValidationBridge
    {
        private const BindingFlags InstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticNonPublic = BindingFlags.Static | BindingFlags.NonPublic;

        internal static bool TryValidateCalc(TaffyLayoutItem item, out string error)
        {
            error = null;
            if (!item)
                return true;
            MethodInfo method = typeof(TaffyLayoutItem).GetMethod("TryValidateCalc", InstanceNonPublic);
            return InvokeOutString(item, method, out error);
        }

        internal static bool TryValidateGridPlacement(TaffyLayoutItem item, out string error)
        {
            error = null;
            if (!item)
                return true;
            MethodInfo method = typeof(TaffyLayoutItem).GetMethod("TryValidateGridPlacement", InstanceNonPublic);
            if (method == null)
                return true;
            object[] args = { item.name, null };
            bool valid = (bool)method.Invoke(item, args);
            error = args[1] as string;
            return valid;
        }

        internal static bool TryValidateGrid(TaffyLayoutGroup group, out string error)
        {
            error = null;
            if (!group)
                return true;
            Type compiler = typeof(TaffyLayoutGroup).Assembly.GetType("TaffyUGUI.TaffyGridCompiler");
            MethodInfo method = compiler?.GetMethod("TryValidate", StaticNonPublic | BindingFlags.Public);
            if (method == null)
                return true;
            object[] args = { group, null };
            bool valid = (bool)method.Invoke(null, args);
            error = args[1] as string;
            return valid;
        }

        private static bool InvokeOutString(object target, MethodInfo method, out string error)
        {
            error = null;
            if (method == null)
                return true;
            object[] args = { null };
            bool valid = (bool)method.Invoke(target, args);
            error = args[0] as string;
            return valid;
        }
    }

    internal static class TaffyLayoutHealthGUI
    {
        private const string FoldoutKey = "TaffyUGUI.Editor.LayoutHealth.Expanded";

        internal static void Draw(TaffyInspectorContext context)
        {
            if (context == null)
                return;
            TaffyLayoutHealth health = TaffyLayoutHealth.Evaluate(context.Targets);
            if (health.IsHealthy)
            {
                EditorGUILayout.HelpBox("Layout Health: Healthy", MessageType.Info);
                return;
            }

            string state = health.HighestSeverity == TaffyDiagnosticSeverity.Error
                ? "Error"
                : health.HighestSeverity == TaffyDiagnosticSeverity.Warning ? "Warning" : "Info";
            MessageType messageType = health.HighestSeverity == TaffyDiagnosticSeverity.Error
                ? MessageType.Error
                : health.HighestSeverity == TaffyDiagnosticSeverity.Warning ? MessageType.Warning : MessageType.Info;
            EditorGUILayout.HelpBox("Layout Health: " + state + " — " + health.Results.Count + " issue(s)", messageType);

            bool expanded = SessionState.GetBool(FoldoutKey, true);
            expanded = EditorGUILayout.Foldout(expanded, "Layout Health Details", true);
            SessionState.SetBool(FoldoutKey, expanded);
            if (!expanded)
                return;

            for (int i = 0; i < health.Results.Count; i++)
                DrawResult(health.Results[i]);
        }

        private static void DrawResult(TaffyDiagnosticResult result)
        {
            MessageType type = result.Severity == TaffyDiagnosticSeverity.Error
                ? MessageType.Error
                : result.Severity == TaffyDiagnosticSeverity.Warning ? MessageType.Warning : MessageType.Info;
            string targetName = result.Target ? " [" + result.Target.name + "]" : string.Empty;
            EditorGUILayout.HelpBox(result.Title + targetName + "\n" + result.Message, type);

            if (result.Fixes.Count > 0 || !string.IsNullOrEmpty(result.DocumentationUrl))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int i = 0; i < result.Fixes.Count; i++)
                    {
                        TaffyDiagnosticFix fix = result.Fixes[i];
                        if (GUILayout.Button(fix.Label))
                            fix.Invoke();
                    }
                    if (!string.IsNullOrEmpty(result.DocumentationUrl) && GUILayout.Button("Help", GUILayout.Width(56f)))
                        Application.OpenURL(result.DocumentationUrl);
                }
            }
        }
    }
}
