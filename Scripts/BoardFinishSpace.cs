using UnityEngine;

public class BoardFinishSpace : MonoBehaviour
{
    [Header("Finish Bonuses")]
    [SerializeField] private int firstPlaceBonus = 30;
    [SerializeField] private int secondPlaceBonus = 20;
    [SerializeField] private int thirdPlaceBonus = 10;

    public int GetFinishBonus(int finishingPlace)
    {
        switch (finishingPlace)
        {
            case 1:
                return firstPlaceBonus;

            case 2:
                return secondPlaceBonus;

            case 3:
                return thirdPlaceBonus;

            default:
                return 0;
        }
    }
}