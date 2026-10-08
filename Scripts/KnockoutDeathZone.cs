using UnityEngine;

public class KnockoutDeathZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        KnockoutPlayer player =
            other.GetComponentInParent<KnockoutPlayer>();

        if (player == null)
            return;

        player.Eliminate();
    }
}