using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackPermission.UI
{
    /// <summary>
    /// One clickable row of the access panel. Hovering highlights the row and shows its caption
    /// in the wheel; a click runs the row's action while the wheel stays open.
    /// </summary>
    internal sealed class PermissionRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private const float HoverFillAlpha = 0.35f;
        private const float HoverScale = 1.04f;

        private PermissionPanel _panel;
        private Image _fill;
        private Action _onActivate;

        /// <summary>Text shown in the wheel's caption while the row is hovered.</summary>
        public string Caption { get; private set; }

        public void Initialize(PermissionPanel panel, Image fill, string caption, Action onActivate)
        {
            _panel = panel;
            _fill = fill;
            Caption = caption;
            _onActivate = onActivate;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _panel?.OnRowClicked(this);
            }
        }

        public void Activate()
        {
            _onActivate?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetHighlighted(true);
            _panel?.OnRowEntered(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Leave();
        }

        private void OnDisable()
        {
            Leave();
        }

        private void Leave()
        {
            SetHighlighted(false);
            _panel?.OnRowExited(this);
        }

        private void SetHighlighted(bool highlighted)
        {
            if (_fill != null)
            {
                _fill.color = HudStyle.WithAlpha(_fill.color, highlighted ? HoverFillAlpha : 0f);
            }
            transform.localScale = highlighted ? Vector3.one * HoverScale : Vector3.one;
        }
    }
}
