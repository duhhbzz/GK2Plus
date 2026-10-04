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
        /// <summary>
        /// Measurements captured directly from GK2 v1.008 native UI.
        /// Keep these separate from feature-specific layout tuning.
        /// </summary>
        internal static class Native
        {
            // comm-frame_1-border + comm-frame_bg_1.
            public const float WindowBackInset = 13f;

            // comm-btn-simple_red-*.
            public static readonly Vector2 RedButtonSize =
                new Vector2(162f, 26f);
            public const int RedButtonPaddingHorizontal = 10;
            public const int RedButtonPaddingTop = 2;
            public const int RedButtonPaddingBottom = 0;
            public const float RedButtonFontSize = 16f;

            // UIItemCell.
            public const float ItemCellSize = 42f;
            public const float ItemCellBackgroundSize = 44f;
            public const float ItemIconSize = 48f;

            // Inspiration progress bar.
            public const float ProgressHorizontalInset = 2f;
            public const float ProgressBottomInset = 1.5f;
            public const float ProgressTopInset = 2.5f;
            public const float ProgressLabelFontSize = 16f;

            // UITooltip.
            public const int TooltipPadding = 14;
            public const float TooltipSpacing = 2f;
            public const float TooltipMinWidth = 60f;
            public const float TooltipPreferredWidth = 200f;
            public const float TooltipTextSize = 16f;
            public const float TooltipSeparatorWidth = 96f;
            public const float TooltipSeparatorHeight = 6f;
            public const float TooltipTailSize = 10f;

            // UIDialogWindow.
            public const float DialogMinimumWidth = 200f;
            public const float DialogMinimumHeight = 100f;
            public const int DialogPaddingHorizontal = 24;
            public const int DialogPaddingTop = 47;
            public const int DialogPaddingBottom = 23;
            public const float DialogContentSpacing = 6f;
            public const float DialogShadowAlpha = 0.4f;
            public const int DialogSortingOrder = 415;
        }

        internal static class Menu
        {
            public static readonly Vector2 WindowSize =
                new Vector2(500f, 330f);

            public static readonly Vector4 FrameInsets =
                new Vector4(9f, 7f, 9f, 7f);

            public const float HeaderTitleY = -17f;
            public const float HeaderDividerY = -38f;
            public const float TabY = -57f;
            public const float TabHeight = 26f;
            public const float TabGap = -1f;
            public const float ContentTopY = -86f;

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

        internal static class QuestJournal
        {
            public const float OuterPadding = 10f;
            public const float PaneGap = 8f;
            public const float LeftPaneWidth = 275f;
            public const float SectionHeaderHeight = 39f;
            public const float FilterHeight = 25f;
            public const float FilterGap = 5f;
            public const float QuestGroupHeight = 54f;
            public const float QuestGroupIconSize = 48f;
            public const float QuestSubRowIndent = 16f;
            public const float QuestRowHeight = 72f;
            public const float QuestRowGap = 5f;
            public const float QuestIconSize = 58f;
            public const float QuestCardPadding = 8f;
            public const float QuestProgressHeight = 12f;
            public const float DetailPadding = 12f;
            public const float DetailPortraitSize = 70f;
            public const float DetailPortraitGap = 10f;
            public const float DetailTitleSize = 15f;
            public const float DetailBodySize = 10.5f;
            public const float DetailStatusSize = 9f;
            public const float ObjectiveCellSize = 44f;
            public const float ObjectiveCellGap = 6f;
            public const int ObjectiveColumns = 4;
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
