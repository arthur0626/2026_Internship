using UnityEngine;

/// <summary>
/// 플레이어 주변의 미네랄을 자동으로 채굴하는 컴포넌트.
/// - 1초에 1개 채굴 시도
/// - 채굴 시점에 가장 가까운 미네랄을 1회 타격
/// - 플레이어는 최대 maxOre 개까지만 보유 가능
/// - 전체 채굴량 통계는 GameManager 싱글턴에 누적
/// </summary>
public class PlayerMiner : MonoBehaviour
{
    [Header("채굴 설정")]
    [Tooltip("채굴 속도 (초당 1번이면 1)")]
    public float mineInterval = 1f;

    [Tooltip("채굴 가능한 최대 거리")]
    public float mineRange = 3f;

    [Tooltip("미네랄이 위치한 레이어 마스크")]
    public LayerMask mineralLayer;

    [Header("보유량 설정")]
    [Tooltip("플레이어가 최대 몇 개까지 들 수 있는지")]
    public int maxOre = 5;

    [Tooltip("현재 들고 있는 광물 개수")]
    public int currentOre = 0;

    [Header("스택 비주얼")]
    [Tooltip("플레이어 자식으로 만든 'Mini Mineral' 오브젝트 (프리팹 템플릿)")]
    public GameObject miniMineral;         // Player/Mini Mineral

    [Tooltip("Mini Mineral 이 쌓일 기준 위치 (보통 Mini Mineral 의 Transform)")]
    public Transform miniMineralRoot;      // 보통 Mini Mineral 의 부모/자기 자신

    [Tooltip("위로 얼마나 간격을 두고 쌓을지")]
    public float stackHeightOffset = 0.2f;

    [Header("MAX 표시")]
    [Tooltip("Player/Canvas/MAX 오브젝트")]
    public GameObject maxTextObject;       // Player/Canvas/MAX

    float mineTimer = 0f;
    Renderer _templateRenderer;

    [Header("채굴 상태 시 색상 변화")]
    [Tooltip("색상 변화를 적용할 플레이어 본체 Renderer — 직접 지정하여 자식 스캔 비용 제거")]
    public Renderer playerBodyRenderer;
    public Color miningColor = Color.yellow;
    [Tooltip("채굴 색상이 유지되는 시간 (초)")]
    public float miningColorDuration = 0.25f;

    Renderer _renderer;
    Material _cachedMaterial;
    Color _originalColor;
    float _miningColorTimer = 0f;

    // 상태 캐시 — 변화가 없을 때 GPU 호출 방지
    bool _prevMiningColorActive = false;
    bool _prevIsMaxOre = false;

    // 생성된 Mini Mineral 들을 관리
    readonly System.Collections.Generic.List<GameObject> _stackVisuals =
        new System.Collections.Generic.List<GameObject>();

    void Awake()
    {
        // 씬에 배치된 miniMineral은 템플릿용이므로 렌더러를 숨김
        if (miniMineral != null)
        {
            _templateRenderer = miniMineral.GetComponent<Renderer>();
            if (_templateRenderer != null)
                _templateRenderer.enabled = false;
        }

        // Renderer를 Awake에서 한 번만 캐시.
        // playerBodyRenderer가 Inspector에서 지정되지 않은 경우에만 자동 탐색.
        // GetComponentInChildren 은 자식 전체를 스캔하므로 직접 지정이 권장됨.
        _renderer = playerBodyRenderer != null
            ? playerBodyRenderer
            : GetComponent<Renderer>();  // 자신에게만 GetComponent (자식 미포함)

        if (_renderer != null)
        {
            _cachedMaterial = _renderer.material;   // 인스턴스 1회만 생성
            _originalColor  = _cachedMaterial.color;
        }
    }

    void Update()
    {
        // 채굴 색상 타이머 카운트다운
        if (_miningColorTimer > 0f)
            _miningColorTimer -= Time.deltaTime;

        // 더 이상 들 수 없으면 채굴 안 함
        if (currentOre >= maxOre)
        {
            UpdateMiningVisual();
            UpdateMaxVisual();
            return;
        }

        mineTimer += Time.deltaTime;

        // 1초(또는 mineInterval)마다 채굴 시도
        if (mineTimer >= mineInterval)
        {
            mineTimer -= mineInterval;
            TryMineNearestMineral();
        }

        UpdateMiningVisual();
        UpdateMaxVisual();
    }

    /// <summary>
    /// 머신에 광물을 납입할 때 호출. currentOre를 0으로 초기화하고 스택 비주얼 제거.
    /// </summary>
    public void DeliverOre()
    {
        currentOre = 0;
        UpdateStackVisuals();
    }

    void TryMineNearestMineral()
    {
        // 주변 미네랄 탐색
        // mineralLayer가 설정되어 있지 않아도 동작하도록 처리
        // 트리거 콜라이더도 반드시 맞도록 QueryTriggerInteraction.Collide 사용
        Collider[] hits;
        if (mineralLayer.value != 0)
            hits = Physics.OverlapSphere(transform.position, mineRange, mineralLayer, QueryTriggerInteraction.Collide);
        else
            hits = Physics.OverlapSphere(transform.position, mineRange, ~0, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0)
            return;

        Mineral nearest = null;
        float minSqrDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit == null || !hit.gameObject.activeInHierarchy)
                continue;

            var node = hit.GetComponent<Mineral>();
            if (node == null)
                continue;

            float sqrDist = (hit.transform.position - transform.position).sqrMagnitude;
            if (sqrDist < minSqrDist)
            {
                minSqrDist = sqrDist;
                nearest = node;
            }
        }

        if (nearest == null)
            return;

        // 실제 채굴 처리 (광물 양 감소 및 리스폰은 Mineral 내부에서 담당)
        // 1초에 1개 채굴이므로 한 번 호출당 1만큼 채굴
        if (nearest.Mine())
        {
            AudioManager.Play("mine");

            // 플레이어 인벤토리에 1개 추가
            currentOre = Mathf.Min(currentOre + 1, maxOre);

            // 전역 통계는 GameManager에서 관리 (선택 사항)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddMinedOre(1);
            }

            // 채굴 색상 타이머 리셋
            _miningColorTimer = miningColorDuration;

            // Mini Mineral 스택 비주얼 갱신
            UpdateStackVisuals();
        }
    }

    void UpdateMiningVisual()
    {
        if (_cachedMaterial == null) return;

        // 상태가 바뀔 때만 material 업데이트 → 불필요한 GPU dirty 방지
        bool active = _miningColorTimer > 0f;
        if (active == _prevMiningColorActive) return;

        _prevMiningColorActive = active;
        _cachedMaterial.color  = active ? miningColor : _originalColor;
    }

    void UpdateMaxVisual()
    {
        if (maxTextObject == null) return;

        // 상태가 바뀔 때만 SetActive 호출 → 매 프레임 호출 비용 제거
        bool isMax = currentOre >= maxOre;
        if (isMax == _prevIsMaxOre) return;

        _prevIsMaxOre = isMax;
        maxTextObject.SetActive(isMax);
    }

    void UpdateStackVisuals()
    {
        if (miniMineral == null || miniMineralRoot == null)
            return;

        // Mini Mineral 루트 아래에 currentOre 개수만큼만 유지
        while (_stackVisuals.Count < currentOre)
        {
            int index = _stackVisuals.Count;
            Vector3 localOffset = Vector3.up * stackHeightOffset * index;

            GameObject obj = Object.Instantiate(miniMineral, miniMineralRoot);
            obj.transform.localPosition = localOffset;
            obj.transform.localRotation = Quaternion.identity;
            // 모든 클론이 템플릿과 동일한 로컬 스케일을 갖도록 명시적으로 고정
            obj.transform.localScale = miniMineral.transform.localScale;

            // 템플릿에서 꺼진 Renderer를 클론에서 다시 켬
            var rend = obj.GetComponent<Renderer>();
            if (rend != null) rend.enabled = true;

            _stackVisuals.Add(obj);
        }

        while (_stackVisuals.Count > currentOre)
        {
            int last = _stackVisuals.Count - 1;
            Object.Destroy(_stackVisuals[last]);
            _stackVisuals.RemoveAt(last);
        }
    }
}