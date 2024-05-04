﻿using Godot;
using KludgeBox;

namespace NeonWarfare;

public class Shake(double strength, double time, bool deceising = true) : IShiftProvider
{
    public double Time { get; private set; } = time;
    public double InitialTime { get; private set; } = time;

    public double Strength => InitialStrength * (Time / InitialTime);
    public double InitialStrength { get; private set; } = strength;

    public Vector2 Shift => Rand.InsideUnitCircle * (deceising ? Strength : InitialStrength);
    public bool IsAlive => Time > Mathf.Epsilon;
		
    public void Update(double delta)
    {
        Time = Mathf.Max(0, Time - delta);
    }
}