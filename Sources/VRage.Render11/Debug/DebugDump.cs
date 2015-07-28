using System;
using System.Collections.Generic;
using System.IO;
using SharpDX.DXGI;
using VRage.Library.Debugging;
using VRageMath;
using VRageRender;
using Vector2 = VRageMath.Vector2;

namespace VRage.Render11.Debug
{
    class DebugDump
    {
        private static readonly Dictionary<string, DebugCapture2D> captures = new Dictionary<string, DebugCapture2D>();

        public static DebugCapture2D DefineCapture(string name, MyBindableResource source, Format maybeFormat, IUnpacker maybeUnpacker)
        {
            lock(captures)
            {
                DebugCapture2D captureDef;
                if(captures.TryGetValue(name, out captureDef)) return captureDef;

                captureDef = new DebugCapture2D(source, maybeFormat, maybeUnpacker);
                captures.Add(name, captureDef);
                return captureDef;
            }
        }

        public static void DumpAll()
        {
            foreach(var pair in captures)
            {
                pair.Value.DumpPixels(Path.Combine("e:\\frames", pair.Key));
            }
            Logging.Flush();
        }

        public static unsafe Vector2 Read2D(MyBindableResource source)
        {
            using(var temp = new DebugCapture2D(source, Format.R32G32_Float, null, new Vector3I { X = 1, Y = 1}))
            {
                temp.Capture();
                Vector2 target;
                var targetPtr = &target;
                temp.DumpTo((IntPtr)targetPtr, (uint)sizeof(Vector2));
                return target;
            }
        }
        public static unsafe float Read1D(MyBindableResource source)
        {
            using(var temp = new DebugCapture2D(source, Format.R32_Float, null, new Vector3I { X = 1, Y = 1}))
            {
                temp.Capture();
                float target;
                var targetPtr = &target;
                temp.DumpTo((IntPtr)targetPtr, (uint)sizeof(float));
                return target;
            }
        }


        public static void FatalDumpAll()
        {
            DumpAll();
            Environment.Exit(0);
        }


    }
}
