using UnityEngine;
using TMPro;

public class WinnerTextEffect : MonoBehaviour
{
    [SerializeField] private TMP_Text winnerText;

    private float timer;

    private void Update()
    {
        if (!gameObject.activeSelf)
            return;

        timer += Time.deltaTime;

        // Gold shimmer
        float shimmer =
            (Mathf.Sin(timer * 5f) + 1f) / 2f;

        Color gold =
            Color.Lerp(
                new Color(1f, 0.55f, 0.05f),
                new Color(1f, 1f, 0.4f),
                shimmer
            );

        winnerText.color = gold;

        // Slight size pulse
        float scale =
            1f + Mathf.Sin(timer * 4f) * 0.04f;

        winnerText.transform.localScale =
            Vector3.one * scale;
    }
}