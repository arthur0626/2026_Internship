using UnityEngine;

/// <summary>
/// 채굴 가드 고용 업그레이드 발판 (Env/Mineral/Upgrade (1)) — 1회성.
/// </summary>
public class MinerGuardUpgrade : UpgradeBase
{
    [Header("채굴 가드 업그레이드 설정")]
    [Tooltip("활성화할 MinerGuardAI 컴포넌트 (Entity/Guard (1))")]
    [SerializeField] private MinerGuardAI minerGuardAI;

    protected override void OnUpgradeApplied()
    {
        if (minerGuardAI != null)
            minerGuardAI.Activate();
    }
}
