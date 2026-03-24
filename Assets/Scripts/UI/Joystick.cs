using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 가상 조이스틱 — 모바일 터치 및 에디터 마우스 클릭·드래그 모두 지원.
///
/// 구조 (Inspector에서 연결)
///   JoystickPanel
///     └ Background  (원형 반투명 이미지 — RectTransform 기준점)
///         └ Handle  (작은 원형 이미지)
///
/// PlayerController 에서 InputDirection 프로퍼티를 읽어 이동에 사용.
/// </summary>
public class Joystick : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("참조")]
    [Tooltip("조이스틱 배경 원 RectTransform")]
    [SerializeField] private RectTransform background;

    [Tooltip("조이스틱 핸들(내부 원) RectTransform")]
    [SerializeField] private RectTransform handle;

    [Header("설정")]
    [Tooltip("핸들이 이동할 수 있는 최대 범위 (0~1, 1 = background 반지름까지)")]
    [SerializeField, Range(0f, 1f)] private float handleRange = 0.9f;

    [Tooltip("이 값 이하의 입력은 0으로 처리 (데드존)")]
    [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.1f;

    // ── 런타임 ──────────────────────────────────────────────────────────────
    private Canvas   _canvas;
    private Camera   _cam;
    private Vector2  _input = Vector2.zero;

    /// <summary>정규화된 입력 방향 (-1 ~ 1). 데드존 이하면 Vector2.zero.</summary>
    public Vector2 InputDirection => _input;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();

        // Screen Space - Camera 모드라면 카메라 필요
        if (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceCamera)
            _cam = _canvas.worldCamera;
    }

    private void Start()
    {
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }

    // ── EventSystem 콜백 (마우스·터치 공통) ──────────────────────────────────
    public void OnPointerDown(PointerEventData eventData) => MoveHandle(eventData);
    public void OnDrag(PointerEventData eventData)       => MoveHandle(eventData);

    public void OnPointerUp(PointerEventData eventData)
    {
        _input = Vector2.zero;
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void MoveHandle(PointerEventData eventData)
    {
        if (background == null || handle == null) return;

        // 스크린 좌표 → background 로컬 좌표 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, _cam, out Vector2 localPos);

        float radius = background.sizeDelta.x * 0.5f * handleRange;

        // 반지름 이내로 클램프
        Vector2 clamped = Vector2.ClampMagnitude(localPos, radius);
        handle.anchoredPosition = clamped;

        // 정규화 입력 계산 (-1 ~ 1)
        Vector2 normalized = clamped / radius;

        // 데드존 처리
        _input = normalized.magnitude < deadZone ? Vector2.zero : normalized;
    }
}
