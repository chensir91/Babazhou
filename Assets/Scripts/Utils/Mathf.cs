using System;

namespace Babazhou
{
    public static class Mathf
    {
        public static int Abs(int value) => Math.Abs(value);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static int FloorToInt(float value) => (int)Math.Floor(value);
        public static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);
    }
}