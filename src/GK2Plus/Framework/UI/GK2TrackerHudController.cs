using System;
using BepInEx.Logging;
using UnityEngine;

namespace GK2Plus.Framework.UI
{
    internal sealed class GK2TrackerHudController : MonoBehaviour
    {
        private ManualLogSource _logger;
        private Func<bool> _visibleProvider;
        private Func<string> _textProvider;
        private string _cachedText = string.Empty;
        private float _nextRefreshAt;

        public static GK2TrackerHudController Create(
            ManualLogSource logger,
            Func<bool> visibleProvider,
            Func<string> textProvider)
        {
            GameObject host =
                GameObject.Find("GK2PlusTrackerHudController");

            if (host == null)
            {
                host = new GameObject(
                    "GK2PlusTrackerHudController");
                DontDestroyOnLoad(host);
            }

            GK2TrackerHudController controller =
                host.GetComponent<GK2TrackerHudController>();

            if (controller == null)
            {
                controller =
                    host.AddComponent<GK2TrackerHudController>();
            }

            controller._logger = logger;
            controller._visibleProvider = visibleProvider;
            controller._textProvider = textProvider;
            controller._nextRefreshAt = 0f;

            return controller;
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefreshAt)
            {
                return;
            }

            _nextRefreshAt =
                Time.unscaledTime + 0.25f;

            if (_visibleProvider?.Invoke() != true)
            {
                _cachedText = string.Empty;
                return;
            }

            try
            {
                _cachedText =
                    _textProvider?.Invoke() ??
                    string.Empty;
            }
            catch (Exception ex)
            {
                _cachedText = string.Empty;
                _logger?.LogError(
                    $"GK2+ tracker HUD refresh failed: {ex}");
            }
        }

        private void OnGUI()
        {
            if (string.IsNullOrWhiteSpace(_cachedText) ||
                _visibleProvider?.Invoke() != true)
            {
                return;
            }

            int lineCount =
                Math.Max(
                    1,
                    _cachedText.Split('\n').Length);

            float height =
                Mathf.Clamp(
                    40f + (lineCount * 18f),
                    70f,
                    420f);

            Rect boxRect = new Rect(
                Screen.width - 326f,
                74f,
                306f,
                height);

            GUI.Box(
                boxRect,
                "TRACKED");

            GUI.Label(
                new Rect(
                    boxRect.x + 12f,
                    boxRect.y + 28f,
                    boxRect.width - 24f,
                    boxRect.height - 36f),
                _cachedText);
        }

        public void ShutdownController()
        {
            _visibleProvider = null;
            _textProvider = null;
            _cachedText = string.Empty;

            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }
    }
}
