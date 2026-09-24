namespace GK2Plus.Framework
{
    /// <summary>
    /// Small lifecycle contract for GK2+ game/framework adapters.
    ///
    /// Services should hide game internals from feature modules whenever practical.
    /// </summary>
    internal interface IGK2Service
    {
        string Name { get; }

        void Initialize();

        void Shutdown();
    }
}
