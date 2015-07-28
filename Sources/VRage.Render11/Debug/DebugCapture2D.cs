using System;
using System.IO;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRageMath;
using VRageRender;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace VRage.Render11.Debug
{
    internal class DebugCapture2D : IDisposable
    {
        private readonly MyBindableResource source;
        private readonly IUnpacker unpacker;
        private readonly Vector3I size;
        private Texture2D texture;

        public int X { get { return size.X; } }
        public int Y { get { return size.Y; } }

        public DebugCapture2D(MyBindableResource source, Format format, IUnpacker unpacker) : this(source, format, unpacker, source.GetSize())
        {
            System.Diagnostics.Debug.Assert(source.GetSize().Z == 1);
        }

        public DebugCapture2D(MyBindableResource source, Format format, IUnpacker unpacker, Vector3I size)
        {
            this.source = source;
            this.unpacker = unpacker;
            this.size = size;
            System.Diagnostics.Debug.Assert(size.X > 0);
            System.Diagnostics.Debug.Assert(size.Y > 0);
            var description = new Texture2DDescription()
            {
                Height = size.Y,
                Width = size.X,
                MipLevels = 0,
                ArraySize = 1,
                SampleDescription = { Count = 1 },
                Format = format,
                Usage = ResourceUsage.Staging,
                CpuAccessFlags = CpuAccessFlags.Read | CpuAccessFlags.Write
            };
            texture = new Texture2D(MyRender11.Device, description);
        }

        public void Capture()
        {
            int mip;

            var targetSubRes = texture.CalculateSubResourceIndex(0, 0, out mip);
            var sourceSubRes = source.m_resource.CalculateSubResourceIndex(0, 0, out mip);

            MyImmediateRC.RC.Context.CopySubresourceRegion(source.m_resource, sourceSubRes, new ResourceRegion(0, 0, 0, size.X, size.Y, 1), texture, targetSubRes);
        }

        public void Dump(Action<DataBox> read)
        {
            int mip;
            var targetSubRes = texture.CalculateSubResourceIndex(0, 0, out mip);
            var box = MyImmediateRC.RC.Context.MapSubresource(texture, targetSubRes, MapMode.Read, MapFlags.None);
            read(box);
            GC.KeepAlive(box);
            MyImmediateRC.RC.Context.UnmapSubresource(texture, targetSubRes);
        }

        public void DumpTo(IntPtr targetPtr, uint bufferSize)
        {
            Dump(box => CopyMemory(targetPtr, box.DataPointer, bufferSize));
        }

        public void DumpPixels(string rootDir)
        {
            if(!Directory.Exists(rootDir)) Directory.CreateDirectory(rootDir);
            var unpacked = unpacker.Unpack(this);

            DebugUtil.WriteImage(unpacked, Path.Combine(rootDir, "pic-0-1.tga"), v => new Vector4UByte { X = (byte)Math.Min(v.X * 256, 255), Y = (byte)Math.Min(v.Y * 256, 255), Z = (byte)Math.Min(v.Z * 256, 255), W = 255 });
            DebugUtil.WriteImage(unpacked, Path.Combine(rootDir, "pic-65536.tga"), v => new Vector4UByte { X = (byte)Math.Min(v.X / 65536, 255), Y = (byte)Math.Min(v.Y / 65536, 255), Z = (byte)Math.Min(v.Z / 65536, 255), W = 255 });

            DebugUtil.WriteCSV(unpacked, Path.Combine(rootDir, "channel-red.csv"), v => v.X);
            DebugUtil.WriteCSV(unpacked, Path.Combine(rootDir, "channel-green.csv"), v => v.Y);
            DebugUtil.WriteCSV(unpacked, Path.Combine(rootDir, "channel-blue.csv"), v => v.Z);
            DebugUtil.WriteCSV(unpacked, Path.Combine(rootDir, "calc-lum.csv"), DebugUtil.CalcLuminance);
        }

        public void Dispose()
        {
            texture.Dispose();
            texture = null;
        }

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);
    }
}