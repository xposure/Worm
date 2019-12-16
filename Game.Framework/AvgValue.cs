using System;
using System.Diagnostics;

namespace Game.Framework
{
    public struct AvgValue
    {
        public float Weight;
        public float Value;

        public AvgValue(float weight, float initialValue)
        {
            Weight = weight;
            Value = initialValue;
        }


        public override string ToString() => $"{Value:0.#}";

        public static implicit operator float(AvgValue value) => value.Value;
        public static AvgValue operator +(AvgValue avg, float value)
        {
            return new AvgValue(avg.Weight, avg.Value * avg.Weight + value * (1f - avg.Weight));
        }

        public static AvgValue operator +(AvgValue avg, TimeSpan value)
        {
            return new AvgValue(avg.Weight, avg.Value * avg.Weight + (float)(value.TotalMilliseconds * (1f - avg.Weight)));
        }
        public static AvgValue operator +(AvgValue avg, Stopwatch value)
        {
            return new AvgValue(avg.Weight, avg.Value * avg.Weight + (float)(value.Elapsed.TotalMilliseconds * (1f - avg.Weight)));
        }


        public static AvgValue operator +(AvgValue avg, double value)
        {
            return new AvgValue(avg.Weight, (avg.Value * avg.Weight + (float)(value * (1f - avg.Weight))));
        }
    }
}