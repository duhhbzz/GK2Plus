using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using GK2Plus.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GK2Plus.Framework.UI
{
    internal static class MainMenuBadgeController
    {
        private const string BadgeObjectName = "GK2PlusMainMenuBadge";
        private static bool _created;

        public static IEnumerator Run(ManualLogSource logger)
        {
            if (_created)
            {
                yield break;
            }

            Component mainMenu = null;

            for (int frame = 0; frame < 7200 && mainMenu == null; frame++)
            {
                mainMenu = FindActiveMainMenuWindow();

                if (mainMenu == null)
                {
                    yield return null;
                }
            }

            if (mainMenu == null)
            {
                logger.LogWarning("GK2+ main-menu badge could not find an active UIMainMenuWindow.");
                yield break;
            }

            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }

            try
            {
                CreateBadge(mainMenu.transform, logger);
                _created = true;
                logger.LogInfo("GK2+ main-menu badge created.");
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to create GK2+ main-menu badge: {ex}");
            }
        }

        private static Component FindActiveMainMenuWindow()
        {
            foreach (var behaviour in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
            {
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour.GetType().Name == "UIMainMenuWindow" &&
                    behaviour.gameObject.activeInHierarchy)
                {
                    return behaviour;
                }
            }

            return null;
        }

        private static void CreateBadge(Transform mainMenuRoot, ManualLogSource logger)
        {
            Transform existing = mainMenuRoot.Find(BadgeObjectName);

            if (existing != null)
            {
                UnityEngine.Object.Destroy(existing.gameObject);
            }

            Sprite stoneBodySprite = FindSprite("comm-content_bg_dark-side-small");
            Sprite frameSprite = FindSprite("comm-frame_1-border");
            Sprite headerSprite = FindSprite("main_window-header_1");
            Sprite headerDecorSprite = FindSprite("main_window-header_1-dec_side_3");
            Sprite dividerSprite = FindSprite("widget_perks-text_decor-drk_1");

            if (frameSprite == null)
            {
                throw new InvalidOperationException("Native frame sprite was not found.");
            }

            if (headerSprite == null || headerDecorSprite == null)
            {
                throw new InvalidOperationException("Native pause-menu header sprites were not found.");
            }

            GameObject bodyTemplate = FindBodyTextTemplate(mainMenuRoot);
            GameObject headerTextTemplate = FindNativeHeaderTextTemplate();

            if (bodyTemplate == null)
            {
                throw new InvalidOperationException("Could not find a native body text template.");
            }

            if (headerTextTemplate == null)
            {
                throw new InvalidOperationException("Could not find a native window-header text template.");
            }

            GameObject badge = new GameObject(BadgeObjectName, typeof(RectTransform));
            badge.transform.SetParent(mainMenuRoot, false);

            RectTransform badgeRect = (RectTransform)badge.transform;
            badgeRect.anchorMin = new Vector2(1f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(1f, 1f);
            badgeRect.anchoredPosition = new Vector2(-14f, -12f);
            badgeRect.sizeDelta = new Vector2(176f, 84f);

            // Preserve the compact footprint established during the v0.1.0 UI pass.
            badgeRect.localScale = new Vector3(0.75f, 0.75f, 1f);

            // Match the native pause window's dark stone content area instead of
            // using the translucent title-screen background.
            if (stoneBodySprite != null)
            {
                CreateStretchImage(
                    badge.transform,
                    "StoneBody",
                    stoneBodySprite,
                    Image.Type.Tiled,
                    new Vector2(9f, 9f),
                    new Vector2(-9f, -28f)
                );
            }

            CreateStretchImage(
                badge.transform,
                "Frame",
                frameSprite,
                Image.Type.Sliced,
                Vector2.zero,
                Vector2.zero
            );

            // Build a compact version of the native pause-window header. There is
            // intentionally no close button, so the decorative side pieces are
            // symmetrical around the centered GK2+ title.
            GameObject headerGroup = new GameObject("HeaderGroup", typeof(RectTransform));
            headerGroup.transform.SetParent(badge.transform, false);

            RectTransform headerRect = (RectTransform)headerGroup.transform;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0f, -11f);
            headerRect.sizeDelta = new Vector2(-22f, 26f);

            CreateStretchImage(
                headerGroup.transform,
                "Background",
                headerSprite,
                Image.Type.Sliced,
                Vector2.zero,
                Vector2.zero
            );

            CreateFixedImage(
                headerGroup.transform,
                "DecorCommonLeft",
                headerDecorSprite,
                Image.Type.Simple,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(14f, -13f),
                new Vector2(28f, 26f)
            );

            GameObject rightDecor = CreateFixedImage(
                headerGroup.transform,
                "DecorCommonRight",
                headerDecorSprite,
                Image.Type.Simple,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-14f, -13f),
                new Vector2(28f, 26f)
            );
            rightDecor.transform.localScale = new Vector3(-1f, 1f, 1f);

            CreateNativeHeaderTitle(
                headerTextTemplate,
                headerGroup.transform,
                $"GK2+ v{ModInfo.Version}"
            );

            CreateLabel(
                bodyTemplate,
                badge.transform,
                "HotkeyText",
                "Press F2 for Mod Menu",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -39f),
                new Vector2(152f, 18f),
                11f,
                new Color(0.93f, 0.78f, 0.50f, 1f)
            );

            if (dividerSprite != null)
            {
                CreateFixedImage(
                    badge.transform,
                    "Divider",
                    dividerSprite,
                    Image.Type.Sliced,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -55f),
                    new Vector2(124f, 6f)
                );
            }

            CreateLabel(
                bodyTemplate,
                badge.transform,
                "StatusText",
                GameCompatibility.BadgeText,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -61f),
                new Vector2(146f, 18f),
                13f,
                new Color(0.87f, 0.80f, 0.62f, 1f),
                2.5f
            );

            headerGroup.transform.SetAsLastSibling();

            logger.LogDebug(
                "GK2+ badge uses the native pause-window stone body and symmetric header styling.");
        }

        private static GameObject FindNativeHeaderTextTemplate()
        {
            MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();

            // Prefer the exact pause-window header the badge is visually matching.
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null ||
                    behaviour.GetType().Name != "UIGamePauseWindow")
                {
                    continue;
                }

                Transform pauseHeader = behaviour.transform.Find(
                    "GenericWIndowLayout/Frame/HeaderGroup/Header");

                if (pauseHeader != null &&
                    FindTmp(pauseHeader.gameObject) != null)
                {
                    return pauseHeader.gameObject;
                }
            }

            // Fall back to another native GenericWindowLayout header if the pause
            // window has not been materialized yet during main-menu startup.
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                Transform header = behaviour.transform.Find(
                    "GenericWIndowLayout/Frame/HeaderGroup/Header");

                if (header != null && FindTmp(header.gameObject) != null)
                {
                    return header.gameObject;
                }
            }

            return null;
        }

        private static GameObject FindBodyTextTemplate(Transform root)
        {
            Transform hint = root.Find("Bg/Vertical Group/ButtonTipsStr");

            if (hint != null)
            {
                return hint.gameObject;
            }

            Transform fallback = root.Find("Bg/Vertical Group/NewGame/Content/Back/Label");
            return fallback != null ? fallback.gameObject : null;
        }

        private static GameObject CreateNativeHeaderTitle(
            GameObject template,
            Transform parent,
            string text)
        {
            GameObject clone = UnityEngine.Object.Instantiate(template, parent, false);
            clone.name = "Header";
            clone.SetActive(true);

            foreach (Component component in clone.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                string typeName = component.GetType().Name;

                if (typeName == "LocalizedLabel" ||
                    typeName == "LanguageRtlLabelState")
                {
                    if (component is Behaviour behaviour)
                    {
                        behaviour.enabled = false;
                    }

                    UnityEngine.Object.Destroy(component);
                }
            }

            RectTransform rect = clone.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = new Vector2(28f, 0f);
            rect.offsetMax = new Vector2(-28f, 0f);
            rect.localScale = Vector3.one;

            Component tmp = FindTmp(clone);

            if (tmp == null)
            {
                throw new InvalidOperationException(
                    "Native window-header template has no TextMeshProUGUI.");
            }

            // Preserve the game's native header font, material, face color, outline,
            // style, and weight; only replace the localized text and alignment.
            SetProperty(tmp, "text", text);
            SetProperty(tmp, "raycastTarget", false);
            TrySetEnumProperty(tmp, "alignment", "Center");

            return clone;
        }

        private static Sprite FindSprite(string name)
        {
            return Resources.FindObjectsOfTypeAll<Sprite>()
                .FirstOrDefault(sprite =>
                    sprite != null &&
                    string.Equals(sprite.name, name, StringComparison.Ordinal));
        }

        private static GameObject CreateStretchImage(
            Transform parent,
            string name,
            Sprite sprite,
            Image.Type type,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject obj = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            obj.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Image image = obj.GetComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.preserveAspect = false;
            image.raycastTarget = false;

            return obj;
        }

        private static GameObject CreateFixedImage(
            Transform parent,
            string name,
            Sprite sprite,
            Image.Type type,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject obj = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            obj.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = obj.GetComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.preserveAspect = true;
            image.raycastTarget = false;

            return obj;
        }

        private static GameObject CreateLabel(
            GameObject template,
            Transform parent,
            string name,
            string text,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            float fontSize,
            Color color,
            float characterSpacing = 0f)
        {
            GameObject clone = UnityEngine.Object.Instantiate(template, parent, false);
            clone.name = name;
            clone.SetActive(true);

            foreach (Component component in clone.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                string typeName = component.GetType().Name;

                if (typeName == "LazyButtonTipsStr" ||
                    typeName == "LocalizedLabel" ||
                    typeName == "LocalizedVerticalOffset")
                {
                    UnityEngine.Object.Destroy(component);
                }
            }

            RectTransform rect = clone.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Component tmp = FindTmp(clone);

            if (tmp == null)
            {
                throw new InvalidOperationException("Cloned native label has no TextMeshProUGUI.");
            }

            SetProperty(tmp, "text", text);
            SetProperty(tmp, "fontSize", fontSize);
            SetProperty(tmp, "color", color);
            SetProperty(tmp, "characterSpacing", characterSpacing);
            TrySetEnumProperty(tmp, "fontStyle", "Normal");
            TrySetEnumProperty(tmp, "fontWeight", "Regular");
            TrySetEnumProperty(tmp, "alignment", "Center");

            return clone;
        }

        private static Component FindTmp(GameObject obj)
        {
            return obj.GetComponents<Component>()
                .FirstOrDefault(component =>
                    component != null &&
                    component.GetType().Name == "TextMeshProUGUI");
        }

        private static void TrySetEnumProperty(
            Component component,
            string propertyName,
            string enumValue)
        {
            PropertyInfo property = component.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (property == null || !property.CanWrite || !property.PropertyType.IsEnum)
            {
                return;
            }

            try
            {
                object value = Enum.Parse(property.PropertyType, enumValue);
                property.SetValue(component, value, null);
            }
            catch
            {
            }
        }

        private static void SetProperty(Component component, string propertyName, object value)
        {
            if (component == null)
            {
                return;
            }

            PropertyInfo property = component.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (property != null && property.CanWrite)
            {
                property.SetValue(component, value, null);
            }
        }
    }
}
