using UnityEngine;

namespace GK2Plus.Framework.UI
{
    /// <summary>
    /// Centralized UI sizing and spacing for GK2+.
    ///
    /// Keep visual tuning here instead of scattering magic RectTransform
    /// numbers across controllers. Values are expressed in GK2 logical UI
    /// units (before Canvas scaling).
    /// </summary>
    internal static class GK2UiMetrics
    {
        internal static class Menu
        {
            public static readonly Vector2 WindowSize =
                new Vector2(500f, 330f);

            public static readonly Vector4 FrameInsets =
                new Vector4(9f, 7f, 9f, 7f);

            public const float HeaderTitleY = -17f;
            public const float HeaderDividerY = -38f;
            public const float TabY = -57f;
            public const float TabHeight = 20f;
            public const float TabGap = 3f;
            public const float ContentTopY = -79f;

            public static readonly Vector2 ContentSize =
                new Vector2(452f, 202f);

            public const float BodyViewportTopOffset = 34f;
            public const float BodyViewportHeight = 164f;
            public const float BodyContentHorizontalPadding = 14f;

            public const float BodyContentWidth = 414f;
            public const float PageTextWideHeight = 112f;
            public const float PageTextCompactHeight = 38f;

            public const float ControlFirstRowY = -88f;
            public const float ControlRowHeight = 22f;
            public const float ControlChildStep = 25f;
            public const float ControlFeatureGap = 36f;
            public const float ControlLabelWidth = 250f;
            public const float ControlButtonWidth = 92f;

            public const float HeaderFontScale = 0.74f;
            public const float SectionTitleScale = 0.64f;
            public const float ButtonFontScale = 0.54f;
            public const float BodyFontSize = 9.5f;
        }

        internal static class Tracker
        {
            public const float PanelWidth = 152f;
            public const float RootTopOffset = 56f;
            public const float SectionGap = 8f;
            public const float TitleHeight = 25f;
            public const float BodyTopPadding = 6f;
            public const float BodyBottomPadding = 6f;
            public const float BodyHorizontalPadding = 8f;

            public const float TitleFontSize = 12.5f;
            public const float BodyFontSize = 10f;
            public const float LineHeight = 13.5f;

            public const float IngredientCellSize = 35f;
            public const float IngredientIconSize = 29f;
            public const float IngredientGap = 3f;
            public const float IngredientCountFontSize = 11f;
            public const int IngredientColumns = 3;
        }
    }
}
