using UnityEngine;

namespace NodeEditorFramework.Extensions
{
    public static class Texture2DExtensions
    {
        public static Texture2D CreateTexture(this Texture2D texture, int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = color;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pixels);
            result.Apply();
            return result;
        }
    }
}