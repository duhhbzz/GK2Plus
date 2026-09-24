using BepInEx.Logging;

namespace GK2Plus.Framework
{
    internal abstract class GK2ServiceBase : IGK2Service
    {
        protected GK2ServiceBase(ManualLogSource logger)
        {
            Logger = logger;
        }

        protected ManualLogSource Logger { get; }

        public abstract string Name { get; }

        public virtual void Initialize()
        {
            Logger.LogDebug($"Initializing framework service: {Name}");
        }

        public virtual void Shutdown()
        {
            Logger.LogDebug($"Shutting down framework service: {Name}");
        }
    }
}
