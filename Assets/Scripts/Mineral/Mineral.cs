using System.Collections;
using UnityEngine;

public class Mineral : MonoBehaviour
{
    [Header("채굴 설정")]
    [SerializeField] private float maxAmount = 10f;
    [SerializeField] private float respawnTime = 5f;

    private float _currentAmount;
    private bool _isDepleted;
    private Collider _collider;
    private Renderer _renderer;

    private void Awake()
    {
        _currentAmount = maxAmount;

        _collider = GetComponent<Collider>();
        _renderer = GetComponent<Renderer>();

        if (_collider != null)
            _collider.isTrigger = true;
    }

    /// <summary>
    /// 플레이어나 다른 시스템에서 호출하는 채굴 함수.
    /// 한 번 호출에 1만큼 채굴하고, 실제로 채굴되었으면 true를 반환.
    /// </summary>
    public bool Mine()
    {
        if (_isDepleted)
            return false;

        _currentAmount -= 1f;

        if (_currentAmount <= 0f)
            StartCoroutine(RespawnRoutine());

        return true;
    }

    private IEnumerator RespawnRoutine()
    {
        _isDepleted = true;
        _currentAmount = 0f;

        // 시각적/물리적으로 비활성화 (SetActive 대신 개별 컴포넌트 끄기)
        if (_renderer != null) _renderer.enabled = false;
        if (_collider != null) _collider.enabled = false;

        yield return new WaitForSeconds(respawnTime);

        // 리스폰
        _isDepleted = false;
        _currentAmount = maxAmount;

        if (_renderer != null) _renderer.enabled = true;
        if (_collider != null) _collider.enabled = true;
    }
}

