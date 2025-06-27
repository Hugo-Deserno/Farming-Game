using System;
using Godot;

// Extended math lib cuz i needed it
namespace MathPlus {
    // class shit
    class MathP {
        // Linear interpolation function
        // A: Startpoint
        // B: Enpoint
        // T: Time
        // exepect : Vector3, Vector2, floats

        // i rlly did overdramatize a three line formula :|
        public static Variant Lerp(Variant A, Variant B, Variant T) {
            // Solves vec 3 Equation
            if(A.VariantType == Variant.Type.Vector3) {
                Vector3 _A = (Vector3)A;
                Vector3 _B = (Vector3)B;
                Vector3 _T;

                // Setting The Timescale Cuz i wanna be cool and have a flexible system
                if(T.VariantType == Variant.Type.Float) {
                    _T = new Vector3((float)T,(float)T,(float)T);
                } else {
                    _T = (Vector3)T;
                }

                // Generate the Differnce between A and B
                (float D_X, float D_Y, float D_Z) = (
                    _B.X - _A.X,
                    _B.Y - _A.Y,
                    _B.Z - _A.Z);
                // Take the Timescale into Equation
                (float E_X, float E_Y, float E_Z) = (
                    D_X * _T.X,
                    D_Y * _T.Y,
                    D_Z * _T.Z
            );
                // Combine A With the timescaled Difference
                (float R_X, float R_Y, float R_Z) = (
                    _A.X + E_X,
                    _A.Y + E_Y,
                    _A.Z + E_Z
                );

                Vector3 R = new Vector3(R_X,R_Y,R_Z);
                return R;
            // Solves vec 2 equation
            } else if(A.VariantType == Variant.Type.Vector2) {
                Vector2 _A = (Vector2)A;
                Vector2 _B = (Vector2)B;
                Vector2 _T = T.VariantType == Variant.Type.Float ? new Vector2((float)T,(float)T) : (Vector2)T;

                // Generate the Differnce between A and B
                float D_X = _B.X - _A.X;
                float D_Y = _B.Y - _A.Y;
                // Take the Timescale into Equation
                float E_X = D_X * _T.X;
                float E_Y = D_Y * _T.Y;
                // Combine A With the timescaled Difference
                float R_X = _A.X + E_X;
                float R_Y = _A.Y + E_Y;
                
                Vector2 R = new Vector2(R_X,R_Y);
                return R;
            // Solves floats
            } else {
                float _A = (float)A;
                float _B = (float)B;
                float _T = (float)T;

                // rinse and repeat the steps above here
                float D = _B - _A;
                float E = D * _T;
                float R = _A + E;
                return R;    
            }
        }

        // Converts A From Degrees to Radians
        // A: Degrees in question
        // D: Decimals to round of to
        // expects: A: float, D: Digit
        
        public static Variant Rad(float A, int D) {
            float PI = (float)(Math.PI / 180);
            float R = A * PI;

            // Check if Decimal could be considerd as a Integer
            if(D == 0) {
                int _R = (int)Math.Round(R,D);
                return _R;
            } else {
                float _R = (int)Math.Round(R,D);
                return _R;
            }
        }
    }
}