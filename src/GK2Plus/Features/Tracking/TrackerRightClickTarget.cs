using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GK2Plus.Features.Tracking
{
    internal sealed class TrackerRightClickTarget :
        MonoBehaviour,
        IPointerClickHandler
    {
        private Action _toggleAction;

        internal void Bind(
            Action toggleAction)
        {
            _toggleAction = toggleAction;
        }

        public void OnPointerClick(
            PointerEventData eventData)
        {
            if (eventData == null ||
                eventData.button !=
                    PointerEventData.InputButton.Right)
            {
                return;
            }

            _toggleAction?.Invoke();
            eventData.Use();
        }

        private void OnDestroy()
        {
            _toggleAction = null;
        }
    }
}
