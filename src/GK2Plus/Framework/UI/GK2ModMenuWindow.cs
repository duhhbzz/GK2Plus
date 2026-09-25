using LazyBearTechnology;

namespace GK2Plus.Framework.UI
{
    /// <summary>
    /// Native LazyWindow host for the GK2+ menu shell.
    ///
    /// ModMenuController still owns the visual tree so the existing polished
    /// shell can be reused. This component gives that shell GK2's native
    /// modal/window-stack lifecycle and Back/Escape behavior.
    /// </summary>
    internal sealed class GK2ModMenuWindowData : LazyWidgetDataBase
    {
    }

    internal sealed class GK2ModMenuWindow : LazyWindow<GK2ModMenuWindowData>
    {
        public override void Init()
        {
            isModalWindow = true;
            base.Init();
        }

        protected override bool OnPressedBack()
        {
            Close();
            return true;
        }

        protected override void TestDraw()
        {
            Open(new GK2ModMenuWindowData());
        }
    }
}
