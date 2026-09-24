using BepInEx.Logging;

namespace GK2Plus.Framework.Farming
{
    /// <summary>
    /// GK2+ adapter boundary for Farming.
    ///
    /// Keep direct game-internal access inside this layer rather than feature modules.
    /// </summary>
    internal sealed class GK2FarmingService : GK2ServiceBase
    {
        public GK2FarmingService(ManualLogSource logger)
            : base(logger)
        {
        }

        public override string Name => "Farming";
    }
}
