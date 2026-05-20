using UnityEngine;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    // Each cycle: 4 normal rooms, 1 mini-boss, 1 perk room.
    private const int CycleLength = 6;
    private const int NormalRoomsPerCycle = 4;
    private const int MiniBossIndexInCycle = 4; // 0-based index within the cycle

    public int CurrentRoomIndex { get; private set; } = 0;
    public bool RunActive { get; private set; } = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        StartRun();
    }

    public void StartRun()
    {
        CurrentRoomIndex = 0;
        RunActive = true;
        RoomManager.Instance.LoadFirstBatch();
    }

    public void AdvanceRoom()
    {
        CurrentRoomIndex++;
        RoomManager.Instance.ShiftRoom();
    }

    /// <summary>
    /// Advances CurrentRoomIndex past the perk room the player is still standing
    /// in when the next batch begins loading. Does NOT call ShiftRoom — the perk
    /// room's destruction is handled separately by the deferred-destroy pipeline.
    /// </summary>
    public void AdvancePastPerkRoom()
    {
        CurrentRoomIndex++;
    }

    public void PlayerDied()
    {
        RunActive = false;
        Debug.Log("Player died — run over.");
    }

    /// <summary>Returns the room type for an absolute room index based on the 6-room cycle.</summary>
    public RoomType GetRoomType(int index)
    {
        int posInCycle = index % CycleLength;
        if (posInCycle == CycleLength - 1) return RoomType.Perk;
        if (posInCycle == MiniBossIndexInCycle) return RoomType.MiniBoss;
        return RoomType.Normal;
    }

    public int GetDifficulty() => CurrentRoomIndex + 1;

    /// <summary>Returns the difficulty for a specific absolute room index.</summary>
    public int GetDifficulty(int roomIndex) => roomIndex + 1;
}

public enum RoomType { Normal, MiniBoss, Boss, Perk }
