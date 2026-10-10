using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // 還沒有美術素材，表情先用嘴巴大小、顏色和晃動表示，成長階段用大小表示
    public class MonsterView : MonoBehaviour
    {
        private static readonly Vector3 MouthClosed = new(1f, 0.25f, 1f);

        [SerializeField] private Image _body;
        [SerializeField] private RectTransform _mouth;
        [SerializeField] private Color _happyColor = new(0.55f, 0.9f, 0.45f);
        [SerializeField] private Color _spitColor = new(0.95f, 0.55f, 0.5f);

        // 依 MonsterStage 順序：寶寶、小孩、大人
        [SerializeField] private float[] _stageScales = { 0.7f, 0.9f, 1.1f };

        private Color _normalColor;
        private float _baseScale = 1f;

        public MonsterStage Stage { get; private set; }
        public Vector3 MouthPosition => _mouth.position;

        private void Awake()
        {
            _normalColor = _body.color;
            _mouth.localScale = MouthClosed;
        }

        // 判定範圍用整隻怪獸而不是只有嘴巴，小朋友比較容易放中
        public bool Contains(Vector2 screenPoint, Camera eventCamera) =>
            RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, screenPoint, eventCamera);

        public void SetStage(MonsterStage stage)
        {
            Stage = stage;
            _baseScale = _stageScales[(int)stage];
            transform.localScale = Vector3.one * _baseScale;
        }

        public async Awaitable GrowTo(MonsterStage stage)
        {
            float from = _baseScale;
            float to = _stageScales[(int)stage];
            Stage = stage;
            await Tween.Run(0.8f, t =>
            {
                // 長大時多彈幾下，讓小朋友注意到
                float bounce = 0.15f * Mathf.Sin(t * Mathf.PI * 4) * (1 - t);
                transform.localScale = Vector3.one * (Mathf.Lerp(from, to, Tween.EaseOut(t)) + bounce);
            }, destroyCancellationToken);
            _baseScale = to;
            transform.localScale = Vector3.one * _baseScale;
        }

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
                transform.localScale = Vector3.one * (_baseScale * (1 + 0.1f * Mathf.Sin(t * Mathf.PI)));
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
