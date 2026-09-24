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

            Sprite backgroundSprite = FindSprite("titlescreen-menu-bg");
            Sprite frameSprite = FindSprite("comm-frame_1-border");
            Sprite dividerSprite = FindSprite("widget_perks-text_decor-drk_1");
            Sprite skullSprite = FindSprite("wskull");

            if (frameSprite == null)
            {
                throw new InvalidOperationException("Native frame sprite was not found.");
            }

            GameObject bodyTemplate = FindBodyTextTemplate(mainMenuRoot);
            GameObject titleButtonTemplate = FindTitleButtonTemplate(mainMenuRoot);

            if (bodyTemplate == null)
            {
                throw new InvalidOperationException("Could not find a native body text template.");
            }

            if (titleButtonTemplate == null)
            {
                throw new InvalidOperationException("Could not find the native New Game button background/title template.");
            }

            GameObject badge = new GameObject(BadgeObjectName, typeof(RectTransform));
            badge.transform.SetParent(mainMenuRoot, false);

            RectTransform badgeRect = (RectTransform)badge.transform;
            badgeRect.anchorMin = new Vector2(1f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(1f, 1f);
            badgeRect.anchoredPosition = new Vector2(-14f, -12f);
            badgeRect.sizeDelta = new Vector2(176f, 84f);

            if (backgroundSprite != null)
            {
                CreateStretchImage(
                    badge.transform,
                    "Background",
                    backgroundSprite,
                    Image.Type.Sliced,
                    new Vector2(-4f, -4f),
                    new Vector2(4f, 4f)
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

            // Instead of trying to imitate the button typography on a brown strip,
            // clone the actual New Game button background + its native label as one unit.
            GameObject titleBar = CreateNativeButtonTitle(
                titleButtonTemplate,
                badge.transform,
                $"GK2+ v{ModInfo.Version}",
                new Vector2(0f, -15f)
            );

            // Skull now overlaps the title button/frame as an ornament.
            if (skullSprite != null)
            {
                CreateFixedImage(
                    badge.transform,
                    "SkullTop",
                    skullSprite,
                    Image.Type.Simple,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -4f),
                    new Vector2(14f, 11f)
                );
            }

            CreateLabel(
                bodyTemplate,
                badge.transform,
                "HotkeyText",
                "Press F2 for Mod Menu",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -39f),
                new Vector2(152f, 16f),
                9f,
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
                "Status: Loaded",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -63f),
                new Vector2(146f, 14f),
                8.5f,
                new Color(0.87f, 0.80f, 0.62f, 1f)
            );

            titleBar.transform.SetAsLastSibling();
            if (skullSprite != null)
            {
                Transform skull = badge.transform.Find("SkullTop");
                if (skull != null)
                {
                    skull.SetAsLastSibling();
                }
            }

            logger.LogDebug("GK2+ badge now uses a cloned native main-menu button as the title bar.");
        }

        private static GameObject FindTitleButtonTemplate(Transform root)
        {
            Transform back = root.Find("Bg/Vertical Group/NewGame/Content/Back");
            return back != null ? back.gameObject : null;
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

        private static GameObject CreateNativeButtonTitle(
            GameObject template,
            Transform parent,
            string text,
            Vector2 anchoredPosition)
        {
            GameObject clone = UnityEngine.Object.Instantiate(template, parent, false);
            clone.name = "TitleButton";
            clone.SetActive(true);

            RectTransform rect = clone.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;

            // Preserve the button's native 142x26 dimensions and visual rendering.
            rect.sizeDelta = new Vector2(142f, 26f);
            rect.localScale = Vector3.one;

            // This object was cloned from the Back section, so remove layout behavior
            // that belonged to the main-menu vertical group.
            foreach (Component component in clone.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                string typeName = component.GetType().Name;

                if (typeName == "HorizontalLayoutGroup")
                {
                    UnityEngine.Object.Destroy(component);
                }
            }

            Transform labelTransform = clone.transform.Find("Label");

            if (labelTransform == null)
            {
                throw new InvalidOperationException("Cloned native button did not contain its Label child.");
            }

            GameObject label = labelTransform.gameObject;

            foreach (Component component in label.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                string typeName = component.GetType().Name;

                if (typeName == "LocalizedLabel" ||
                    typeName == "LocalizedVerticalOffset")
                {
                    UnityEngine.Object.Destroy(component);
                }
            }

            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.offsetMin = new Vector2(8f, 1f);
            labelRect.offsetMax = new Vector2(-8f, -1f);

            Component tmp = FindTmp(label);

            if (tmp == null)
            {
                throw new InvalidOperationException("Native title button label has no TextMeshProUGUI.");
            }

            // Only change the text. Do NOT touch font size, font material,
            // face color, outline, style, or weight.
            SetProperty(tmp, "text", text);
            TrySetEnumProperty(tmp, "alignment", "Center");

            // Make this a decorative title, not an interactive control.
            Image image = clone.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
            }

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
            Color color)
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
