using System;
using System.Runtime.InteropServices;
using Object = UnityEngine.Object;
using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyWebRuntimeRegressionTests
    {
        public const string AbiHandshakePassMarker =
            "TAFFY_WEB_RUNTIME_ABI_PASS abi=1 stage>=2 taffy=0.13.0 capabilities=required";

        [UnityTest]
        public IEnumerator AbiVersionStageAndCapabilitiesHandshakePassesThroughNormalLayoutApis()
        {
            var rootObject = new GameObject("WebRuntimeAbiHandshake", typeof(RectTransform), typeof(TaffyLayoutGroup));
            RectTransform root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.sizeDelta = new Vector2(160f, 80f);

            TaffyLayoutGroup group = rootObject.GetComponent<TaffyLayoutGroup>();
            group.direction = TaffyFlexDirection.Row;

            var childObject = new GameObject("HandshakeItem", typeof(RectTransform), typeof(TaffyLayoutItem));
            childObject.transform.SetParent(root, false);
            TaffyLayoutItem item = childObject.GetComponent<TaffyLayoutItem>();
            item.width = TaffyLength.Points(20f);
            item.height = TaffyLength.Points(10f);

            yield return null;

            Assert.DoesNotThrow(
                () => LayoutRebuilder.ForceRebuildLayoutImmediate(root),
                "Normal TaffyLayoutGroup initialization must complete the native ABI/version/capability handshake before creating its context.");

            Debug.Log(AbiHandshakePassMarker);

            Object.Destroy(rootObject);
            yield return null;

        }

        public const string FlexPassMarker =
            "TAFFY_WEB_RUNTIME_FLEX_PASS row=2 column=2";

        [UnityTest]
        public IEnumerator FlexRowAndColumnLayoutsProduceDeterministicGeometry()
        {
            RectTransform row = CreateRoot("WebRuntimeFlexRow", new Vector2(200f, 100f), TaffyFlexDirection.Row);
            RectTransform rowA = CreateItem(row, "RowA", 40f, 20f);
            RectTransform rowB = CreateItem(row, "RowB", 60f, 30f);

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(row);

            Assert.That(Left(rowA), Is.EqualTo(0f).Within(0.01f));
            Assert.That(Left(rowB), Is.EqualTo(40f).Within(0.01f));
            Assert.That(Top(rowA), Is.EqualTo(0f).Within(0.01f));
            Assert.That(Top(rowB), Is.EqualTo(0f).Within(0.01f));
            Assert.That(rowA.rect.width, Is.EqualTo(40f).Within(0.01f));
            Assert.That(rowA.rect.height, Is.EqualTo(20f).Within(0.01f));
            Assert.That(rowB.rect.width, Is.EqualTo(60f).Within(0.01f));
            Assert.That(rowB.rect.height, Is.EqualTo(30f).Within(0.01f));


            Object.Destroy(row.gameObject);
            yield return null;

            RectTransform column = CreateRoot("WebRuntimeFlexColumn", new Vector2(100f, 200f), TaffyFlexDirection.Column);
            RectTransform columnA = CreateItem(column, "ColumnA", 40f, 20f);
            RectTransform columnB = CreateItem(column, "ColumnB", 60f, 30f);

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(column);

            Assert.That(Left(columnA), Is.EqualTo(0f).Within(0.01f));
            Assert.That(Left(columnB), Is.EqualTo(0f).Within(0.01f));
            Assert.That(Top(columnA), Is.EqualTo(0f).Within(0.01f));
            Assert.That(Top(columnB), Is.EqualTo(20f).Within(0.01f));
            Assert.That(columnA.rect.width, Is.EqualTo(40f).Within(0.01f));
            Assert.That(columnA.rect.height, Is.EqualTo(20f).Within(0.01f));
            Assert.That(columnB.rect.width, Is.EqualTo(60f).Within(0.01f));
            Assert.That(columnB.rect.height, Is.EqualTo(30f).Within(0.01f));


            Debug.Log(FlexPassMarker);

            Object.Destroy(column.gameObject);
            yield return null;
        }

        public const string GridPassMarker =
            "TAFFY_WEB_RUNTIME_GRID_PASS rows=1 columns=2 horizontalGap=10 items=1";

        [UnityTest]
        public IEnumerator GridTracksPlacementGapsAndDetailedDiagnosticsAreDeterministic()
        {
            RectTransform root = CreateRoot("WebRuntimeGrid", new Vector2(200f, 100f), TaffyFlexDirection.Row);
            TaffyLayoutGroup group = root.GetComponent<TaffyLayoutGroup>();
            group.containerDisplay = TaffyContainerDisplay.Grid;
            group.horizontalGap = 10f;
            group.gridRows.Add(TaffyGridTrack.Points(100f));
            group.gridColumns.Add(TaffyGridTrack.Points(80f));
            group.gridColumns.Add(TaffyGridTrack.Points(110f));

            RectTransform child = CreateItem(root, "GridPlacedItem", 20f, 20f);
            TaffyLayoutItem item = child.GetComponent<TaffyLayoutItem>();
            item.gridRowStart = TaffyGridPlacement.Line(1);
            item.gridRowEnd = TaffyGridPlacement.Line(2);
            item.gridColumnStart = TaffyGridPlacement.Line(2);
            item.gridColumnEnd = TaffyGridPlacement.Line(3);

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);

            Assert.That(Left(child), Is.EqualTo(90f).Within(0.01f));
            Assert.That(Top(child), Is.EqualTo(0f).Within(0.01f));
            Assert.That(group.TryGetGridDiagnostics(out TaffyGridDiagnostics diagnostics, out string error), Is.True, error);
            Assert.That(diagnostics.explicitRows, Is.EqualTo(1u));
            Assert.That(diagnostics.explicitColumns, Is.EqualTo(2u));
            Assert.That(diagnostics.rowTrackSizes, Has.Length.EqualTo(1));
            Assert.That(diagnostics.columnTrackSizes, Has.Length.EqualTo(2));
            Assert.That(diagnostics.rowTrackSizes[0], Is.EqualTo(100f).Within(0.01f));
            Assert.That(diagnostics.columnTrackSizes[0], Is.EqualTo(80f).Within(0.01f));
            Assert.That(diagnostics.columnTrackSizes[1], Is.EqualTo(110f).Within(0.01f));
            Assert.That(diagnostics.columnGutters, Has.Length.EqualTo(3));
            Assert.That(diagnostics.columnGutters[0], Is.EqualTo(0f).Within(0.01f));
            Assert.That(diagnostics.columnGutters[1], Is.EqualTo(10f).Within(0.01f));
            Assert.That(diagnostics.columnGutters[2], Is.EqualTo(0f).Within(0.01f));
            Assert.That(diagnostics.items, Has.Length.EqualTo(1));

            Debug.Log(GridPassMarker);

            Object.Destroy(root.gameObject);
            yield return null;
        }

        public const string BlockPassMarker =
            "TAFFY_WEB_RUNTIME_BLOCK_PASS block=stack flowRoot=float-clear";

        [UnityTest]
        public IEnumerator BlockAndFlowRootBehaviorsRemainDeterministic()
        {
            RectTransform blockRoot = CreateRoot("WebRuntimeBlock", new Vector2(220f, 140f), TaffyFlexDirection.Row);
            TaffyLayoutGroup blockGroup = blockRoot.GetComponent<TaffyLayoutGroup>();
            blockGroup.containerDisplay = TaffyContainerDisplay.Block;

            RectTransform blockA = CreateItem(blockRoot, "BlockA", 80f, 20f);
            RectTransform blockB = CreateItem(blockRoot, "BlockB", 100f, 30f);
            blockA.GetComponent<TaffyLayoutItem>().display = TaffyDisplay.Block;
            blockB.GetComponent<TaffyLayoutItem>().display = TaffyDisplay.Block;

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(blockRoot);

            Assert.That(Top(blockA), Is.EqualTo(0f).Within(0.01f));
            Assert.That(Top(blockB), Is.GreaterThanOrEqualTo(20f - 0.01f));
            Assert.That(blockA.rect.width, Is.EqualTo(80f).Within(0.01f));
            Assert.That(blockB.rect.width, Is.EqualTo(100f).Within(0.01f));

            Object.Destroy(blockRoot.gameObject);
            yield return null;

            RectTransform flowRoot = CreateRoot("WebRuntimeFlowRoot", new Vector2(220f, 140f), TaffyFlexDirection.Row);
            TaffyLayoutGroup flowGroup = flowRoot.GetComponent<TaffyLayoutGroup>();
            flowGroup.containerDisplay = TaffyContainerDisplay.FlowRoot;

            RectTransform floating = CreateItem(flowRoot, "Float", 60f, 30f);
            TaffyLayoutItem floatItem = floating.GetComponent<TaffyLayoutItem>();
            floatItem.display = TaffyDisplay.Block;
            floatItem.floatMode = TaffyFloat.Left;

            RectTransform cleared = CreateItem(flowRoot, "Clear", 100f, 20f);
            TaffyLayoutItem clearItem = cleared.GetComponent<TaffyLayoutItem>();
            clearItem.display = TaffyDisplay.Block;
            clearItem.clearMode = TaffyClear.Both;

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(flowRoot);

            Assert.That(floating.rect.width, Is.EqualTo(60f).Within(0.01f));
            Assert.That(Top(cleared), Is.GreaterThanOrEqualTo(Top(floating) + floating.rect.height - 0.01f));

            Debug.Log(BlockPassMarker);

            Object.Destroy(flowRoot.gameObject);
            yield return null;
        }

        public const string CalcPassMarker =
            "TAFFY_WEB_RUNTIME_CALC_PASS item=90 track=120";

        [UnityTest]
        public IEnumerator CalcValuesRecomputeAcrossItemAndGridMutation()
        {
            RectTransform root = CreateRoot("WebRuntimeCalc", new Vector2(220f, 100f), TaffyFlexDirection.Row);
            TaffyLayoutGroup group = root.GetComponent<TaffyLayoutGroup>();
            group.containerDisplay = TaffyContainerDisplay.Grid;
            group.gridRows.Add(TaffyGridTrack.Points(100f));
            group.gridColumns.Add(TaffyGridTrack.Calc(TaffyCalcExpression.Length(100f)));
            group.gridColumns.Add(TaffyGridTrack.Fraction(1f));

            RectTransform child = CreateItem(root, "CalcItem", 70f, 20f);
            TaffyLayoutItem item = child.GetComponent<TaffyLayoutItem>();
            item.width = TaffyLength.Calc(TaffyCalcExpression.Length(70f));

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Assert.That(child.rect.width, Is.EqualTo(70f).Within(0.01f));

            item.width = TaffyLength.Calc(TaffyCalcExpression.Length(90f));
            group.gridColumns[0] = TaffyGridTrack.Calc(TaffyCalcExpression.Length(120f));
            group.SetLayoutDirty();

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);

            Assert.That(child.rect.width, Is.EqualTo(90f).Within(0.01f));
            Assert.That(group.TryGetGridDiagnostics(out TaffyGridDiagnostics diagnostics, out string error), Is.True, error);
            Assert.That(diagnostics.columnTrackSizes[0], Is.EqualTo(120f).Within(0.01f));

            Debug.Log(CalcPassMarker);

            Object.Destroy(root.gameObject);
            yield return null;
        }

        public const string ResponsivePassMarker =
            "TAFFY_WEB_RUNTIME_RESPONSIVE_PASS automatic=narrow/wide forced=wide clear=narrow";

        [UnityTest]
        public IEnumerator ResponsiveProfilesResolveAutomaticallyAndCanBeForcedAtRuntime()
        {
            RectTransform root = CreateRoot("WebRuntimeResponsive", new Vector2(200f, 100f), TaffyFlexDirection.Row);
            TaffyLayoutGroup group = root.GetComponent<TaffyLayoutGroup>();
            group.responsiveProfiles.Add(new TaffyResponsiveProfile
            {
                name = "narrow",
                priority = 5,
                maxWidth = 250f,
                overrideFlexDirection = true,
                direction = TaffyFlexDirection.Column,
            });
            group.responsiveProfiles.Add(new TaffyResponsiveProfile
            {
                name = "wide",
                priority = 5,
                minWidth = 251f,
                overrideFlexDirection = true,
                direction = TaffyFlexDirection.Row,
            });

            RectTransform first = CreateItem(root, "ResponsiveFirst", 40f, 20f);
            RectTransform second = CreateItem(root, "ResponsiveSecond", 60f, 30f);

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);

            Assert.That(group.ActiveResponsiveProfileName, Is.EqualTo("narrow"));
            Assert.That(group.RuntimeResponsiveProfileOverride, Is.Null.Or.Empty);
            Assert.That(Top(second), Is.EqualTo(20f).Within(0.01f));

            Assert.That(group.SetRuntimeResponsiveProfile("wide", out string error), Is.True, error);
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);

            Assert.That(group.RuntimeResponsiveProfileOverride, Is.EqualTo("wide"));
            Assert.That(group.ActiveResponsiveProfileName, Is.EqualTo("wide"));
            Assert.That(Top(second), Is.EqualTo(Top(first)).Within(0.01f));
            Assert.That(Left(second), Is.EqualTo(40f).Within(0.01f));

            group.ClearRuntimeResponsiveProfile();
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);

            Assert.That(group.RuntimeResponsiveProfileOverride, Is.Null.Or.Empty);
            Assert.That(group.ActiveResponsiveProfileName, Is.EqualTo("narrow"));
            Assert.That(Top(second), Is.EqualTo(20f).Within(0.01f));

            root.sizeDelta = new Vector2(400f, 100f);
            group.SetLayoutDirty();
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);

            Assert.That(group.ActiveResponsiveProfileName, Is.EqualTo("wide"));
            Assert.That(Top(second), Is.EqualTo(Top(first)).Within(0.01f));
            Assert.That(Left(second), Is.EqualTo(40f).Within(0.01f));

            Debug.Log(ResponsivePassMarker);

            Object.Destroy(root.gameObject);
            yield return null;
        }

        public const string TmpPassMarker =
            "TAFFY_WEB_RUNTIME_TMP_PASS intrinsic=1 wrapped=1";

        [UnityTest]
        public IEnumerator TextMeshProIntrinsicMeasurementAndWidthConstrainedWrappingAreDeterministic()
        {
            RectTransform root = CreateRoot("WebRuntimeTmp", new Vector2(800f, 220f), TaffyFlexDirection.Row);
            TaffyLayoutGroup group = root.GetComponent<TaffyLayoutGroup>();
            group.alignItems = TaffyAlign.Start;

            TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/Inter-Regular SDF");
            Assert.That(fontAsset, Is.Not.Null, "WEB3 TMP regression requires the maintained test-host TMP font resource.");

            var textObject = new GameObject("MeasuredTmp", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(TaffyLayoutItem));
            RectTransform child = textObject.GetComponent<RectTransform>();
            child.SetParent(root, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            TaffyLayoutItem item = textObject.GetComponent<TaffyLayoutItem>();
            text.font = fontAsset;
            text.fontSize = 22f;
            text.text = "Responsive layout text wraps across several short words";
#if UNITY_6000_0_OR_NEWER
            text.textWrappingMode = TextWrappingModes.Normal;
#else
            text.enableWordWrapping = true;
#endif

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);

            Vector2 unboundedPreferred = text.GetPreferredValues(text.text, 100000f, 100000f);
            Assert.That(unboundedPreferred.x, Is.GreaterThan(160f));
            Assert.That(child.rect.width, Is.EqualTo(unboundedPreferred.x).Within(1f));
            float intrinsicHeight = child.rect.height;
            Assert.That(intrinsicHeight, Is.GreaterThanOrEqualTo(unboundedPreferred.y - 1f));

            // 128 px is a permanent measurement sample width, so the native bridge should
            // reproduce TMP's width-constrained preferred height without nearest-sample approximation.
            const float constrainedWidth = 128f;
            Vector2 wrappedPreferred = text.GetPreferredValues(text.text, constrainedWidth, 100000f);
            Assert.That(wrappedPreferred.y, Is.GreaterThan(intrinsicHeight + 1f),
                "The chosen TMP fixture must produce additional wrapped lines at the sampled width.");

            item.width = TaffyLength.Points(constrainedWidth);
            item.height = TaffyLength.Auto;
            group.SetLayoutDirty();
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);

            Assert.That(child.rect.width, Is.EqualTo(constrainedWidth).Within(0.05f));
            Assert.That(child.rect.height, Is.EqualTo(wrappedPreferred.y).Within(1f));
            Assert.That(child.rect.height, Is.GreaterThan(unboundedPreferred.y + 1f));

            Debug.Log(TmpPassMarker);

            Object.Destroy(root.gameObject);
            yield return null;
        }

        public const string UguiMeasurementPassMarker =
            "TAFFY_WEB_RUNTIME_UGUI_MEASURE_PASS text=1 image=64x32 raw=40x20";

        [UnityTest]
        public IEnumerator UguiTextImageAndRawImageMeasurementsRemainDeterministic()
        {
            RectTransform root = CreateRoot("WebRuntimeUguiMeasurement", new Vector2(800f, 180f), TaffyFlexDirection.Row);
            TaffyLayoutGroup group = root.GetComponent<TaffyLayoutGroup>();
            group.alignItems = TaffyAlign.Start;

            var textObject = new GameObject("MeasuredText", typeof(RectTransform), typeof(Text));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(root, false);
            Text text = textObject.GetComponent<Text>();
#if UNITY_2022_1_OR_NEWER
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            Assert.That(font, Is.Not.Null, "uGUI Text measurement requires an available built-in font resource.");
            text.font = font;
            text.fontSize = 20;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = "Web text";

            Texture2D imageTexture = new Texture2D(64, 32);
            Sprite sprite = Sprite.Create(imageTexture, new Rect(0f, 0f, 64f, 32f), new Vector2(0.5f, 0.5f), 100f);
            var imageObject = new GameObject("MeasuredImage", typeof(RectTransform), typeof(Image));
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.SetParent(root, false);
            imageObject.GetComponent<Image>().sprite = sprite;

            Texture2D rawTexture = new Texture2D(80, 40);
            var rawObject = new GameObject("MeasuredRawImage", typeof(RectTransform), typeof(RawImage));
            RectTransform rawRect = rawObject.GetComponent<RectTransform>();
            rawRect.SetParent(root, false);
            RawImage rawImage = rawObject.GetComponent<RawImage>();
            rawImage.texture = rawTexture;
            rawImage.uvRect = new Rect(0f, 0f, 0.5f, 0.5f);

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);

            float shortTextWidth = textRect.rect.width;
            Assert.That(shortTextWidth, Is.GreaterThan(0f));
            Assert.That(imageRect.rect.width, Is.EqualTo(64f).Within(0.1f));
            Assert.That(imageRect.rect.height, Is.EqualTo(32f).Within(0.1f));
            Assert.That(rawRect.rect.width, Is.EqualTo(40f).Within(0.1f));
            Assert.That(rawRect.rect.height, Is.EqualTo(20f).Within(0.1f));

            text.text = "Web text measurement grows after runtime content changes";
            group.SetLayoutDirty();
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);

            Assert.That(textRect.rect.width, Is.GreaterThan(shortTextWidth + 20f));
            Assert.That(imageRect.rect.width, Is.EqualTo(64f).Within(0.1f));
            Assert.That(rawRect.rect.width, Is.EqualTo(40f).Within(0.1f));

            Debug.Log(UguiMeasurementPassMarker);

            Object.Destroy(root.gameObject);
            Object.Destroy(sprite);
            Object.Destroy(imageTexture);
            Object.Destroy(rawTexture);
            yield return null;
        }

        public const string NestedScrollPassMarker =
            "TAFFY_WEB_RUNTIME_NESTED_SCROLL_PASS nested=90x35 scroll=140";

        [UnityTest]
        public IEnumerator NestedGroupsAndScrollRectIntegrationRemainDeterministic()
        {
            RectTransform root = CreateRoot("WebRuntimeNestedRoot", new Vector2(300f, 160f), TaffyFlexDirection.Row);
            TaffyLayoutGroup rootGroup = root.GetComponent<TaffyLayoutGroup>();
            rootGroup.alignItems = TaffyAlign.Start;

            var nestedObject = new GameObject("NestedGroup", typeof(RectTransform), typeof(TaffyLayoutGroup));
            RectTransform nested = nestedObject.GetComponent<RectTransform>();
            nested.SetParent(root, false);
            TaffyLayoutGroup nestedGroup = nestedObject.GetComponent<TaffyLayoutGroup>();
            nestedGroup.direction = TaffyFlexDirection.Column;
            nestedGroup.alignItems = TaffyAlign.Start;
            RectTransform nestedChild = CreateItem(nested, "NestedChild", 90f, 35f);

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);

            Assert.That(nested.rect.width, Is.EqualTo(90f).Within(0.1f));
            Assert.That(nested.rect.height, Is.EqualTo(35f).Within(0.1f));
            Assert.That(nestedChild.rect.width, Is.EqualTo(90f).Within(0.1f));
            Assert.That(nestedChild.rect.height, Is.EqualTo(35f).Within(0.1f));

            Object.Destroy(root.gameObject);
            yield return null;

            var scrollObject = new GameObject("WebRuntimeScroll", typeof(RectTransform), typeof(ScrollRect));
            RectTransform viewport = scrollObject.GetComponent<RectTransform>();
            viewport.anchorMin = new Vector2(0f, 1f);
            viewport.anchorMax = new Vector2(0f, 1f);
            viewport.pivot = new Vector2(0f, 1f);
            viewport.sizeDelta = new Vector2(120f, 100f);

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.viewport = viewport;

            var contentObject = new GameObject("ScrollContent", typeof(RectTransform), typeof(TaffyLayoutGroup));
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.sizeDelta = new Vector2(120f, 100f);
            scroll.content = content;

            TaffyLayoutGroup contentGroup = contentObject.GetComponent<TaffyLayoutGroup>();
            contentGroup.direction = TaffyFlexDirection.Column;
            contentGroup.alignItems = TaffyAlign.Start;
            CreateItem(content, "ScrollOne", 100f, 70f);

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            Assert.That(content.rect.height, Is.EqualTo(100f).Within(0.1f));

            CreateItem(content, "ScrollTwo", 100f, 70f);
            contentGroup.SetLayoutDirty();
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            Assert.That(content.rect.height, Is.EqualTo(140f).Within(0.1f));

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            Assert.That(content.rect.height, Is.EqualTo(140f).Within(0.1f));

            Debug.Log(NestedScrollPassMarker);

            Object.Destroy(scrollObject);
            yield return null;
        }

        public const string BulkAbiPassMarker =
            "TAFFY_WEB_RUNTIME_BULK_ABI_PASS styles=2 topology=1 measurement=1 layouts=3";

        [UnityTest]
        public IEnumerator BulkStyleTopologyMeasurementAndLayoutAbiCallsRoundTrip()
        {
            AssertNativeOk(WebBulkNative.tu_context_create(out ulong context), "create bulk ABI context");
            try
            {
                WebBulkNative.Style rootStyle = WebBulkNative.Style.DefaultFlex();
                rootStyle.alignItems = 0;
                WebBulkNative.Style firstStyle = WebBulkNative.Style.DefaultFlex();
                WebBulkNative.Style secondStyle = WebBulkNative.Style.DefaultFlex();

                AssertNativeOk(WebBulkNative.tu_node_create(context, ref rootStyle, out ulong root), "create bulk ABI root");
                AssertNativeOk(WebBulkNative.tu_node_create(context, ref firstStyle, out ulong first), "create first bulk ABI child");
                AssertNativeOk(WebBulkNative.tu_node_create(context, ref secondStyle, out ulong second), "create second bulk ABI child");

                firstStyle.width = WebBulkNative.Value.Points(40f);
                firstStyle.height = WebBulkNative.Value.Points(20f);
                var styleUpdates = new[]
                {
                    new WebBulkNative.StyleUpdate { node = first, style = firstStyle },
                    new WebBulkNative.StyleUpdate { node = second, style = secondStyle },
                };
                AssertNativeOk(
                    WebBulkNative.tu_nodes_set_styles(context, styleUpdates, (uint)styleUpdates.Length),
                    "bulk style upload");

                ulong[] children = { first, second };
                GCHandle childrenPin = GCHandle.Alloc(children, GCHandleType.Pinned);
                try
                {
                    var topologyUpdates = new[]
                    {
                        new WebBulkNative.ChildrenUpdate
                        {
                            parent = root,
                            children = childrenPin.AddrOfPinnedObject(),
                            childCount = (uint)children.Length,
                        },
                    };
                    AssertNativeOk(
                        WebBulkNative.tu_nodes_set_children(context, topologyUpdates, (uint)topologyUpdates.Length),
                        "bulk topology upload");
                }
                finally
                {
                    childrenPin.Free();
                }

                var measurementUpdates = new[]
                {
                    new WebBulkNative.MeasurementUpdate
                    {
                        node = second,
                        hasMeasurement = 1,
                        measurement = new WebBulkNative.Measurement
                        {
                            minWidth = 30f,
                            minHeight = 12f,
                            maxWidth = 30f,
                            maxHeight = 12f,
                            preferredWidth = 30f,
                            preferredHeight = 12f,
                        },
                    },
                };
                AssertNativeOk(
                    WebBulkNative.tu_nodes_set_measurements(context, measurementUpdates, (uint)measurementUpdates.Length),
                    "bulk measurement upload");

                AssertNativeOk(WebBulkNative.tu_compute_layout(context, root, 200f, 100f), "compute bulk ABI layout");

                ulong[] handles = { root, first, second };
                var layouts = new WebBulkNative.Layout[handles.Length];
                AssertNativeOk(
                    WebBulkNative.tu_get_layouts_bulk(
                        context,
                        handles,
                        (uint)handles.Length,
                        layouts,
                        (uint)layouts.Length,
                        out uint written),
                    "bulk layout retrieval");

                Assert.That(written, Is.EqualTo(3u));
                Assert.That(layouts[0].node, Is.EqualTo(root));
                Assert.That(layouts[1].node, Is.EqualTo(first));
                Assert.That(layouts[2].node, Is.EqualTo(second));
                Assert.That(layouts[1].x, Is.EqualTo(0f).Within(0.01f));
                Assert.That(layouts[1].width, Is.EqualTo(40f).Within(0.01f));
                Assert.That(layouts[1].height, Is.EqualTo(20f).Within(0.01f));
                Assert.That(layouts[2].x, Is.EqualTo(40f).Within(0.01f));
                Assert.That(layouts[2].width, Is.EqualTo(30f).Within(0.01f));
                Assert.That(layouts[2].height, Is.EqualTo(12f).Within(0.01f));

                Debug.Log(BulkAbiPassMarker);
            }
            finally
            {
                AssertNativeOk(WebBulkNative.tu_context_destroy(context), "destroy bulk ABI context");
            }

            yield return null;
        }

        public const string LifecyclePassMarker =
            "TAFFY_WEB_RUNTIME_LIFECYCLE_PASS cycles=32 context=1 node=1 resource=1";

        [UnityTest]
        public IEnumerator RepeatedContextNodeAndResourceCreateDestroyCyclesRemainStable()
        {
            const int cycles = 32;
            for (int cycle = 0; cycle < cycles; cycle++)
            {
                AssertNativeOk(WebBulkNative.tu_context_create(out ulong context), $"create lifecycle context {cycle}");
                try
                {
                    WebBulkNative.Style style = WebBulkNative.Style.DefaultFlex();
                    AssertNativeOk(WebBulkNative.tu_node_create(context, ref style, out ulong node), $"create lifecycle node {cycle}");

                    var calc = new WebBulkNative.CalcSpec
                    {
                        op = 0,
                        value = 10f + cycle,
                    };
                    AssertNativeOk(WebBulkNative.tu_calc_create(context, ref calc, out ulong resource), $"create lifecycle resource {cycle}");

                    AssertNativeOk(WebBulkNative.tu_node_remove(context, node), $"remove lifecycle node {cycle}");
                    AssertNativeOk(WebBulkNative.tu_calc_remove(context, resource), $"remove lifecycle resource {cycle}");
                }
                finally
                {
                    AssertNativeOk(WebBulkNative.tu_context_destroy(context), $"destroy lifecycle context {cycle}");
                }
            }

            Debug.Log(LifecyclePassMarker);
            yield return null;
        }

        private static void AssertNativeOk(int status, string operation)
        {
            Assert.That(status, Is.EqualTo(0), $"Taffy native operation failed: {operation}");
        }

        private static class WebBulkNative
        {
#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
            private const string Library = "__Internal";
#else
            private const string Library = "taffy_ugui";
#endif

            [StructLayout(LayoutKind.Sequential)]
            internal struct Value
            {
                public int kind;
                public float value;
                public ulong resource;

                internal static Value Auto => default;
                internal static Value Points(float points) => new Value { kind = 1, value = points };
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct StringView
            {
                public IntPtr data;
                public uint len;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct GridPlacement
            {
                public int kind;
                public int line;
                public uint span;
                public int occurrence;
                public StringView name;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct Style
            {
                public int display;
                public int boxSizing;
                public int direction;
                public int overflowX;
                public int overflowY;
                public float scrollbarWidth;
                public int position;
                public Value insetLeft, insetRight, insetTop, insetBottom;
                public Value width, height, minWidth, minHeight, maxWidth, maxHeight;
                public float aspectRatio;
                public Value marginLeft, marginRight, marginTop, marginBottom;
                public Value paddingLeft, paddingRight, paddingTop, paddingBottom;
                public Value borderLeft, borderRight, borderTop, borderBottom;
                public int flexDirection;
                public int flexWrap;
                public Value flexBasis;
                public float flexGrow;
                public float flexShrink;
                public int alignItems;
                public int alignSelf;
                public int alignContent;
                public int justifyContent;
                public int justifyItems;
                public int justifySelf;
                public Value gapX;
                public Value gapY;
                public byte itemIsTable;
                public byte itemIsReplaced;
                public int floatMode;
                public int clearMode;
                public int textAlign;
                public int gridAutoFlow;
                public GridPlacement gridRowStart, gridRowEnd, gridColumnStart, gridColumnEnd;

                internal static Style DefaultFlex()
                {
                    Value zero = Value.Points(0f);
                    return new Style
                    {
                        display = 1,
                        width = Value.Auto,
                        height = Value.Auto,
                        minWidth = Value.Auto,
                        minHeight = Value.Auto,
                        maxWidth = Value.Auto,
                        maxHeight = Value.Auto,
                        insetLeft = Value.Auto,
                        insetRight = Value.Auto,
                        insetTop = Value.Auto,
                        insetBottom = Value.Auto,
                        marginLeft = zero,
                        marginRight = zero,
                        marginTop = zero,
                        marginBottom = zero,
                        paddingLeft = zero,
                        paddingRight = zero,
                        paddingTop = zero,
                        paddingBottom = zero,
                        borderLeft = zero,
                        borderRight = zero,
                        borderTop = zero,
                        borderBottom = zero,
                        flexBasis = Value.Auto,
                        flexShrink = 1f,
                        alignItems = -1,
                        alignSelf = -1,
                        alignContent = -1,
                        justifyContent = -1,
                        justifyItems = -1,
                        justifySelf = -1,
                        gapX = zero,
                        gapY = zero,
                    };
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct StyleUpdate
            {
                public ulong node;
                public Style style;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct ChildrenUpdate
            {
                public ulong parent;
                public IntPtr children;
                public uint childCount;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct CalcSpec
            {
                public int op;
                public float value;
                public IntPtr operands;
                public uint operandCount;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct Measurement
            {
                public float minWidth;
                public float minHeight;
                public float maxWidth;
                public float maxHeight;
                public float preferredWidth;
                public float preferredHeight;
                public float aspectRatio;
                public byte isReplaced;
                public IntPtr samples;
                public uint sampleCount;
            }


            [StructLayout(LayoutKind.Sequential)]
            internal struct MeasurementUpdate
            {
                public ulong node;
                public Measurement measurement;
                public byte hasMeasurement;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct Layout
            {
                public ulong node;
                public uint order;
                public float x, y, width, height;
                public float contentWidth, contentHeight;
                public float scrollWidth, scrollHeight;
            }

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int tu_context_create(out ulong context);

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int tu_context_destroy(ulong context);

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int tu_node_create(ulong context, ref Style style, out ulong node);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int tu_node_remove(ulong context, ulong node);

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int tu_calc_create(ulong context, ref CalcSpec spec, out ulong resource);

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int tu_calc_remove(ulong context, ulong resource);


            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int tu_nodes_set_styles(ulong context, [In] StyleUpdate[] updates, uint count);

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int tu_nodes_set_children(ulong context, [In] ChildrenUpdate[] updates, uint count);

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int tu_nodes_set_measurements(ulong context, [In] MeasurementUpdate[] updates, uint count);

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int tu_compute_layout(ulong context, ulong root, float width, float height);

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int tu_get_layouts_bulk(
                ulong context,
                [In] ulong[] handles,
                uint count,
                [Out] Layout[] output,
                uint capacity,
                out uint written);
        }

        private static RectTransform CreateRoot(string name, Vector2 size, TaffyFlexDirection direction)
        {
            var rootObject = new GameObject(name, typeof(RectTransform), typeof(TaffyLayoutGroup));
            RectTransform root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.sizeDelta = size;

            TaffyLayoutGroup group = rootObject.GetComponent<TaffyLayoutGroup>();
            group.direction = direction;
            group.alignItems = TaffyAlign.Start;
            return root;
        }

        private static RectTransform CreateItem(RectTransform parent, string name, float width, float height)
        {
            var childObject = new GameObject(name, typeof(RectTransform), typeof(TaffyLayoutItem));
            RectTransform child = childObject.GetComponent<RectTransform>();
            child.SetParent(parent, false);
            TaffyLayoutItem item = childObject.GetComponent<TaffyLayoutItem>();
            item.width = TaffyLength.Points(width);
            item.height = TaffyLength.Points(height);
            return child;
        }

        private static float Left(RectTransform rect)
        {
            return rect.anchoredPosition.x - rect.rect.width * rect.pivot.x;
        }

        private static float Top(RectTransform rect)
        {
            return -(rect.anchoredPosition.y + rect.rect.height * (1f - rect.pivot.y));
        }
    }
}
