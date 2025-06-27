using System;
using Godot;

// spring stuff yea, Thats it

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

/*

local Spring = {} do
	Spring.__index = Spring

	local pi = math.pi
	local exp = math.exp
	local sin = math.sin
	local cos = math.cos
	local sqrt = math.sqrt

	local EPS = 1e-4

	function Spring.new(dampingRatio: number, frequency: number, position)
		assert(type(dampingRatio) == "number")
		assert(type(frequency) == "number")
		assert(dampingRatio*frequency >= 0, "Spring does not converge")

		return setmetatable({
			d = dampingRatio,
			f = frequency,
			g = position,
			p = position,
			v = position*0, -- Match the original vector type
		}, Spring)
	end

	function Spring:setGoal(newGoal)
		self.g = newGoal
	end

	function Spring:getPosition()
		return self.p
	end

	function Spring:getVelocity()
		return self.v
	end

	function Spring:step(dt: number)
		local d = self.d
		local f = self.f*2*pi
		local g = self.g
		local p0 = self.p
		local v0 = self.v

		local offset = p0 - g
		local decay = exp(-d*f*dt)

		local p1, v1

		if d == 1 then -- Critically damped
			p1 = (offset*(1 + f*dt) + v0*dt)*decay + g
			v1 = (v0*(1 - f*dt) - offset*(f*f*dt))*decay

		elseif d < 1 then -- Underdamped
			local c = sqrt(1 - d*d)

			local i = cos(f*c*dt)
			local j = sin(f*c*dt)

			-- Damping ratios approaching 1 can cause division by small numbers.
			-- To fix that, group terms around z=j/c and find an approximation for z.
			-- Start with the definition of z:
			--    z = sin(dt*f*c)/c
			-- Substitute a=dt*f:
			--    z = sin(a*c)/c
			-- Take the Maclaurin expansion of z with respect to c:
			--    z = a - (a^3*c^2)/6 + (a^5*c^4)/120 + O(c^6)
			--    z ≈ a - (a^3*c^2)/6 + (a^5*c^4)/120
			-- Rewrite in Horner form:
			--    z ≈ a + ((a*a)*(c*c)*(c*c)/20 - c*c)*(a*a*a)/6

			local z
			if c > EPS then
				z = j/c
			else
				local a = dt*f
				z = a + ((a*a)*(c*c)*(c*c)/20 - c*c)*(a*a*a)/6
			end

			-- Frequencies approaching 0 present a similar problem.
			-- We want an approximation for y as f approaches 0, where:
			--    y = sin(dt*f*c)/(f*c)
			-- Substitute b=dt*c:
			--    y = sin(b*c)/b
			-- Now reapply the process from z.

			local y
			if f*c > EPS then
				y = j/(f*c)
			else
				local b = f*c
				y = dt + ((dt*dt)*(b*b)*(b*b)/20 - b*b)*(dt*dt*dt)/6
			end

			p1 = (offset*(i + d*z) + v0*y)*decay + g
			v1 = (v0*(i - z*d) - offset*(z*f))*decay

		else -- Overdamped
			local c = sqrt(d*d - 1)

			local r1 = -f*(d - c)
			local r2 = -f*(d + c)

			local co2 = (v0 - offset*r1)/(2*f*c)
			local co1 = offset - co2

			local e1 = co1*exp(r1*dt)
			local e2 = co2*exp(r2*dt)

			p1 = e1 + e2 + g
			v1 = e1*r1 + e2*r2
		end

		self.p = p1
		self.v = v1

		return p1
	end
end

*/