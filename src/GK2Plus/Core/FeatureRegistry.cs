using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace GK2Plus.Core
{
    internal sealed class FeatureRegistry
    {
        private readonly List<IFeature> _features = new List<IFeature>();

        internal IReadOnlyList<IFeature> Features => _features;

        internal void Register(IFeature feature)
        {
            if (feature == null)
            {
                throw new ArgumentNullException(nameof(feature));
            }

            _features.Add(feature);
        }

        internal void InitializeAll(
            ConfigFile config,
            ManualLogSource logger,
            Harmony harmony
        )
        {
            logger.LogInfo(
                $"Initializing {_features.Count} registered GK2+ feature(s)..."
            );

            foreach (IFeature feature in _features)
            {
                try
                {
                    feature.Initialize(config, logger, harmony);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        $"Failed to initialize feature '{feature.Name}' ({feature.Id})."
                    );

                    logger.LogError(ex);
                }
            }
        }

        internal IFeature Find(string id)
        {
            foreach (IFeature feature in _features)
            {
                if (string.Equals(
                    feature.Id,
                    id,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return feature;
                }
            }

            return null;
        }
    }
}
