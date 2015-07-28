using System;
using System.IO;
using VRageMath;
using Vector3 = SharpDX.Vector3;

namespace VRage.Render11.Debug
{
    static class DebugUtil
    {
        public static void WriteCSV(Vector3[,] texture, string fileNameCsv, Func<Vector3, float> getPlane)
        {
            var y = texture.GetLength(0);
            var x = texture.GetLength(1);
            
            using(var fs = File.CreateText(fileNameCsv))
            {
                for(var i = 0; i < y; i++)
                {
                    for(var j = 0; j < x; j++)
                    {
                        if(j > 0) fs.Write(",");
                        fs.Write("{0}", getPlane(texture[i,j]));
                    }
                    fs.WriteLine();
                }
            }
        }

        public static void WriteImage(Vector3[,] texture, string fileNameTga, Func<Vector3, Vector4UByte> getPlanes)
        {
            var y = texture.GetLength(0);
            var x = texture.GetLength(1);
            
            using(var fs = new BinaryWriter(File.Create(fileNameTga)))
            {
                var header = new byte[]{
                    0,
                    0,
                    2,
                    0, 0, 0, 0, 0,
                    0, 0, 0, 0
                };
                fs.Write(header, 0, header.Length);
                fs.Write((ushort)x);
                fs.Write((ushort)y);
                fs.Write((byte)32);
                fs.Write((byte)0x18);
                for(var i = y - 1; i >= 0; i--) // bottom-up
                {
                    for(var j = 0; j < x; j++)
                    {
                        var planes = getPlanes(texture[i,j]);
                        fs.Write(planes.Z);
                        fs.Write(planes.Y);
                        fs.Write(planes.X);
                        fs.Write(planes.W);
                    }
                }
            }
        }
        

        public static float UnpackFloat(ref uint part, int mantissaBits, int exponentBits, int bias)
        {
            var mMask = (uint)(1 << mantissaBits) - 1;
            var mantissa = (part & mMask);
            part >>= mantissaBits;
            var eMask =  (uint)(1 << exponentBits) - 1;
            var exponent = (part & eMask);
            part >>= exponentBits;

            if(mantissa == 0)
            {
                if(exponent == eMask) return Single.PositiveInfinity;
            }
            if(exponent != 0)
            {
                mantissa += mMask + 1;
            }
            

            return (float)Math.Pow(2, exponent - bias - mantissaBits) * mantissa;
        }


        public static float CalcLuminance(Vector3 v)
        {
            return Vector3.Dot(LUM_VEC, v);
        }
        private static readonly Vector3 LUM_VEC = new Vector3(0.299f, 0.587f, 0.114f);
    }
}