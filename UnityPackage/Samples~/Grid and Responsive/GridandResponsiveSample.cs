using System.Collections.Generic;
using TaffyUGUI;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI.Samples
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class GridAndResponsiveSample : MonoBehaviour
    {
        private void Start()
        {
            if (transform.childCount != 0) return;
            RectTransform root = (RectTransform)transform;
            root.sizeDelta = new Vector2(640f, 300f);
            TaffyLayoutGroup group = gameObject.GetComponent<TaffyLayoutGroup>() ?? gameObject.AddComponent<TaffyLayoutGroup>();
            group.containerDisplay = TaffyContainerDisplay.Grid;
            group.gridColumns = new List<TaffyGridTrack> { TaffyGridTrack.Fraction(1f), TaffyGridTrack.Fraction(2f) };
            group.gridAutoRows = new List<TaffyGridTrack> { TaffyGridTrack.Points(96f) };
            group.horizontalGap = 12f;
            group.verticalGap = 12f;
            group.padding = new RectOffset(16, 16, 16, 16);
            group.responsiveProfiles = new List<TaffyResponsiveProfile>
            {
                new TaffyResponsiveProfile
                {
                    name = "Compact",
                    priority = 10,
                    maxWidth = 420f,
                    overrideContainerDisplay = true,
                    containerDisplay = TaffyContainerDisplay.Flex,
                    overrideFlexDirection = true,
                    direction = TaffyFlexDirection.Column,
                    overrideGaps = true,
                    horizontalGap = 8f,
                    verticalGap = 8f,
                }
            };

            for (int i = 0; i < 4; i++)
            {
                var childObject = new GameObject($"Grid Item {i + 1}", typeof(RectTransform), typeof(Image), typeof(TaffyLayoutItem));
                childObject.transform.SetParent(root, false);
                TaffyLayoutItem item = childObject.GetComponent<TaffyLayoutItem>();
                item.minWidth = TaffyLength.Points(72f);
                item.height = TaffyLength.Points(80f);
                if (i == 0)
                {
                    item.width = TaffyLength.Calc(TaffyCalcExpression.Clamp(
                        TaffyCalcExpression.Length(80f),
                        TaffyCalcExpression.Percent(0.5f),
                        TaffyCalcExpression.Length(220f)));
                }
                childObject.GetComponent<Image>().color = Color.Lerp(new Color(0.75f, 0.35f, 0.35f), new Color(0.55f, 0.35f, 0.85f), i / 3f);
            }
        }
    }
}
