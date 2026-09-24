using BepInEx.Logging;

namespace GK2Plus.Framework.Crafting
{
    /// <summary>
    /// GK2+ adapter boundary for Crafting.
    ///
    /// Keep direct game-internal access inside this layer rather than feature modules.
    /// </summary>
    internal sealed class GK2CraftingService : GK2ServiceBase
    {
        public GK2CraftingService(ManualLogSource logger)
            : base(logger)
        {
        }

        public override string Name => "Crafting";
    }
}
