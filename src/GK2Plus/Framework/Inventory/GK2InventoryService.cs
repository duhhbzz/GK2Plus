using BepInEx.Logging;

namespace GK2Plus.Framework.Inventory
{
    /// <summary>
    /// GK2+ adapter boundary for Inventory.
    ///
    /// Keep direct game-internal access inside this layer rather than feature modules.
    /// </summary>
    internal sealed class GK2InventoryService : GK2ServiceBase
    {
        public GK2InventoryService(ManualLogSource logger)
            : base(logger)
        {
        }

        public override string Name => "Inventory";
    }
}
