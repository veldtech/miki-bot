using System;

namespace IA.SDK
{
    public struct Color
    {
        public float R;
        public float G;
        public float B;

        public Color(float r, float g, float b)
        {
            r = Math.Max(0, Math.Min(r, 1));
            g = Math.Max(0, Math.Min(g, 1));
            b = Math.Max(0, Math.Min(b, 1));

            R = r;
            G = g;
            B = b;
        }

        public static Color Lerp(Color colorA, Color ColorB, float time)
        {
            float newR = colorA.R + (ColorB.R - colorA.R) * time;
            float newG = colorA.G + (ColorB.G - colorA.G) * time;
            float newB = colorA.B + (ColorB.B - colorA.B) * time;
            return new Color(newR, newG, newB);
        }
    }
}