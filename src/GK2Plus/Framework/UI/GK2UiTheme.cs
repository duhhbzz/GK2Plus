using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2Plus.Framework.UI
{
    /// <summary>
    /// Resolves GK2's native UI assets once and exposes them as a reusable
    /// theme for all GK2+ views.
    /// </summary>
    internal sealed class GK2UiTheme
    {
        private static GK2UiTheme _current;

        public static GK2UiTheme Current => _current;

        public TextMeshProUGUI TitleTextTemplate { get; private set; }
        public TextMeshProUGUI BodyTextTemplate { get; private set; }
        public TextMeshProUGUI ButtonTextTemplate { get; private set; }
        public TextMeshProUGUI CountTextTemplate { get; private set; }

        public Sprite WindowFrameSprite { get; private set; }
        public Sprite WindowBackgroundSprite { get; private set; }
        public Sprite DividerSprite { get; private set; }
        // Native red action button.
        public Sprite ButtonSprite { get; private set; }
        public Sprite ButtonHighlightedSprite { get; private set; }
        public Sprite ButtonPressedSprite { get; private set; }
        public Sprite ButtonDisabledSprite { get; private set; }

        public Sprite ContentCellSprite { get; private set; }
        public Sprite ContentStoneSprite { get; private set; }

        // Native inventory/item cell.
        public Sprite ItemSlotSprite { get; private set; }
        public Sprite ItemSelectionSprite { get; private set; }
        public Sprite ItemInactiveSprite { get; private set; }
        public Material ItemIconMaterial { get; private set; }

        // Native CharacterWindow header/tab chrome.
        public Sprite MainWindowHeaderSprite { get; private set; }
        public Sprite MainWindowHeaderSideSprite { get; private set; }
        public Sprite MainWindowHeaderSplitterSprite { get; private set; }
        public Sprite MainWindowHeaderButtonSprite { get; private set; }
        public Sprite MainWindowHeaderButtonOverSprite { get; private set; }
        public Sprite MainWindowHeaderSeparatorSprite { get; private set; }
        public Sprite MainWindowHeaderTabLeftSprite { get; private set; }
        public Sprite MainWindowHeaderTabRightSprite { get; private set; }

        // Native inspiration progress bar.
        public Sprite ProgressFrameSprite { get; private set; }
        public Sprite ProgressFillSprite { get; private set; }
        public TextMeshProUGUI ProgressTextTemplate { get; private set; }

        // Native tooltip.
        public Sprite TooltipFrameSprite { get; private set; }
        public Sprite TooltipTailSprite { get; private set; }
        public Sprite TooltipSeparatorSprite { get; private set; }
        public TextMeshProUGUI TooltipRegularTextTemplate { get; private set; }
        public TextMeshProUGUI TooltipAdditionalTextTemplate { get; private set; }

        // Native dialog body style.
        public TextMeshProUGUI DialogBodyTextTemplate { get; private set; }

        public Sprite SectionHeaderSprite { get; private set; }
        public Sprite SectionHeaderInactiveSprite { get; private set; }
        public TextStyle SectionHeaderTextStyle { get; private set; }

        public Sprite InspirationCardSprite { get; private set; }
        public Sprite InspirationCardCompletedSprite { get; private set; }
        public Sprite InspirationIconFrameSprite { get; private set; }
        public TextMeshProUGUI InspirationTitleTemplate { get; private set; }
        public TextMeshProUGUI InspirationDescriptionTemplate { get; private set; }
        public TextMeshProUGUI InspirationProgressTemplate { get; private set; }

        public Color ItemIconTint { get; private set; } = Color.white;
        public TextStyle CountNormalStyle { get; private set; }
        public TextStyle CountRedStyle { get; private set; }

        public Color PanelBackground =>
            new Color(0.055f, 0.060f, 0.073f, 0.97f);

        public Color ContentBackground =>
            new Color(0.075f, 0.082f, 0.098f, 0.96f);

        public Color HudBackground =>
            new Color(0f, 0f, 0f, 0.68f);

        public Color HudTitleBackground =>
            new Color(0.10f, 0.08f, 0.06f, 0.94f);

        public Color AccentText =>
            new Color(1f, 0.82f, 0.45f, 1f);

        public Color ButtonNeutral =>
            new Color(0.62f, 0.57f, 0.52f, 0.92f);

        public Color ButtonSelected =>
            new Color(0.92f, 0.76f, 0.52f, 1f);

        public Color HeaderBackground =>
            new Color(0.43f, 0.33f, 0.20f, 0.97f);

        public Color TabNeutral =>
            new Color(0.15f, 0.17f, 0.21f, 0.96f);

        public Color TabSelected =>
            new Color(0.38f, 0.30f, 0.20f, 0.98f);

        public Color TabBorder =>
            new Color(0.34f, 0.36f, 0.40f, 0.85f);

        public Color RowBackground =>
            new Color(0.12f, 0.13f, 0.16f, 0.88f);

        public Color ChildRowBackground =>
            new Color(0.095f, 0.105f, 0.13f, 0.82f);

        public bool IsReady =>
            TitleTextTemplate != null &&
            BodyTextTemplate != null &&
            ButtonTextTemplate != null;

        public static GK2UiTheme Resolve(
            ManualLogSource logger,
            GameObject bodyFallback = null,
            GameObject buttonFallback = null)
        {
            if (_current != null &&
                _current.IsReady)
            {
                // Retry exact name/style lookups because some native UI assets
                // are loaded lazily as their vanilla screens are opened.
                _current.ResolveKnownNativeAssets();

                TextMeshProUGUI nativeTitle =
                    ResolveInventoryHeaderTemplate();

                if (nativeTitle != null)
                {
                    _current.TitleTextTemplate =
                        nativeTitle;
                }

                TextMeshProUGUI nativeBody =
                    ResolveBuildingListTemplate();

                if (nativeBody != null)
                {
                    _current.BodyTextTemplate =
                        nativeBody;
                }
                else if (_current.BodyTextTemplate == null)
                {
                    _current.BodyTextTemplate =
                        FindTmp(bodyFallback);
                }

                if (_current.ButtonTextTemplate == null)
                {
                    TextMeshProUGUI laterButtonTemplate =
                        FindTmp(buttonFallback);

                    if (laterButtonTemplate != null)
                    {
                        _current.ButtonTextTemplate =
                            laterButtonTemplate;
                    }
                }

                if (_current.CountTextTemplate == null ||
                    _current.ItemSlotSprite == null ||
                    _current.ItemIconMaterial == null)
                {
                    _current.ResolveItemCellStyle();
                }

                _current.ContentCellSprite ??=
                    FindSprite("comm-cell_dark_2");

                if (_current.SectionHeaderSprite == null)
                {
                    _current.ResolveSectionHeaderStyle();
                }

                if (_current.InspirationCardSprite == null ||
                    _current.InspirationTitleTemplate == null)
                {
                    _current.ResolveInspirationCardStyle();
                }

                return _current;
            }

            GK2UiTheme theme =
                _current ??
                new GK2UiTheme();

            theme.ResolveKnownNativeAssets();

            theme.TitleTextTemplate =
                ResolveInventoryHeaderTemplate();

            theme.BodyTextTemplate =
                ResolveBuildingListTemplate();

            if (theme.ButtonTextTemplate == null)
            {
                theme.ButtonTextTemplate =
                    FindTmp(buttonFallback);
            }

            if (theme.ButtonTextTemplate == null)
            {
                theme.ButtonTextTemplate =
                    theme.BodyTextTemplate;
            }

            if (theme.BodyTextTemplate == null)
            {
                theme.BodyTextTemplate =
                    FindTmp(bodyFallback);
            }

            TextMeshProUGUI fallback =
                Resources
                    .FindObjectsOfTypeAll<TextMeshProUGUI>()
                    .FirstOrDefault(text =>
                        text != null &&
                        text.font != null &&
                        text.gameObject.activeInHierarchy) ??
                Resources
                    .FindObjectsOfTypeAll<TextMeshProUGUI>()
                    .FirstOrDefault(text =>
                        text != null &&
                        text.font != null);

            theme.TitleTextTemplate ??=
                fallback;
            theme.BodyTextTemplate ??=
                fallback;
            theme.ButtonTextTemplate ??=
                fallback;

            theme.ResolveItemCellStyle();
            theme.ResolveSectionHeaderStyle();
            theme.ResolveInspirationCardStyle();

            _current =
                theme;

            logger?.LogInfo(
                "GK2+ native UI theme cache resolved.");

            return theme;
        }

        public static void Reset()
        {
            _current = null;
        }

        /// <summary>
        /// Resolve the exact native primitives harvested from GK2 v1.008.
        /// These are explicit asset/style names rather than heuristic
        /// "looks close enough" matches. Missing assets are left null and
        /// retried on later Resolve calls as vanilla screens load.
        /// </summary>
        private void ResolveKnownNativeAssets()
        {
            WindowFrameSprite ??=
                FindSprite("comm-frame_1-border");
            WindowBackgroundSprite ??=
                FindSprite("comm-frame_bg_1");

            DividerSprite ??=
                FindSprite("widget_perks-text_decor-drk_1");
            ContentCellSprite ??=
                FindSprite("comm-cell_dark_2");
            ContentStoneSprite ??=
                FindSprite("comm-content_bg_dark-side-small");

            ButtonSprite ??=
                FindSprite("comm-btn-simple_red-active");
            ButtonHighlightedSprite ??=
                FindSprite("comm-btn-simple_red-over");
            ButtonPressedSprite ??=
                FindSprite("comm-btn-simple_red-press");
            ButtonDisabledSprite ??=
                FindSprite("comm-btn-simple_red-inactive");

            ItemSlotSprite ??=
                FindSprite("widget_items_cell-inventory_item_cell");
            ItemSelectionSprite ??=
                FindSprite("selection");
            ItemInactiveSprite ??=
                FindSprite("comm-item-inactive_shade");

            MainWindowHeaderSprite ??=
                FindSprite("main_window-header_1");
            MainWindowHeaderSideSprite ??=
                FindSprite("main_window-header_1-dec_side_3");
            MainWindowHeaderSplitterSprite ??=
                FindSprite("comm-header_frame_splitter");
            MainWindowHeaderButtonSprite ??=
                FindSprite("main_window-header_1-button");
            MainWindowHeaderButtonOverSprite ??=
                FindSprite("main_window-header_1-button-over");
            MainWindowHeaderSeparatorSprite ??=
                FindSprite("main_window-header_1-dec_separator");
            MainWindowHeaderTabLeftSprite ??=
                FindSprite("main_window-header_1-dec_left");
            MainWindowHeaderTabRightSprite ??=
                FindSprite("main_window-header_1-dec_right");

            ProgressFrameSprite ??=
                FindSprite("inspiration-plate-green-value_frame");
            ProgressFillSprite ??=
                FindSprite("inspiration-plate-green-bar_fill");

            TooltipFrameSprite ??=
                FindSprite("hint-frame");
            TooltipTailSprite ??=
                FindSprite("hint-frame_tail");
            TooltipSeparatorSprite ??=
                FindSprite("hint-text_separator");

            TextMeshProUGUI nativeButtonText =
                FindTmpByStyle(
                    "small_font_bold",
                    "btn_red_active");

            if (nativeButtonText != null)
            {
                // Upgrade an earlier generic fallback once the vanilla red
                // button style has actually been loaded by the game.
                ButtonTextTemplate =
                    nativeButtonText;
            }

            ProgressTextTemplate ??=
                FindTmpByStyle(
                    "tiny_font",
                    "inspiration-plate_bar_progress");

            TooltipRegularTextTemplate ??=
                FindTmpByStyle(
                    "small_font",
                    "hint_regular_text");

            TooltipAdditionalTextTemplate ??=
                FindTmpByStyle(
                    "tiny_font",
                    "hint_additional_text");

            DialogBodyTextTemplate ??=
                FindTmpByStyle(
                    "small_font",
                    "regular_text_3");
        }

        private void ResolveItemCellStyle()
        {
            UIItemCell itemCell =
                Resources
                    .FindObjectsOfTypeAll<UIItemCell>()
                    .FirstOrDefault(cell =>
                        cell != null &&
                        cell.Background != null &&
                        cell.Icon != null);

            if (itemCell == null)
            {
                return;
            }

            ItemSlotSprite =
                itemCell.Background.sprite;

            ItemIconMaterial =
                itemCell.Icon.material;

            CountTextTemplate =
                Traverse.Create(itemCell)
                    .Field("countLabel")
                    .GetValue<TextMeshProUGUI>();

            ImageColors colors =
                Traverse.Create(itemCell)
                    .Field("colors")
                    .GetValue<ImageColors>();

            if (colors != null)
            {
                ItemIconTint =
                    colors.NormalColor;
            }

            CountNormalStyle =
                Traverse.Create(itemCell)
                    .Field("countLabelNormal")
                    .GetValue<TextStyle>();

            CountRedStyle =
                Traverse.Create(itemCell)
                    .Field("countLabelRed")
                    .GetValue<TextStyle>();
        }

        private void ResolveSectionHeaderStyle()
        {
            InventoryHeaderWidget widget =
                Resources
                    .FindObjectsOfTypeAll<InventoryHeaderWidget>()
                    .FirstOrDefault(candidate =>
                        candidate != null);

            if (widget == null)
            {
                return;
            }

            SectionHeaderSprite =
                Traverse.Create(widget)
                    .Field("headerBackgroundActiveSprite")
                    .GetValue<Sprite>();

            SectionHeaderInactiveSprite =
                Traverse.Create(widget)
                    .Field("headerBackgroundInactiveSprite")
                    .GetValue<Sprite>();

            SectionHeaderTextStyle =
                Traverse.Create(widget)
                    .Field("headerActiveStyle")
                    .GetValue<TextStyle>();
        }

        private void ResolveInspirationCardStyle()
        {
            InspirationWidget widget =
                Resources
                    .FindObjectsOfTypeAll<InspirationWidget>()
                    .FirstOrDefault(candidate =>
                        candidate != null);

            if (widget == null)
            {
                return;
            }

            InspirationCardSprite =
                Traverse.Create(widget)
                    .Field("backNotCompletedSprite")
                    .GetValue<Sprite>();

            InspirationCardCompletedSprite =
                Traverse.Create(widget)
                    .Field("backCompletedSprite")
                    .GetValue<Sprite>();

            Sprite[] frames =
                Traverse.Create(widget)
                    .Field("framesSprites")
                    .GetValue<Sprite[]>();

            if (frames != null &&
                frames.Length > 0)
            {
                InspirationIconFrameSprite =
                    frames[0];
            }

            InspirationTitleTemplate =
                Traverse.Create(widget)
                    .Field("idLabel")
                    .GetValue<TextMeshProUGUI>();

            InspirationDescriptionTemplate =
                Traverse.Create(widget)
                    .Field("descriptionLabel")
                    .GetValue<TextMeshProUGUI>();

            InspirationProgressTemplate =
                Traverse.Create(widget)
                    .Field("progressLabel")
                    .GetValue<TextMeshProUGUI>();
        }

        private static TextMeshProUGUI ResolveInventoryHeaderTemplate()
        {
            foreach (InventoryHeaderWidget widget in
                     Resources.FindObjectsOfTypeAll<InventoryHeaderWidget>())
            {
                if (widget == null)
                {
                    continue;
                }

                TextMeshProUGUI header =
                    Traverse.Create(widget)
                        .Field("header")
                        .GetValue<TextMeshProUGUI>();

                if (header != null &&
                    header.font != null)
                {
                    return header;
                }
            }

            return null;
        }

        private static TextMeshProUGUI ResolveBuildingListTemplate()
        {
            foreach (UIBuildingWidget widget in
                     Resources.FindObjectsOfTypeAll<UIBuildingWidget>())
            {
                if (widget == null)
                {
                    continue;
                }

                TextMeshProUGUI label =
                    Traverse.Create(widget)
                        .Field("nameLabel")
                        .GetValue<TextMeshProUGUI>();

                if (label != null &&
                    label.font != null)
                {
                    return label;
                }
            }

            return null;
        }

        internal static Sprite FindSprite(
            string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return Resources
                .FindObjectsOfTypeAll<Sprite>()
                .FirstOrDefault(sprite =>
                    sprite != null &&
                    string.Equals(
                        sprite.name,
                        name,
                        StringComparison.OrdinalIgnoreCase));
        }

        private static TextMeshProUGUI FindTmpByStyle(
            string fontName,
            string materialNameFragment)
        {
            return Resources
                .FindObjectsOfTypeAll<TextMeshProUGUI>()
                .FirstOrDefault(text =>
                    text != null &&
                    text.font != null &&
                    string.Equals(
                        text.font.name,
                        fontName,
                        StringComparison.OrdinalIgnoreCase) &&
                    text.fontSharedMaterial != null &&
                    text.fontSharedMaterial.name.IndexOf(
                        materialNameFragment,
                        StringComparison.OrdinalIgnoreCase) >= 0);
        }

        internal static TextMeshProUGUI FindTmp(
            GameObject source)
        {
            if (source == null)
            {
                return null;
            }

            return source.GetComponent<TextMeshProUGUI>() ??
                   source.GetComponentInChildren<TextMeshProUGUI>(
                       true);
        }
    }
}
