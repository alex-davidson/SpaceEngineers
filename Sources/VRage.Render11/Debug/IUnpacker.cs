using SharpDX;

namespace VRage.Render11.Debug
{
    internal interface IUnpacker
    {
        Vector3[,] Unpack(DebugCapture2D capture);
    }
}