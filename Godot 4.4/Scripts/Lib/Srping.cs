using System;
using Godot;

// spring stuff yea, Thats it

public class CriticallyDampedSpring {
    private float _D { get;  set;}
    private Vector3 _P { get; set;}
    private Vector3 _G { get; set;}
    private Vector3 _V { get; set;}

    public CriticallyDampedSpring(float? DampingRatio, Vector3 Position) {
        float RecastDampRatio = DampingRatio ?? 0.15f; // lower = snappier, higher = floatier
        _D = RecastDampRatio;
        _P = Position;
        _G = Position;
        _V = Vector3.Zero;
    }

    public void Update(float DampingRatio) {
        _D = DampingRatio;
    }

    public void SetGoal(Vector3 Goal) {
        _G = Goal;
    }

    public Vector3 Step(double Delta) {
        float O = 2.0f / _D;
        float X = O * (float)Delta;
        float EXP = 1.0f / (1.0f + X + 0.48f * X * X + 0.235f * X * X * X);

        Vector3 D = _P - _G;
        Vector3 Temp = (_V + D * 2.0f) * (float)Delta;
        _V = (_V - Temp * O) * EXP;
        _P = _G + (D + Temp) * EXP;

        return _P;
    }

    // @export var smooth_time := 0.15  #

    //  pos = global_position
    // var target_pos = target.global_position

    // # Damped spring (based on SmoothDamp from Unity)
    // var omega = 2.0 / smooth_time
    // var x = omega * delta
    // var exp = 1.0 / (1.0 + x + 0.48 * x * x + 0.235 * x * x * x)

    // var change = pos - target_pos
    // var temp = (velocity + change * omega) * delta
    // velocity = (velocity - temp * omega) * exp
    // var new_pos = target_pos + (change + temp) * exp

    // global_position = new_pos
}

public class Spring {
    // Basic Storage Shit
    private float _D{get; set;}
    private float _F{get; set;}
    private Vector3 _G{get; set;}
    private Vector3 _P{get; set;}
    private Vector3 _V{get; set;}

    float EPS = 1e-4f;

    // Constructor yes
    public Spring(float DampingRatio,float Frequency,Vector3 Position) {
        _D = DampingRatio;
        _F = Frequency;
        _G = Position;
        _P = Position;
        _V = Position * 0;
    }

	// Updates Everything
	public void Update(float DampingRatio,float Frequency,Vector3 Position) {
		 _D = DampingRatio;
        _F = Frequency;
        _G = Position;
        _P = Position;
        _V = Position * 0;
	}

    // Sets The Goal
    public void SetGoal(Vector3 Position) {
        _G = Position;
    }
    // returns Position
    public Vector3 GetPosition() {
        return _P;
    }
    // returns Velocity
    public Vector3 GetVelocity() {
        return _V;
    }

    // Does the main fram update
    public Vector3 Step(double Delta) {
        float DeltaTime = (float)Delta;

        float D = _D;
        float F = _F * 2 * (float)Math.PI;
        Vector3 G = _G;
        Vector3 P0 = _P;
        Vector3 V0 = _V;

        Vector3 Offset = P0 - G;
        float Decay = (float)Math.Exp(-D * F * DeltaTime);

        Vector3 P1;
        Vector3 V1;

        if(D == 1f) { //Critically damped
            P1 = (Offset * (1 + F * DeltaTime) + V0 * DeltaTime) * Decay + G;
            V1 = (V0 * (1 - F * DeltaTime) - Offset * (F * F * DeltaTime)) * Decay;
        } else if(D < 1) {
            float C = (float)Math.Sqrt(1 - D * D);

            float I = (float)Math.Cos(F * C * DeltaTime);
            float J = (float)Math.Sin(F * C * DeltaTime);

            float Z;

            if(C > EPS) {
                Z = J / C;
            } else {
                float A = DeltaTime * F;
                Z = A + ((A * A) * (C * C) * (C * C) / 20 - C * C) * (A * A * A) / 6;
            }

            float Y;

            if(F * C > EPS) {
                Y = J / (F * C);
            } else {
                float B = F * C;
                Y = DeltaTime + ((DeltaTime * DeltaTime) * (B * B) * (B * B) / 20 - B * B) * (DeltaTime * DeltaTime * DeltaTime) / 6;
            }

            P1 = (Offset * (I + D * Z) + V0 * Y) * Decay + G;
            V1 = (V0 * (I - Z * D) - Offset * (Z * F)) * Decay;
        } else { //Overdamped
            float C = (float)Math.Sqrt(D * D - 1);

            float R1 = -F * (D - C);
            float R2 = -F * (D + C);

            Vector3 C02 = (V0 - Offset * R1) / (2 * F * C);
            Vector3 C01 = Offset - C02;

            Vector3 E1 = C01 * (float)Math.Exp(R1 * DeltaTime);
            Vector3 E2 = C02 * (float)Math.Exp(R2 * DeltaTime);

            P1 = E1 + E2 + G;
            V1 = E1 * R1 + E2 * R2;
        }

        _P = P1;
        _V = V1;

		return P1;
    }
}
