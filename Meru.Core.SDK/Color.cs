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

        public static Color GetColor(IAColor color)
        {
            switch (color)
            {
                case IAColor.BLACK: return new Color(0.1f, 0.2f, 0.1f);
                case IAColor.WHITE: return new Color(0.9f, 0.9f, 0.9f);
                case IAColor.RED: return new Color(1.0f, 0.6f, 0.4f);
                case IAColor.GREEN: return new Color(0.6f, 1.0f, 0.4f);
                case IAColor.BLUE: return new Color(0.4f, 0.6f, 1.0f);
                case IAColor.YELLOW: return new Color(1.0f, 1.0f, 0.2f);
                default:
                case IAColor.ORANGE: return new Color(0.8f, 0.8f, 0.2f);
                case IAColor.PURPLE: return new Color(1.0f, 0.6f, 0.8f);
            }
        }

        public static Color Lerp(Color colorA, Color ColorB, float time)
        {
            float newR = colorA.R + (ColorB.R - colorA.R) * time;
            float newG = colorA.G + (ColorB.G - colorA.G) * time;
            float newB = colorA.B + (ColorB.B - colorA.B) * time;
            return new Color(newR, newG, newB);
        }
    }

    public enum IAColor
    {
        WHITE, BLACK, RED, ORANGE, YELLOW, GREEN, BLUE, PURPLE
    }
}