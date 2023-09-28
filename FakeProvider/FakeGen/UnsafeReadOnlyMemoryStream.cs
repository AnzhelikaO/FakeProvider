#region Using

using System;
using System.Runtime.InteropServices;
using System.IO;

#endregion

namespace FakeProvider.FakeGen
{
    /// <summary>
    /// Exists only to allow Terraria to change the position of the information being read.
    /// Please do not use this class for any other purpose.
    /// </summary>
    public unsafe sealed class UnsafeReadOnlyMemoryStream : MemoryStream
    {
        #region Data

        public byte[] Data;
        public byte* dataPtr;
        private GCHandle handle;
        private bool disposedValue;

        #endregion
        #region Properties

        public override long Position
        {
            get => (int)(dataPtr - (byte*)handle.AddrOfPinnedObject());
            set
            {
                fixed (byte* ptr = &Data[value])
                    dataPtr = ptr;
            }
        }

        public override bool CanWrite => false;
        public override bool CanRead => true;

        #endregion

        #region Constructors

        public UnsafeReadOnlyMemoryStream(byte[] buffer) : this(buffer, true)
        {

        }

        public UnsafeReadOnlyMemoryStream(byte[] buffer, bool writable)
            : base(buffer, writable)
        {
            Data = buffer;
            Initialize();
        }

        public UnsafeReadOnlyMemoryStream(byte[] buffer, int index, int count)
            : this(buffer, index, count, true, false)
        {

        }

        public UnsafeReadOnlyMemoryStream(byte[] buffer, int index, int count, bool writable)
            : this(buffer, index, count, writable, false)
        {

        }

        public UnsafeReadOnlyMemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible)
            : base(buffer, index, count, writable, publiclyVisible)
        {
            Data = buffer;
            Initialize();
        }

        #endregion
        #region Initialize

        private unsafe void Initialize()
        {
            handle = GCHandle.Alloc(Data, GCHandleType.Pinned);
            fixed (byte* ptr = &Data[0])
                dataPtr = ptr;
        }

        #endregion
        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        GC.SuppressFinalize(this);
                    }
                    Data = null;
                    handle.Free();
                    disposedValue = true;
                }
            }
            base.Dispose(disposing);
        }

        #endregion

        #region Read

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }
        public override int Read(Span<byte> buffer)
        {
            return 0;
        }

        #endregion
        #region ReadByte

        public override int ReadByte()
        {
            return 0;
        }

        #endregion

        #region Seek

        public override long Seek(long offset, SeekOrigin loc)
        {
            return 0;
        }

        #endregion

        #region SetLength

        public override void SetLength(long value)
        {

        }

        #endregion
    }
}
