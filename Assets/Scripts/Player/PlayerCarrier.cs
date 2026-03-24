using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 수갑·돈을 보유하고 스택으로 표시하는 컴포넌트.
///
/// 변수명 규칙
/// - handcuff* / money* 접두사로 두 자원을 구분
/// - current* : 현재 보유량,  max* : 최대 보유량
/// - *Visuals : 표시용 GameObject 목록,  *TemplateRenderer : 초기 숨김 대상
/// </summary>
public class PlayerCarrier : MonoBehaviour
{
    // ── 수갑 ─────────────────────────────────────────────────────────────
    [Header("수갑 스택 비주얼")]
    [Tooltip("수갑 프리팹 (또는 씬 내 템플릿 오브젝트)")]
    public GameObject handcuffPrefab;

    [Tooltip("수갑이 쌓일 기준 위치")]
    public Transform handcuffRoot;

    [Tooltip("수갑 쌓임 간격 (Y 방향)")]
    public float handcuffHeightOffset = 0.25f;

    [Tooltip("비주얼 최대 표시 개수 (실제 보유량과 분리)")]
    public int maxHandcuffVisuals = 10;

    [Header("수갑 보유량")]
    public int currentHandcuffs = 0;
    public int maxHandcuffs     = 30;

    // ── 돈 ───────────────────────────────────────────────────────────────
    [Header("돈 스택 비주얼")]
    [Tooltip("돈 프리팹 (또는 씬 내 템플릿 오브젝트, 비워두면 비주얼 없음)")]
    public GameObject moneyPrefab;

    [Tooltip("돈이 쌓일 기준 위치")]
    public Transform moneyRoot;

    [Tooltip("돈 쌓임 간격 (Y 방향)")]
    public float moneyHeightOffset = 0.25f;

    [Tooltip("비주얼 최대 표시 개수 (실제 보유량과 분리)")]
    public int maxMoneyVisuals = 10;

    [Header("돈 보유량")]
    public int currentMoney = 0;
    public int maxMoney     = 999;

    // ── 내부 ─────────────────────────────────────────────────────────────
    private Renderer _handcuffTemplateRenderer;
    private Renderer _moneyTemplateRenderer;

    private readonly List<GameObject> _handcuffVisuals = new List<GameObject>();
    private readonly List<GameObject> _moneyVisuals    = new List<GameObject>();

    private void Awake()
    {
        if (handcuffPrefab != null)
        {
            _handcuffTemplateRenderer = handcuffPrefab.GetComponent<Renderer>();
            if (_handcuffTemplateRenderer != null) _handcuffTemplateRenderer.enabled = false;
        }
        if (moneyPrefab != null)
        {
            _moneyTemplateRenderer = moneyPrefab.GetComponent<Renderer>();
            if (_moneyTemplateRenderer != null) _moneyTemplateRenderer.enabled = false;
        }
    }

    // ── 수갑 API ─────────────────────────────────────────────────────────
    public void AddHandcuffs(int amount)
    {
        if (amount <= 0) return;
        currentHandcuffs = Mathf.Min(currentHandcuffs + amount, maxHandcuffs);
        RefreshHandcuffVisuals();
    }

    public void ConsumeHandcuffs(int amount)
    {
        currentHandcuffs = Mathf.Max(0, currentHandcuffs - amount);
        RefreshHandcuffVisuals();
    }

    /// <summary>수갑 전량 소비 후 개수를 반환.</summary>
    public int DeliverAllHandcuffs()
    {
        int amount = currentHandcuffs;
        currentHandcuffs = 0;
        RefreshHandcuffVisuals();
        return amount;
    }

    // ── 돈 API ───────────────────────────────────────────────────────────
    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        currentMoney = Mathf.Min(currentMoney + amount, maxMoney);
        RefreshMoneyVisuals();
    }

    public void ConsumeMoney(int amount)
    {
        currentMoney = Mathf.Max(0, currentMoney - amount);
        RefreshMoneyVisuals();
    }

    /// <summary>돈 전량 소비 후 개수를 반환.</summary>
    public int DeliverAllMoney()
    {
        int amount = currentMoney;
        currentMoney = 0;
        RefreshMoneyVisuals();
        return amount;
    }

    // ── 비주얼 갱신 ───────────────────────────────────────────────────────
    private void RefreshHandcuffVisuals() => SyncStackVisuals(
        handcuffPrefab, handcuffRoot, handcuffHeightOffset,
        Mathf.Min(currentHandcuffs, maxHandcuffVisuals),
        _handcuffVisuals);

    private void RefreshMoneyVisuals() => SyncStackVisuals(
        moneyPrefab, moneyRoot, moneyHeightOffset,
        Mathf.Min(currentMoney, maxMoneyVisuals),
        _moneyVisuals);

    /// <summary>
    /// visuals 리스트를 targetCount 개수로 동기화.
    /// prefab 을 복제하거나 Destroy 하여 개수를 맞춘다.
    /// </summary>
    private static void SyncStackVisuals(
        GameObject prefab, Transform root, float heightOffset,
        int targetCount, List<GameObject> visuals)
    {
        if (prefab == null || root == null) return;

        while (visuals.Count < targetCount)
        {
            int index = visuals.Count;
            var obj   = Instantiate(prefab, root);
            obj.transform.localPosition = Vector3.up * heightOffset * index;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale    = prefab.transform.localScale * 1.5f;

            var rend = obj.GetComponent<Renderer>();
            if (rend != null) rend.enabled = true;

            var col = obj.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            visuals.Add(obj);
        }

        while (visuals.Count > targetCount)
        {
            int last = visuals.Count - 1;
            Destroy(visuals[last]);
            visuals.RemoveAt(last);
        }
    }
}
