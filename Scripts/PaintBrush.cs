using UnityEngine;

public class PaintBrush : MonoBehaviour
{
    [SerializeField] private RenderTexture paintTexture;
    [SerializeField] private Texture2D brushTexture;
    [SerializeField] private int brushSize = 30;

    public void Paint(Vector2 uv, Color color)
    {
        if (paintTexture == null)
        {
            Debug.LogError("PaintBrush: Paint Texture missing!");
            return;
        }

        if (brushTexture == null)
        {
            Debug.LogError("PaintBrush: Brush Texture missing!");
            return;
        }

        int x = Mathf.RoundToInt(
            uv.x * paintTexture.width
        );

        int y = Mathf.RoundToInt(
            uv.y * paintTexture.height
        );

        RenderTexture previous =
            RenderTexture.active;

        RenderTexture.active =
            paintTexture;

        GL.PushMatrix();

        GL.LoadPixelMatrix(
            0,
            paintTexture.width,
            0,
            paintTexture.height
        );

        Material material =
            new Material(
                Shader.Find("Unlit/Transparent")
            );

        material.color = color;

        Graphics.DrawTexture(
            new Rect(
                x - brushSize / 2,
                y - brushSize / 2,
                brushSize,
                brushSize
            ),
            brushTexture,
            material
        );

        GL.PopMatrix();

        RenderTexture.active =
            previous;

        Destroy(material);
    }
}