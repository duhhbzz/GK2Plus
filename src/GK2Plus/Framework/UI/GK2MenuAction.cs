using System;

namespace GK2Plus.Framework.UI
{
    /// <summary>
    /// Small registration model used by feature modules to expose actions in
    /// the GK2+ menu without putting feature logic inside ModMenuController.
    /// </summary>
    internal sealed class GK2MenuAction
    {
        public GK2MenuAction(
            string id,
            string tab,
            string label,
            Action execute,
            Func<bool> isAvailable = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Tab = tab ?? throw new ArgumentNullException(nameof(tab));
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Execute = execute ?? throw new ArgumentNullException(nameof(execute));
            IsAvailable = isAvailable;
        }

        public string Id { get; }

        public string Tab { get; }

        public string Label { get; }

        public Action Execute { get; }

        public Func<bool> IsAvailable { get; }

        public bool CanExecute()
        {
            return IsAvailable == null || IsAvailable();
        }
    }
}
