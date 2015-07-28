using System;
using SharpDX;

namespace VRage.Render11.Debug
{
    internal class UnpackerR11G11B10 : IUnpacker
    {
        public unsafe Vector3[,] Unpack(DebugCapture2D capture)
        {
            var data = new uint[capture.Y,capture.X];
            fixed(uint* targetPtr = data)
            {
                capture.DumpTo((IntPtr)targetPtr,(uint)(data.Length * sizeof(uint)));
            }

            return UnpackTextureR11G11B10(data);
        }

        
        private static Vector3[,] UnpackTextureR11G11B10(uint[,] texture)
        {
            var x = texture.GetLength(1);
            var y = texture.GetLength(0);
            var unpacked = new Vector3[y,x];
            for(var i = 0; i < y; i++)
            {
                for(var j = 0; j < x; j++)
                {
                    unpacked[i,j] = UnpackR11G11B10(texture[i,j]);
                }
            }
            return unpacked;
        }

        private static Vector3 UnpackR11G11B10(uint p)
        {
            var r = DebugUtil.UnpackFloat(ref p, 6, 5, 15);
            var g = DebugUtil.UnpackFloat(ref p, 6, 5, 15);
            var b = DebugUtil.UnpackFloat(ref p, 5, 5, 15);
            System.Diagnostics.Debug.Assert(p == 0);

            return new Vector3{ X = r, Y = g, Z = b };
        }
    }
}