using TaffyUGUI;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI.Samples
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ResponsiveDashboardSample : MonoBehaviour
    {
        [SerializeField] private RectTransform responsiveRoot;
        [SerializeField] private TaffyLayoutGroup headerLayout;
        [SerializeField] private TaffyLayoutItem desktopNavigation;
        [SerializeField] private TaffyLayoutItem mobileMenuButton;
        [SerializeField, Min(200f)] private float mobileBreakpoint = 700f;

        private float _lastWidth = -1f;
        private bool _lastMobile;
        private bool _hasApplied;

        private void OnEnable()
        {
            EnsureBuiltinFonts();
            ApplyResponsiveVisibility(true);
        }

        private void OnValidate()
        {
            ApplyResponsiveVisibility(true);
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyResponsiveVisibility(false);
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (!Application.isPlaying)
                ApplyResponsiveVisibility(false);
        }
#endif

        private void EnsureBuiltinFonts()
        {
#if UNITY_2022_2_OR_NEWER
            Font fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            Font fallback = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            if (!fallback)
                return;

            Text[] labels = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (!labels[i].font)
                    labels[i].font = fallback;
            }
        }

        private void ApplyResponsiveVisibility(bool force)
        {
            if (!responsiveRoot)
                responsiveRoot = transform as RectTransform;
            if (!responsiveRoot || !desktopNavigation || !mobileMenuButton)
                return;

            float width = Mathf.Max(0f, responsiveRoot.rect.width);
            bool mobile = width < mobileBreakpoint;
            if (!force && _hasApplied && Mathf.Abs(width - _lastWidth) < 0.25f && mobile == _lastMobile)
                return;

            _hasApplied = true;
            _lastWidth = width;
            _lastMobile = mobile;

            desktopNavigation.display = mobile ? TaffyDisplay.None : TaffyDisplay.Flex;
            mobileMenuButton.display = mobile ? TaffyDisplay.Flex : TaffyDisplay.None;

            if (headerLayout)
                headerLayout.SetLayoutDirty();
        }
    }
}
