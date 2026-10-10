using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game
{
    // 可拖曳的食物，只負責跟著手指移動與播放動畫，放到哪裡算對由 GameScreen 判斷
    public class FoodItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Text _label;

        private RectTransform _rect;
        private Vector2 _homePosition;
        private Vector3 _dragOffset;
        private bool _isDragging;

        public event Action<FoodItem> DragStarted;
        public event Action<FoodItem, Vector2> Released;

        public int Choice { get; private set; }
        public bool Draggable { get; set; }

        private RectTransform Rect => _rect ??= (RectTransform)transform;

        public void Setup(int choice, Vector2 homePosition)
        {
            Choice = choice;
            _label.text = choice.ToString();
            _homePosition = homePosition;
            Rect.anchoredPosition = homePosition;
            Rect.localScale = Vector3.one;
            gameObject.SetActive(true);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!Draggable)
                return;

            _isDragging = true;
            // 拖曳中的食物要畫在其他食物上面
            Rect.SetAsLastSibling();
            if (TryGetWorldPoint(eventData, out var point))
                _dragOffset = Rect.position - point;
            DragStarted?.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isDragging && TryGetWorldPoint(eventData, out var point))
                Rect.position = point + _dragOffset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging)
                return;
            _isDragging = false;
            Released?.Invoke(this, eventData.position);
        }

        public async Awaitable ReturnHome()
        {
            Vector2 from = Rect.anchoredPosition;
            await Tween.Run(0.25f, t => Rect.anchoredPosition = Vector2.Lerp(from, _homePosition, Tween.EaseOut(t)),
                destroyCancellationToken);
        }

        public async Awaitable FlyInto(Vector3 worldTarget)
        {
            Vector3 from = Rect.position;
            await Tween.Run(0.25f, t =>
            {
                Rect.position = Vector3.Lerp(from, worldTarget, t);
                Rect.localScale = Vector3.one * (1 - t);
            }, destroyCancellationToken);
            gameObject.SetActive(false);
        }

        private bool TryGetWorldPoint(PointerEventData eventData, out Vector3 point) =>
            RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)Rect.parent, eventData.position,
                eventData.pressEventCamera, out point);
    }
}
