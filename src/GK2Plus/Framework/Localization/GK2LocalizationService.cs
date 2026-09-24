using BepInEx.Logging;

namespace GK2Plus.Framework.Localization
{
    /// <summary>
    /// GK2+ adapter boundary for Localization.
    ///
    /// Keep direct game-internal access inside this layer rather than feature modules.
    /// </summary>
    internal sealed class GK2LocalizationService : GK2ServiceBase
    {
        public GK2LocalizationService(ManualLogSource logger)
            : base(logger)
        {
        }

        public override string Name => "Localization";
    }
}
