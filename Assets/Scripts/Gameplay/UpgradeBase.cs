using TMPro;
using UnityEngine;

/// <summary>
/// 모든 업그레이드 발판의 공통 베이스 클래스.
///
/// 공통 동작
/// - 플레이어가 돈을 취득하면 발판이 표시됨 (초기 숨김)
/// - 플레이어가 반경 내에 있으면 0.1초당 1개씩 돈을 순차 차감
/// - 비용 텍스트가 실시간으로 감소 표시
/// - 초록 프로그레스 바가 아래서부터 위로 채워짐
/// - 전액 납부 완료 → OnUpgradeApplied() 호출
///   - 1회성 업그레이드: 오브젝트 비활성
///   - 반복 가능 업그레이드: GetNextCost() 반환값으로 비용 리셋
/// </summary>
public abstract class UpgradeBase : MonoBehaviour
{
    // ── 공통 참조 ────────────────────────────────────────────────────────
    [Header("공통 참조")]
    [Tooltip("비용을 차감할 PlayerCarrier")]
    [SerializeField] protected PlayerCarrier playerCarrier;

    // ── 비용 ─────────────────────────────────────────────────────────────
    [Header("비용")]
    [Tooltip("업그레이드 비용 (반복 가능한 경우 첫 번째 라운드 비용)")]
    [SerializeField] protected int cost = 20;

    // ── 감지 설정 ─────────────────────────────────────────────────────────
    [Header("감지 설정")]
    [Tooltip("플레이어 감지 반경 (XZ 평면 기준)")]
    [SerializeField] protected float interactRadius = 2f;

    [Tooltip("감지 중심 Transform (null 이면 이 오브젝트 위치 사용)")]
    [SerializeField] protected Transform interactCenter;

    // ── 비주얼 ───────────────────────────────────────────────────────────
    [Header("비주얼")]
    [Tooltip("표시/숨김을 제어할 루트 Transform (null 이면 이 오브젝트 하위 전체)")]
    [SerializeField] protected Transform visualRoot;

    [Tooltip("남은 비용을 표시할 TMP 3D 텍스트")]
    [SerializeField] private TMP_Text costText;

    [Tooltip("아래서부터 채워지는 초록 바 오브젝트 — 피벗이 가운데여도 자동 보정됨")]
    [SerializeField] private Transform progressBar;

    // ── 내부 ─────────────────────────────────────────────────────────────
    private const float PAY_INTERVAL = 0.1f;

    protected Renderer[] _renderers;
    protected bool _isVisible;

    private int   _roundCost;      // 이번 라운드 총 비용 (프로그레스 바 기준)
    private int   _remainingCost;  // 아직 납부해야 할 금액
    private float _payTimer;

    // 프로그레스 바 bottom-fill 계산용 캐시
    private Vector3 _barInitLocalPos;
    private float   _barFullScaleY;

    // ─────────────────────────────────────────────────────────────────────
    protected virtual void Start()
    {
        Transform root = visualRoot != null ? visualRoot : transform;
        _renderers = root.GetComponentsInChildren<Renderer>(true);

        _roundCost     = cost;
        _remainingCost = cost;

        if (progressBar != null)
        {
            _barInitLocalPos = progressBar.localPosition;
            _barFullScaleY   = progressBar.localScale.y;
            ApplyBarProgress(0f);
        }

        SetVisible(false);
        RefreshUI();
    }

    protected virtual void Update()
    {
        if (playerCarrier == null) return;

        // 돈을 취득한 순간부터 표시
        if (!_isVisible && playerCarrier.currentMoney > 0)
            SetVisible(true);

        if (!_isVisible) return;

        // XZ 평면 거리 판정
        Vector3 center = interactCenter != null ? interactCenter.position : transform.position;
        Vector3 delta  = playerCarrier.transform.position - center;
        delta.y        = 0f;
        bool inRange   = delta.magnitude <= interactRadius;

        if (inRange && playerCarrier.currentMoney > 0 && _remainingCost > 0)
        {
            _payTimer += Time.deltaTime;

            // 0.1초마다 1원씩 순차 차감
            while (_payTimer >= PAY_INTERVAL
                   && _remainingCost > 0
                   && playerCarrier.currentMoney > 0)
            {
                _payTimer -= PAY_INTERVAL;
                playerCarrier.ConsumeMoney(1);
                _remainingCost--;
                RefreshUI();
                AudioManager.Play("upgrade_tick");
            }

            // 전액 납부 완료
            if (_remainingCost <= 0)
                CompletePurchase();
        }
        else
        {
            _payTimer = 0f; // 범위 밖으로 나가면 타이머 초기화
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    private void CompletePurchase()
    {
        AudioManager.Play("upgrade_complete");
        OnUpgradeApplied();

        int nextCost = GetNextCost(cost);
        if (nextCost <= 0)
        {
            // 1회성: 비활성
            SetVisible(false);
            gameObject.SetActive(false);
        }
        else
        {
            // 반복 가능: 비용 리셋 후 계속
            cost           = nextCost;
            _roundCost     = nextCost;
            _remainingCost = nextCost;
            _payTimer      = 0f;
            ApplyBarProgress(0f);
            RefreshUI();
        }
    }

    /// <summary>전액 납부 완료 시 호출. 서브클래스에서 실제 업그레이드 적용.</summary>
    protected abstract void OnUpgradeApplied();

    /// <summary>
    /// 다음 라운드 비용 반환.
    /// 0 이하 → 1회성 (비활성), 양수 → 반복 가능 (해당 비용으로 리셋).
    /// </summary>
    protected virtual int GetNextCost(int currentCost) => -1;

    // ── UI 갱신 ───────────────────────────────────────────────────────────
    private void RefreshUI()
    {
        if (costText != null)
            costText.text = _remainingCost.ToString();

        if (progressBar != null && _roundCost > 0)
        {
            float progress = 1f - (float)_remainingCost / _roundCost;
            ApplyBarProgress(progress);
        }
    }

    /// <summary>
    /// progress: 0 = 빈 상태, 1 = 가득 찬 상태.
    /// 피벗이 가운데여도 바닥 끝이 고정되도록 위치를 보정.
    /// </summary>
    private void ApplyBarProgress(float progress)
    {
        if (progressBar == null) return;

        float height = _barFullScaleY * Mathf.Clamp01(progress);

        Vector3 scale = progressBar.localScale;
        scale.y = Mathf.Max(0.0001f, height);
        progressBar.localScale = scale;

        // 바닥 끝 고정: bottomEdge = initPos.y - fullHeight/2
        float   bottomEdge = _barInitLocalPos.y - _barFullScaleY * 0.5f;
        Vector3 pos        = progressBar.localPosition;
        pos.y              = bottomEdge + height * 0.5f;
        progressBar.localPosition = pos;
    }

    // ── 표시/숨김 ─────────────────────────────────────────────────────────
    protected void SetVisible(bool visible)
    {
        _isVisible = visible;

        if (_renderers != null)
            foreach (var r in _renderers)
                r.enabled = visible;

        if (costText    != null) costText.enabled = visible;
        if (progressBar != null) progressBar.gameObject.SetActive(visible);
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        Vector3 center = interactCenter != null ? interactCenter.position : transform.position;
        Gizmos.color = new Color(0f, 1f, 0.8f, 0.5f);
        Gizmos.DrawWireSphere(center, interactRadius);
        Gizmos.color = new Color(0f, 1f, 0.8f, 1f);
        Gizmos.DrawWireSphere(center, 0.08f);
    }
#endif
}
