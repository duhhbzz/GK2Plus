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
        public Sprite ContentCellSprite { get; private set; }
        public Sprite NativePaneSurfaceSprite { get; private set; }
        public Color NativePaneSurfaceColor { get; private set; } = Color.white;
        public Image.Type NativePaneSurfaceType { get; private set; } = Image.Type.Sliced;
        public Sprite NativePaneFrameSprite { get; private set; }
        public Color NativePaneFrameColor { get; private set; } = Color.white;
        public Image.Type NativePaneFrameType { get; private set; } = Image.Type.Sliced;
        public Sprite ItemSlotSprite { get; private set; }
        public Material ItemIconMaterial { get; private set; }

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

                TextMeshProUGUI laterButtonTemplate =
                    FindTmp(buttonFallback);

                if (laterButtonTemplate != null)
                {
                    _current.ButtonTextTemplate =
                        laterButtonTemplate;
                }

                if (_current.CountTextTemplate == null ||
                    _current.ItemSlotSprite == null ||
                    _current.ItemIconMaterial == null)
                {
                    _current.ResolveItemCellStyle();
                }

                _current.ContentCellSprite ??=
                    FindSprite("comm-cell_dark_2");

                if (_current.NativePaneSurfaceSprite == null)
                {
                    _current.ResolveNativePaneStyle(
                        logger);
                }

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

            theme.WindowFrameSprite =
                FindSprite("comm-frame_1-border");
            theme.WindowBackgroundSprite =
                FindSprite("titlescreen-menu-bg");
            theme.DividerSprite =
                FindSprite("widget_perks-text_decor-drk_1");
            theme.ButtonSprite =
                FindSprite("comm-btn-simple_red-active");
            theme.ContentCellSprite =
                FindSprite("comm-cell_dark_2");

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
            theme.ResolveSectionHeaderStyle();
            theme.ResolveInspirationCardStyle();
            theme.ResolveNativePaneStyle(
                logger);

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

        private void ResolveNativePaneStyle(
            ManualLogSource logger)
        {
            InspirationWidget widget =
                Resources
                    .FindObjectsOfTypeAll<InspirationWidget>()
                    .FirstOrDefault(candidate =>
                        candidate != null &&
                        candidate.transform != null);

            if (widget == null)
            {
                return;
            }

            Image surface =
                null;

            Image frame =
                null;

            Transform current =
                widget.transform;

            int depth =
                0;

            while (current != null &&
                   depth < 10 &&
                   (surface == null ||
                    frame == null))
            {
                Image[] images =
                    current
                        .GetComponentsInChildren<Image>(
                            true)
                        .Where(image =>
                            image != null &&
                            (image.transform == current ||
                             image.transform.parent == current))
                        .ToArray();

                foreach (Image image in images)
                {
                    if (image.sprite == null ||
                        !IsLargePaneImage(
                            image))
                    {
                        continue;
                    }

                    string objectName =
                        image.gameObject.name ??
                        string.Empty;

                    string spriteName =
                        image.sprite.name ??
                        string.Empty;

                    bool looksLikeFrame =
                        ContainsAny(
                            objectName,
                            "frame",
                            "border") ||
                        ContainsAny(
                            spriteName,
                            "frame",
                            "border");

                    if (looksLikeFrame)
                    {
                        frame ??=
                            image;
                    }
                    else
                    {
                        surface ??=
                            image;
                    }
                }

                current =
                    current.parent;

                depth++;
            }

            if (surface != null)
            {
                NativePaneSurfaceSprite =
                    surface.sprite;

                NativePaneSurfaceColor =
                    surface.color;

                NativePaneSurfaceType =
                    surface.type;
            }

            if (frame != null)
            {
                NativePaneFrameSprite =
                    frame.sprite;

                NativePaneFrameColor =
                    frame.color;

                NativePaneFrameType =
                    frame.type;
            }

            if (NativePaneSurfaceSprite != null)
            {
                logger?.LogInfo(
                    $"GK2+ native page pane surface: '{NativePaneSurfaceSprite.name}'" +
                    (NativePaneFrameSprite != null
                        ? $", frame: '{NativePaneFrameSprite.name}'."
                        : ".") +
                    $" Source: '{surface?.gameObject?.name ?? "unknown"}'.");
            }
        }

        private static bool IsLargePaneImage(
            Image image)
        {
            if (image?.rectTransform == null)
            {
                return false;
            }

            Rect rect =
                image.rectTransform.rect;

            Vector2 sizeDelta =
                image.rectTransform.sizeDelta;

            float width =
                Mathf.Max(
                    Mathf.Abs(
                        rect.width),
                    Mathf.Abs(
                        sizeDelta.x));

            float height =
                Mathf.Max(
                    Mathf.Abs(
                        rect.height),
                    Mathf.Abs(
                        sizeDelta.y));

            return
                width >= 220f &&
                height >= 160f;
        }

        private static bool ContainsAny(
            string value,
            params string[] terms)
        {
            if (string.IsNullOrEmpty(
                    value) ||
                terms == null)
            {
                return false;
            }

            foreach (string term in terms)
            {
                if (!string.IsNullOrEmpty(
                        term) &&
                    value.IndexOf(
                        term,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
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
