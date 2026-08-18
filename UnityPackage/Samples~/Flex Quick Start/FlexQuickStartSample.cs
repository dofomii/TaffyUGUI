using TaffyUGUI;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI.Samples
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class FlexQuickStartSample : MonoBehaviour
    {
        private void Start()
        {
            if (transform.childCount != 0) return;
            RectTransform root = (RectTransform)transform;
            root.sizeDelta = new Vector2(640f, 180f);
            TaffyLayoutGroup group = gameObject.GetComponent<TaffyLayoutGroup>() ?? gameObject.AddComponent<TaffyLayoutGroup>();
            group.containerDisplay = TaffyContainerDisplay.Flex;
            group.direction = TaffyFlexDirection.Row;
            group.horizontalGap = 12f;
            group.alignItems = TaffyAlign.Center;
            group.justifyContent = TaffyJustify.SpaceBetween;
            group.padding = new RectOffset(16, 16, 16, 16);

            float[] widths = { 96f, 160f, 120f };
            for (int i = 0; i < widths.Length; i++)
            {
                var childObject = new GameObject($"Flex Item {i + 1}", typeof(RectTransform), typeof(Image), typeof(TaffyLayoutItem));
                RectTransform child = childObject.GetComponent<RectTransform>();
                child.SetParent(root, false);
                TaffyLayoutItem item = childObject.GetComponent<TaffyLayoutItem>();
                item.width = TaffyLength.Points(widths[i]);
                item.height = TaffyLength.Points(64f);
                item.flexShrink = 1f;
                childObject.GetComponent<Image>().color = Color.Lerp(new Color(0.2f, 0.5f, 0.9f), new Color(0.3f, 0.85f, 0.55f), i / 2f);
            }
        }
    }
}
