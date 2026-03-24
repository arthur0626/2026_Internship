using UnityEngine;

/// <summary>
/// 채굴 능력 업그레이드 발판 — 반복 구매 가능, 비용 costMultiplier 배씩 증가.
/// </summary>
public class MiningUpgrade : UpgradeBase
{
    [Header("채굴 업그레이드 설정")]
    [Tooltip("업그레이드 대상 PlayerMiner")]
    [SerializeField] private PlayerMiner playerMiner;

    [Tooltip("채굴 간격 감소량 (초) — mineInterval에서 차감, 최소 0.1초")]
    [SerializeField] private float mineIntervalReduction = 0.5f;

    [Tooltip("최대 보유 광물 증가량")]
    [SerializeField] private int maxOreBonus = 5;

    [Tooltip("구매마다 비용에 곱하는 배율 (2 = 매번 2배)")]
    [SerializeField] private float costMultiplier = 2f;

    protected override void OnUpgradeApplied()
    {
        if (playerMiner == null) return;
        playerMiner.mineInterval = Mathf.Max(0.1f, playerMiner.mineInterval - mineIntervalReduction);
        playerMiner.maxOre      += maxOreBonus;
    }

    protected override int GetNextCost(int currentCost) =>
        Mathf.RoundToInt(currentCost * costMultiplier);
}
