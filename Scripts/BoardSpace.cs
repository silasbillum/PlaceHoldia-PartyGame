using UnityEngine;

public class BoardSpace : MonoBehaviour
{
    public enum SpaceType
    {
        Normal,
        AddPoints,
        RemovePoints,
        Minigame
    }

    [Header("Space")]
    [SerializeField] private SpaceType spaceType;

    [Header("Points")]
    [SerializeField] private int points = 10;

    [Header("Minigame")]
    [SerializeField] private string minigameName;

    public SpaceType GetSpaceType()
    {
        return spaceType;
    }

    public int GetPoints()
    {
        return points;
    }

    public string GetMinigameName()
    {
        return minigameName;
    }
}
