using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Machine.cs와 동일한 패턴으로 동작하는 무한 Prisoner 큐.
///
/// 흐름:
///   1. 플레이어가 Area에 접근 → 보유한 Handcuff 전량 투입
///   2. processTimePerPrisoner(기본 1초)마다 Handcuff를 소비하여 Prisoner 1명 처리
///   3. 처리 시 moneyPerPrisoner(기본 4)개의 돈을 Plate에 드롭
///   4. 플레이어가 Plate에 접근 → 돈 전량 수령
/// </summary>
public class PrisonerQueue : MonoBehaviour
{
    [Header("큐 설정")]
    [Tooltip("초기 Prisoner Transform 목록 (앞→뒤 순서로 Inspector에서 드래그)")]
    [SerializeField] private List<Transform> prisoners = new List<Transform>();

    [Tooltip("무한 리필에 사용할 Prisoner 프리팹")]
    [SerializeField] private GameObject prisonerPrefab;

    [Header("처리 설정")]
    [Tooltip("Prisoner 1명당 소비하는 Handcuff 초기값")]
    [SerializeField] private int initialHandcuffsRequired = 2;

    [Tooltip("Prisoner 처리마다 Handcuff 요구량 증가분 (0 = 항상 고정)")]
    [SerializeField] private int handcuffsIncrement = 0;

    [Tooltip("Prisoner 1명 처리 간격 (초)")]
    [SerializeField] private float processTimePerPrisoner = 1f;

    [Tooltip("Prisoner 1명 처리 시 Plate에 드롭하는 돈 개수")]
    [SerializeField] private int moneyPerPrisoner = 4;

    [Header("Area 감지 (Handcuff 투입)")]
    [Tooltip("플레이어가 이 반경 안에 들어오면 Handcuff 전량 투입")]
    [SerializeField] private float areaRadius = 3f;

    [Tooltip("Area 기준 Transform (비워두면 자신)")]
    [SerializeField] private Transform areaCenter;

    [Header("Plate 감지 (돈 수령)")]
    [Tooltip("플레이어가 이 반경 안에 들어오면 돈 전량 수령")]
    [SerializeField] private float plateRadius = 2f;

    [Tooltip("Plate 기준 Transform (비워두면 자신)")]
    [SerializeField] private Transform plateCenter;

    [Header("돈 스택 비주얼")]
    [Tooltip("돈 오브젝트 프리팹")]
    [SerializeField] private GameObject moneyPrefab;

    [Tooltip("돈이 쌓일 위치 (Plate Transform 또는 그 자식)")]
    [SerializeField] private Transform moneyStackRoot;

    [Tooltip("돈 쌓임 간격")]
    [SerializeField] private float stackHeightOffset = 0.25f;

    [Tooltip("돈 드롭 시작 높이")]
    [SerializeField] private float dropStartHeight = 2f;

    [Tooltip("돈 드롭 애니메이션 시간 (초)")]
    [SerializeField] private float dropDuration = 0.3f;

    [Tooltip("Plate에 표시할 돈 비주얼 최대 개수")]
    [SerializeField] private int maxPlateVisuals = 20;

    [Header("감옥 설정")]
    [Tooltip("Prison 입구 경유지 (Prisoner (3)) — 비워두면 직행")]
    [SerializeField] private Transform prisonEntryPoint;

    [Tooltip("Prison 최종 배치 기준 위치 (Prisoner (2)) — 첫 번째 열/행의 원점")]
    [SerializeField] private Transform prisonRoot;

    [Tooltip("열(가로) 간격 — prisonRoot.right 방향으로 이 간격씩 배치")]
    [SerializeField] private float prisonSpacing = 1.5f;

    [Tooltip("행(세로) 간격 — prisonRoot.forward 방향으로 이 간격씩 배치 (음수 = z 감소)")]
    [SerializeField] private float prisonRowSpacing = -1.5f;

    [Tooltip("한 행에 배치되는 Prisoner 수 (열 수)")]
    [SerializeField] private int prisonColumns = 4;

    [Tooltip("Prison 최대 수용 인원 초기값")]
    [SerializeField] private int initialMaxCapacity = 20;

    [Tooltip("Prisoner 이동 속도 (유닛/초)")]
    [SerializeField] private float moveSpeed = 5f;

    // ── 런타임 상태 ──────────────────────────────────────────────────────
    private PlayerCarrier _carrier;
    private int  _handcuffQueue;
    private int  _moneyCount;
    private bool _isProcessingQueue;
    private int  _currentRequired;
    private int  _prisonCount;        // 현재 감옥에 수용된 인원 (슬롯 인덱스)
    private int  _maxPrisonCapacity;  // 현재 최대 수용 인원 (업그레이드로 증가)

    private readonly List<Vector3>    _slotPositions = new List<Vector3>();
    private readonly List<Transform>  _queue         = new List<Transform>();
    private readonly List<GameObject> _moneyVisuals  = new List<GameObject>();
    private readonly Queue<GameObject> _moneyPool    = new Queue<GameObject>();
    private Transform _spawnParent;

    private WaitForSeconds _waitProcess;
    private WaitForSeconds _waitCapacityCheck;

    // ── 초기화 ───────────────────────────────────────────────────────────
    private void Start()
    {
        var player = GameObject.Find("Player");
        if (player != null)
            _carrier = player.GetComponent<PlayerCarrier>();
        else
            Debug.LogError("[PrisonerQueue] 'Player'를 찾지 못했습니다.");

        _currentRequired    = initialHandcuffsRequired;
        _maxPrisonCapacity  = initialMaxCapacity;
        _waitProcess        = new WaitForSeconds(processTimePerPrisoner);
        _waitCapacityCheck  = new WaitForSeconds(0.5f);

        if (areaCenter     == null) areaCenter     = transform;
        if (plateCenter    == null) plateCenter    = transform;
        if (moneyStackRoot == null) moneyStackRoot = plateCenter;

        if (prisonRoot == null)
            Debug.LogError("[PrisonerQueue] prisonRoot가 비어 있습니다. Inspector에서 연결하세요.");
        if (prisonerPrefab == null)
            Debug.LogWarning("[PrisonerQueue] prisonerPrefab이 없습니다. 리필이 동작하지 않습니다.");

        foreach (var p in prisoners)
        {
            if (p == null) continue;
            _slotPositions.Add(p.position);
            _queue.Add(p);
        }

        _spawnParent = _queue.Count > 0 ? _queue[0].parent : null;

        if (_slotPositions.Count == 0)
            Debug.LogError("[PrisonerQueue] prisoners 목록이 비어 있습니다.");

        PrewarmMoneyPool();
    }

    private void PrewarmMoneyPool()
    {
        if (moneyPrefab == null || moneyStackRoot == null) return;
        for (int i = 0; i < maxPlateVisuals; i++)
        {
            var obj = Instantiate(moneyPrefab, moneyStackRoot);
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale   *= 1.5f;
            var col = obj.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            obj.SetActive(false);
            _moneyPool.Enqueue(obj);
        }
    }

    // ── 매 프레임 ────────────────────────────────────────────────────────
    private void Update()
    {
        if (_carrier == null) return;
        CheckAreaDelivery();
        CheckPlatePickup();
    }

    // ── Area: Handcuff 전량 투입 ──────────────────────────────────────────
    private void CheckAreaDelivery()
    {
        float dx = areaCenter.position.x - _carrier.transform.position.x;
        float dz = areaCenter.position.z - _carrier.transform.position.z;
        if (dx * dx + dz * dz <= areaRadius * areaRadius)
            OnPlayerEnterArea();
    }

    private void OnPlayerEnterArea()
    {
        if (_carrier.currentHandcuffs <= 0) return;

        int delivered = _carrier.DeliverAllHandcuffs();
        _handcuffQueue += delivered;

        if (!_isProcessingQueue)
            StartCoroutine(ProcessQueue());
    }

    // ── Plate: 돈 전량 수령 ───────────────────────────────────────────────
    private void CheckPlatePickup()
    {
        float dx = plateCenter.position.x - _carrier.transform.position.x;
        float dz = plateCenter.position.z - _carrier.transform.position.z;
        if (dx * dx + dz * dz <= plateRadius * plateRadius)
            PickupMoney();
    }

    private void PickupMoney()
    {
        if (_moneyCount <= 0) return;
        _carrier.AddMoney(_moneyCount);
        _moneyCount = 0;
        UpdateMoneyVisuals();
        AudioManager.Play("money_pickup");
    }

    public bool HasMoney => _moneyCount > 0;

    /// <summary>
    /// Guard 등 NPC가 Area에 수갑을 납입할 때 호출.
    /// 플레이어가 Area에 진입하는 것과 동일하게 처리.
    /// </summary>
    public void DepositHandcuffs(int amount)
    {
        if (amount <= 0) return;
        _handcuffQueue += amount;
        if (!_isProcessingQueue)
            StartCoroutine(ProcessQueue());
    }

    // ── 처리 큐 코루틴 (Machine.ProcessQueue 와 동일 구조) ────────────────
    private IEnumerator ProcessQueue()
    {
        _isProcessingQueue = true;

        while (_handcuffQueue >= _currentRequired && _queue.Count > 0)
        {
            // 감옥이 가득 차면 수용 공간이 생길 때까지 대기 (수갑 소비 없음)
            while (_prisonCount >= _maxPrisonCapacity)
                yield return _waitCapacityCheck;

            yield return _waitProcess;

            // 대기 후 재확인
            if (_handcuffQueue < _currentRequired || _queue.Count == 0) break;

            // Handcuff 소비
            _handcuffQueue -= _currentRequired;
            _currentRequired += handcuffsIncrement;

            // 돈 드롭 (즉시)
            _moneyCount += moneyPerPrisoner;
            for (int i = 0; i < moneyPerPrisoner; i++)
                SpawnMoneyVisual();
            AudioManager.Play("money_drop");

            // Prisoner 이동 + 큐 전진 동시 시작, 완료 대기 후 스폰
            var frontPrisoner = _queue[0];
            _queue.RemoveAt(0);
            int prisonSlot = _prisonCount++;

            AudioManager.Play("prisoner_in");

            var coroutines = new List<Coroutine>();
            coroutines.Add(StartCoroutine(MoveToPrisonViaEntry(frontPrisoner, GetPrisonDestination(prisonSlot))));
            for (int i = 0; i < _queue.Count; i++)
                coroutines.Add(StartCoroutine(MoveToTarget(_queue[i], _slotPositions[i])));

            foreach (var c in coroutines)
                yield return c;

            SpawnNextPrisoner();
        }

        _isProcessingQueue = false;
    }

    // ── 돈 비주얼 (Machine.SpawnHandcuffVisual 과 동일 구조) ─────────────
    private void SpawnMoneyVisual()
    {
        if (_moneyVisuals.Count >= maxPlateVisuals) return;

        int index = _moneyVisuals.Count;
        Vector3 targetLocal = Vector3.up * stackHeightOffset * index;

        GameObject obj;
        if (_moneyPool.Count > 0)
        {
            obj = _moneyPool.Dequeue();
            obj.SetActive(true);
        }
        else if (moneyPrefab != null && moneyStackRoot != null)
        {
            obj = Instantiate(moneyPrefab, moneyStackRoot);
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale   *= 1.5f;
            var col = obj.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
        else return;

        var rend = obj.GetComponent<Renderer>();
        if (rend != null) rend.enabled = true;

        _moneyVisuals.Add(obj);
        StartCoroutine(DropRoutine(obj, targetLocal));
    }

    private IEnumerator DropRoutine(GameObject obj, Vector3 targetLocal)
    {
        Vector3 startLocal = targetLocal + Vector3.up * dropStartHeight;
        obj.transform.localPosition = startLocal;

        float elapsed = 0f;
        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            if (obj == null || !obj.activeInHierarchy) yield break;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / dropDuration);
            obj.transform.localPosition = Vector3.Lerp(startLocal, targetLocal, t);
            yield return null;
        }
        if (obj != null && obj.activeInHierarchy)
            obj.transform.localPosition = targetLocal;
    }

    private void UpdateMoneyVisuals()
    {
        while (_moneyVisuals.Count > _moneyCount)
        {
            int last = _moneyVisuals.Count - 1;
            var obj = _moneyVisuals[last];
            _moneyVisuals.RemoveAt(last);
            obj.SetActive(false);
            _moneyPool.Enqueue(obj);
        }
    }

    // ── Prisoner 이동 ─────────────────────────────────────────────────────
    private void SpawnNextPrisoner()
    {
        if (prisonerPrefab == null || _slotPositions.Count == 0) return;
        Vector3 spawnPos = _slotPositions[_slotPositions.Count - 1];
        var go = Instantiate(prisonerPrefab, spawnPos, Quaternion.identity, _spawnParent);
        _queue.Add(go.transform);
    }

    private IEnumerator MoveToPrisonViaEntry(Transform prisoner, Vector3 finalDestination)
    {
        if (prisonEntryPoint != null)
            yield return StartCoroutine(MoveToTarget(prisoner, prisonEntryPoint.position));
        yield return StartCoroutine(MoveToTarget(prisoner, finalDestination));
    }

    private IEnumerator MoveToTarget(Transform t, Vector3 destination)
    {
        while (Vector3.Distance(t.position, destination) > 0.05f)
        {
            t.position = Vector3.MoveTowards(t.position, destination, moveSpeed * Time.deltaTime);
            Vector3 dir = (destination - t.position).normalized;
            if (dir.sqrMagnitude > 0.001f)
                t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
            yield return null;
        }
        t.position = destination;
    }

    /// <summary>Prison 내부 그리드 배치 좌표 계산.
    /// 슬롯 0~(prisonColumns-1) : 첫 번째 행 (prisonRoot 위치 기준 right 방향)
    /// 슬롯 prisonColumns~...    : 두 번째 행 (prisonRoot.forward * prisonRowSpacing 만큼 이동)
    /// prisonRowSpacing 를 음수로 설정하면 z값이 줄어드는 방향(=prison 안쪽)으로 배치됨.
    /// </summary>
    private Vector3 GetPrisonDestination(int slot)
    {
        int col = slot % prisonColumns;
        int row = slot / prisonColumns;
        return prisonRoot.position
             + prisonRoot.right   * (col * prisonSpacing)
             + prisonRoot.forward * (row * prisonRowSpacing);
    }

    /// <summary>PrisonUpgrade 에서 감옥 확장 시 호출.</summary>
    public void IncreasePrisonCapacity(int amount)
    {
        _maxPrisonCapacity += amount;
    }

    public int CurrentPrisonCount  => _prisonCount;
    public int MaxPrisonCapacity   => _maxPrisonCapacity;

    // ── 에디터 기즈모 ─────────────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 ac = areaCenter  != null ? areaCenter.position  : transform.position;
        Vector3 pc = plateCenter != null ? plateCenter.position : transform.position;

        Gizmos.color = new Color(0.8f, 0f, 1f, 0.15f);
        Gizmos.DrawSphere(ac, areaRadius);
        Gizmos.color = new Color(0.8f, 0f, 1f, 1f);
        Gizmos.DrawWireSphere(ac, areaRadius);

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.15f);
        Gizmos.DrawSphere(pc, plateRadius);
        Gizmos.color = new Color(1f, 0.6f, 0f, 1f);
        Gizmos.DrawWireSphere(pc, plateRadius);

        if (_slotPositions.Count > 0)
        {
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.5f);
            foreach (var pos in _slotPositions)
                Gizmos.DrawWireSphere(pos, 0.3f);
        }
    }
#endif
}
