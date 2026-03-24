using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 광물을 받아 수갑을 생성하는 머신.
/// - Area 감지 (광물 납입) 및 Plate 감지 (수갑 픽업) 모두 이 스크립트에서 처리.
/// - MachineArea / MachinePlate 스크립트 불필요 (있어도 무방).
/// </summary>
public class Machine : MonoBehaviour
{
    [Header("처리 설정")]
    [Tooltip("광물 1개당 생성되는 수갑 수")]
    [SerializeField] private int handcuffsPerMineral = 2;

    [Tooltip("광물 1개 처리에 걸리는 시간 (초)")]
    [SerializeField] private float processTimePerMineral = 1f;

    [Header("감지 반경")]
    [Tooltip("플레이어가 이 반경 안에 들어오면 광물을 납입")]
    [SerializeField] private float areaRadius = 3f;

    [Tooltip("플레이어가 이 반경 안에 들어오면 수갑을 수령")]
    [SerializeField] private float plateRadius = 2f;

    [Tooltip("Area 기준 Transform (없으면 자신 사용)")]
    [SerializeField] private Transform areaCenter;

    [Tooltip("Plate 기준 Transform (없으면 자신 사용)")]
    [FormerlySerializedAs("handcuffPlate")]
    [SerializeField] private Transform plateCenter;

    [Header("수갑 스택 비주얼")]
    [Tooltip("수갑 프리팹")]
    [SerializeField] private GameObject handcuffPrefab;

    [Tooltip("수갑이 쌓일 위치 (Plate Transform)")]
    [SerializeField] private Transform handcuffStackRoot;

    [Tooltip("수갑 쌓임 간격")]
    [SerializeField] private float stackHeightOffset = 0.25f;

    [Tooltip("수갑 드롭 시작 높이")]
    [SerializeField] private float dropStartHeight = 2f;

    [Tooltip("수갑 드롭 애니메이션 시간 (초)")]
    [SerializeField] private float dropDuration = 0.3f;

    [Tooltip("Plate에 표시할 수갑 비주얼 최대 개수")]
    [SerializeField] private int maxPlateVisuals = 12;

    // ── 런타임 상태 ──────────────────────────────────────────────────────
    private int  _mineralQueue;
    private int  _handcuffCount;
    private bool _isProcessing;

    private readonly List<GameObject>  _handcuffVisuals = new List<GameObject>();
    private readonly Queue<GameObject> _handcuffPool    = new Queue<GameObject>();

    private WaitForSeconds _waitProcess;

    private PlayerMiner   _miner;
    private PlayerCarrier _carrier;

    // ── 초기화 ───────────────────────────────────────────────────────────
    private void Start()
    {
        var player = GameObject.Find("Player");
        if (player != null)
        {
            _miner   = player.GetComponent<PlayerMiner>();
            _carrier = player.GetComponent<PlayerCarrier>();
        }
        else
        {
            Debug.LogError("[Machine] 'Player' 오브젝트를 찾지 못했습니다.");
        }

        // areaCenter / plateCenter 미지정 시 자동 탐색
        // 자식 → 부모의 자식(형제) 순서로 탐색
        if (areaCenter == null)
            areaCenter = FindInSelfOrSiblings("Area") ?? transform;

        if (plateCenter == null)
        {
            plateCenter = FindInSelfOrSiblings("Plate")
                       ?? FindInSelfOrSiblings("Plane")
                       ?? transform;
        }

        if (handcuffStackRoot == null)
            handcuffStackRoot = plateCenter;

        _waitProcess = new WaitForSeconds(processTimePerMineral);
        PrewarmPool();
    }

    private void PrewarmPool()
    {
        if (handcuffPrefab == null || handcuffStackRoot == null) return;
        for (int i = 0; i < maxPlateVisuals; i++)
        {
            var obj = Instantiate(handcuffPrefab, handcuffStackRoot);
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale   *= 1.5f;
            var col = obj.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            obj.SetActive(false);
            _handcuffPool.Enqueue(obj);
        }
    }

    // ── 매 프레임 감지 ───────────────────────────────────────────────────
    private void Update()
    {
        if (_miner == null && _carrier == null) return;

        if (_miner != null)   CheckAreaDelivery();
        if (_carrier != null) CheckPlatePickup();
    }

    // Area 반경 안에 있으면 광물 납입 (내부 가드: currentOre <= 0 이면 무시)
    private void CheckAreaDelivery()
    {
        float dx = areaCenter.position.x - _miner.transform.position.x;
        float dz = areaCenter.position.z - _miner.transform.position.z;
        if (dx * dx + dz * dz <= areaRadius * areaRadius)
            OnPlayerEnter(_miner);
    }

    // Plate 반경 안에 있으면 수갑 수령
    private void CheckPlatePickup()
    {
        float dx = plateCenter.position.x - _carrier.transform.position.x;
        float dz = plateCenter.position.z - _carrier.transform.position.z;
        if (dx * dx + dz * dz <= plateRadius * plateRadius)
            PickupHandcuffs(_carrier);
    }

    // ── 유틸: 자식 → 형제(부모의 자식) 순서로 Transform 탐색 ─────────────
    private Transform FindInSelfOrSiblings(string name)
    {
        // 직접 자식에서 먼저 찾기
        var found = transform.Find(name);
        if (found != null) return found;

        // 부모의 자식(형제)에서 찾기
        if (transform.parent != null)
        {
            found = transform.parent.Find(name);
            if (found != null) return found;
        }
        return null;
    }

    // ── 공개 API (MachineArea / MachinePlate 에서도 호출 가능) ───────────

    public void OnPlayerEnter(PlayerMiner miner)
    {
        if (miner == null || miner.currentOre <= 0) return;
        int delivered = miner.currentOre;
        miner.DeliverOre();
        EnqueueMinerals(delivered);
        AudioManager.Play("deposit");
    }

    public void PickupHandcuffs(PlayerCarrier carrier)
    {
        if (carrier == null || _handcuffCount <= 0) return;
        carrier.AddHandcuffs(_handcuffCount);
        _handcuffCount = 0;
        UpdateHandcuffVisuals();
        AudioManager.Play("handcuff_pickup");
    }

    public bool HasHandcuffs => _handcuffCount > 0;

    /// <summary>
    /// MinerGuard 등 NPC가 채굴한 광물을 직접 납입할 때 호출.
    /// 플레이어가 Area 에 진입하는 것과 동일하게 처리.
    /// </summary>
    public void DeliverOre(int amount)
    {
        if (amount <= 0) return;
        EnqueueMinerals(amount);
    }

    /// <summary>
    /// Guard 등 NPC가 Plate의 수갑을 전량 가져갈 때 호출.
    /// 현재 수갑 수를 반환하고 내부 카운트 및 비주얼을 초기화.
    /// </summary>
    public int TakeAllHandcuffs()
    {
        if (_handcuffCount <= 0) return 0;
        int taken = _handcuffCount;
        _handcuffCount = 0;
        UpdateHandcuffVisuals();
        return taken;
    }

    // ── 내부 처리 ─────────────────────────────────────────────────────────
    private void EnqueueMinerals(int amount)
    {
        _mineralQueue += amount;
        if (!_isProcessing)
            StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        _isProcessing = true;
        while (_mineralQueue > 0)
        {
            yield return _waitProcess;
            _mineralQueue--;
            _handcuffCount += handcuffsPerMineral;

            // handcuffsPerMineral 만큼 비주얼 생성 (각 수갑마다 1개)
            for (int i = 0; i < handcuffsPerMineral; i++)
                SpawnHandcuffVisual();

            AudioManager.Play("handcuff_spawn");
        }
        _isProcessing = false;
    }

    private void SpawnHandcuffVisual()
    {
        if (_handcuffVisuals.Count >= maxPlateVisuals) return;

        int index = _handcuffVisuals.Count;
        Vector3 targetLocal = Vector3.up * stackHeightOffset * index;

        GameObject obj;
        if (_handcuffPool.Count > 0)
        {
            obj = _handcuffPool.Dequeue();
            obj.SetActive(true);
        }
        else if (handcuffPrefab != null && handcuffStackRoot != null)
        {
            obj = Instantiate(handcuffPrefab, handcuffStackRoot);
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale   *= 1.5f;
            var col = obj.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
        else return;

        var rend = obj.GetComponent<Renderer>();
        if (rend != null) rend.enabled = true;

        _handcuffVisuals.Add(obj);
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
            // 풀에 반환되어 비활성화된 경우 즉시 종료
            if (obj == null || !obj.activeInHierarchy) yield break;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / dropDuration);
            obj.transform.localPosition = Vector3.Lerp(startLocal, targetLocal, t);
            yield return null;
        }
        if (obj != null && obj.activeInHierarchy)
            obj.transform.localPosition = targetLocal;
    }

    private void UpdateHandcuffVisuals()
    {
        while (_handcuffVisuals.Count > _handcuffCount)
        {
            int last = _handcuffVisuals.Count - 1;
            var obj = _handcuffVisuals[last];
            _handcuffVisuals.RemoveAt(last);
            obj.SetActive(false);
            _handcuffPool.Enqueue(obj);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 ac = areaCenter  != null ? areaCenter.position  : transform.position;
        Vector3 pc = plateCenter != null ? plateCenter.position : transform.position;

        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
        Gizmos.DrawSphere(ac, areaRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(ac, areaRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
        Gizmos.DrawSphere(pc, plateRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        Gizmos.DrawWireSphere(pc, plateRadius);
    }
#endif
}
