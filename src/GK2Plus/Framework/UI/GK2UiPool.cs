using System;
using System.Collections.Generic;

namespace GK2Plus.Framework.UI
{
    /// <summary>
    /// Tiny allocation-free view pool for reusable GK2+ UI controls.
    /// Begin -> Rent as many as needed -> End to hide unused controls.
    /// </summary>
    internal sealed class GK2UiPool<T>
        where T : class
    {
        private readonly Func<T> _factory;
        private readonly Action<T, bool> _setActive;
        private readonly List<T> _items =
            new List<T>();

        private int _cursor;

        public GK2UiPool(
            Func<T> factory,
            Action<T, bool> setActive)
        {
            _factory =
                factory ??
                throw new ArgumentNullException(
                    nameof(factory));

            _setActive =
                setActive ??
                throw new ArgumentNullException(
                    nameof(setActive));
        }

        public int Count =>
            _items.Count;

        public void Begin()
        {
            _cursor = 0;
        }

        public T Rent()
        {
            T item;

            if (_cursor < _items.Count)
            {
                item =
                    _items[_cursor];
            }
            else
            {
                item =
                    _factory();

                _items.Add(
                    item);
            }

            _cursor++;

            _setActive(
                item,
                true);

            return item;
        }

        public void End()
        {
            for (int i = _cursor; i < _items.Count; i++)
            {
                _setActive(
                    _items[i],
                    false);
            }
        }

        public void ForEach(
            Action<T> action)
        {
            if (action == null)
            {
                return;
            }

            foreach (T item in _items)
            {
                action(
                    item);
            }
        }
    }
}
