using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // 還沒有美術素材，表情先用嘴巴大小、顏色和晃動表示
    public class MonsterView : MonoBehaviour
    {
        private static readonly Vector3 MouthClosed = new(1f, 0.25f, 1f);

        [SerializeField] private Image _body;
        [SerializeField] private RectTransform _mouth;
        [SerializeField] private Color _happyColor = new(0.55f, 0.9f, 0.45f);
        [SerializeField] private Color _spitColor = new(0.95f, 0.55f, 0.5f);

        private Color _normalColor;

        public Vector3 MouthPosition => _mouth.position;

        private void Awake()
        {
            _normalColor = _body.color;
            _mouth.localScale = MouthClosed;
        }

        // 判定範圍用整隻怪獸而不是只有嘴巴，小朋友比較容易放中
        public bool Contains(Vector2 screenPoint, Camera eventCamera) =>
            RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, screenPoint, eventCamera);

        public void SetMouthOpen(bool open)
        {
            _mouth.localScale = open ? Vector3.one : MouthClosed;
        }

        public async Awaitable PlayEat()
        {
            _body.color = _happyColor;
            await Tween.Run(0.4f, t =>
            {
                // 嚼兩下
                _mouth.localScale = Vector3.Lerp(MouthClosed, Vector3.one, Mathf.Abs(Mathf.Cos(t * Mathf.PI * 2)));
                transform.localScale = Vector3.one * (1 + 0.1f * Mathf.Sin(t * Mathf.PI));
            }, destroyCancellationToken);
            SetMouthOpen(false);
            _body.color = _normalColor;
        }

        public async Awaitable PlaySpit()
        {
            _body.color = _spitColor;
            Vector3 origin = transform.localPosition;
            await Tween.Run(0.4f, t =>
            {
                transform.localPosition = origin + Vector3.right * (20 * Mathf.Sin(t * Mathf.PI * 6) * (1 - t));
            }, destroyCancellationToken);
            SetMouthOpen(false);
            _body.color = _normalColor;
        }
    }
}
