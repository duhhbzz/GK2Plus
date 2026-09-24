using BepInEx.Logging;

namespace GK2Plus.Framework.Zombies
{
    /// <summary>
    /// GK2+ adapter boundary for Zombies.
    ///
    /// Keep direct game-internal access inside this layer rather than feature modules.
    /// </summary>
    internal sealed class GK2ZombieService : GK2ServiceBase
    {
        public GK2ZombieService(ManualLogSource logger)
            : base(logger)
        {
        }

        public override string Name => "Zombies";
    }
}
