using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace TaffyUGUI
{
    internal sealed class TaffyNativeMarshallingScope : IDisposable
    {
        private readonly List<GCHandle> _handles = new List<GCHandle>();

        internal IntPtr PinArray<T>(T[] values) where T : struct
        {
            if (values == null || values.Length == 0)
                return IntPtr.Zero;

            GCHandle handle = GCHandle.Alloc(values, GCHandleType.Pinned);
            _handles.Add(handle);
            return handle.AddrOfPinnedObject();
        }

        internal TaffyNative.StringView PinString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return default;

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            _handles.Add(handle);
            return new TaffyNative.StringView
            {
                data = handle.AddrOfPinnedObject(),
                len = (uint)bytes.Length,
            };
        }

        public void Dispose()
        {
            for (int i = _handles.Count - 1; i >= 0; i--)
            {
                if (_handles[i].IsAllocated)
                    _handles[i].Free();
            }
            _handles.Clear();
        }
    }
}
