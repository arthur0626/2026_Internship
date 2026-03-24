using UnityEngine;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [Header("전역 통계")]
    public int totalMinedOre = 0; // 지금까지 채굴된 전체 광물 수
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void AddMinedOre(int amount)
    {
        if (amount <= 0) return;
        totalMinedOre += amount;
    }
}