using UnityEngine;

public class PaintCanvas : MonoBehaviour
{
    [SerializeField] private RenderTexture paintTexture;
    [SerializeField] private Shader paintShader;

    private Material paintMaterial;

    private void Start()
    {
        if (paintTexture == null)
        {
            Debug.LogError("Paint Texture is missing!");
            return;
        }

        if (paintShader == null)
        {
            Debug.LogError("Paint Shader is missing!");
            return;
        }

        paintMaterial = new Material(paintShader);

        ClearCanvas();
    }

    private void ClearCanvas()
    {
        RenderTexture previous =
            RenderTexture.active;

        RenderTexture.active =
            paintTexture;

        GL.Clear(
            true,
            true,
            Color.white
        );

        RenderTexture.active =
            previous;
    }

    public void Paint(Vector2 uv, Color color)
    {
        if (paintTexture == null)
            return;

        if (paintMaterial == null)
            return;

        Debug.Log("PAINT: " + uv);

        paintMaterial.SetVector(
            "_BrushUV",
            new Vector4(
                uv.x,
                uv.y,
                0,
                0
            )
        );

        paintMaterial.SetFloat(
            "_BrushSize",
            0.03f
        );

        paintMaterial.SetColor(
            "_PaintColor",
            color
        );

        RenderTexture temp =
            RenderTexture.GetTemporary(
                paintTexture.descriptor
            );

        // Copy current painting into temporary texture
        Graphics.Blit(
            paintTexture,
            temp
        );

        // Draw the new paint
        Graphics.Blit(
            temp,
            paintTexture,
            paintMaterial
        );

        RenderTexture.ReleaseTemporary(
            temp
        );
    }

    private void OnDestroy()
    {
        if (paintShader == null)
        {
            Debug.LogError("Paint Shader is missing!");
            return;
        }

        paintMaterial = new Material(paintShader);
    }

    public Texture2D GetPaintTextureAsTexture2D()
    {
        RenderTexture previous =
            RenderTexture.active;

        RenderTexture.active =
            paintTexture;

        Texture2D texture =
            new Texture2D(
                paintTexture.width,
                paintTexture.height,
                TextureFormat.RGBA32,
                false
            );

        texture.ReadPixels(
            new Rect(
                0,
                0,
                paintTexture.width,
                paintTexture.height
            ),
            0,
            0
        );

        texture.Apply();

        RenderTexture.active =
            previous;

        return texture;
    }
}