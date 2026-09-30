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
        public Sprite ButtonSprite { get; private set; }
        public Sprite ItemSlotSprite { get; private set; }

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
                return _current;
            }

            GK2UiTheme theme =
                _current ??
                new GK2UiTheme();

            theme.WindowFrameSprite =
                FindSprite("comm-frame_1-border");
            theme.WindowBackgroundSprite =
                FindSprite("titlescreen-menu-bg");
            theme.DividerSprite =
                FindSprite("widget_perks-text_decor-drk_1");
            theme.ButtonSprite =
                FindSprite("comm-btn-simple_red-active");

            theme.TitleTextTemplate =
                ResolveInventoryHeaderTemplate();

            theme.BodyTextTemplate =
                ResolveBuildingListTemplate();

            theme.ButtonTextTemplate =
                FindTmp(buttonFallback);

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
