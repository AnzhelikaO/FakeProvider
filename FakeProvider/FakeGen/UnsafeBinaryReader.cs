#region Using

using System;
using System.IO;
using System.Text;
using System.Runtime.CompilerServices;

#endregion

namespace FakeProvider.FakeGen
{
    /// <summary>
    /// Replaces the BinaryReader used by Terraria to speed up data read loading.
    /// Please do not use this class for any other purpose.
    /// </summary>
    public unsafe sealed class UnsafeBinaryReader : BinaryReader
    {
        #region Data

        public UnsafeReadOnlyMemoryStream UnsafeStream;

        #endregion
        #region Properties

        public byte* DataPtr { get => UnsafeStream.dataPtr; set => UnsafeStream.dataPtr = value; }
        
        #endregion

        #region Constructor

        public UnsafeBinaryReader(UnsafeReadOnlyMemoryStream stream) : base(stream)
        {
            UnsafeStream = stream;
        }

        #endregion

        #region PeekChar

        // Does not require override as all functions are already implemented.
        //public override int PeekChar()
        //{
        //    return base.PeekChar();
        //}

        #endregion
        #region Read

        public override int Read()
        {
            // Terraria does not currently use this method, so there is no need to implement it.
            // But if Terraria is going to use this method, we'll recognize it by mistake and add an implementation right away.
            throw new NotImplementedException();
        }

        #endregion

        #region ReadBoolean

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool ReadBoolean()
        {
            bool value = *(bool*)DataPtr;
            DataPtr += 1;
            return value;
        }

        #endregion
        #region ReadByte

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte ReadByte()
        {
            byte value = *DataPtr;
            DataPtr += 1;
            return value;
        }

        #endregion
        #region ReadSByte

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sbyte ReadSByte()
        {
            sbyte value = *(sbyte*)DataPtr;
            DataPtr += 1;
            return value;
        }

        #endregion

        #region ReadChar

        // Does not require override as all functions are already implemented.
        // If necessary, it will be necessary to implement the "int Read()" method
        //public override char ReadChar()
        //{
        //    return base.ReadChar();
        //}

        #endregion

        #region ReadInt16

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override short ReadInt16()
        {
            short value = *(short*)DataPtr;
            DataPtr += 2;
            return value;
        }

        #endregion
        #region ReadUInt16

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ushort ReadUInt16()
        {
            ushort value = *(ushort*)DataPtr;
            DataPtr += 2;
            return value;
        }
        #endregion

        #region ReadInt32

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int ReadInt32()
        {
            int value = *(int*)DataPtr;
            DataPtr += 4;
            return value;
        }

        #endregion
        #region ReadUInt32

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override uint ReadUInt32()
        {
            uint value = *(uint*)DataPtr;
            DataPtr += 4;
            return value;
        }

        #endregion

        #region ReadInt64

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long ReadInt64()
        {
            long value = *(long*)DataPtr;
            DataPtr += 8;
            return value;
        }

        #endregion
        #region ReadUInt64

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ulong ReadUInt64()
        {
            ulong value = *(ulong*)DataPtr;
            DataPtr += 8;
            return value;
        }

        #endregion

        #region ReadSingle

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override float ReadSingle()
        {
            float value = *(float*)DataPtr;
            DataPtr += 4;
            return value;
        }

        #endregion
        #region ReadDouble

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override double ReadDouble()
        {
            double value = *(double*)DataPtr;
            DataPtr += 8;
            return value;
        }

        #endregion
        #region ReadDecimal

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override decimal ReadDecimal()
        {
            decimal value = *(decimal*)DataPtr;
            DataPtr += 16;
            return value;
        }

        #endregion

        #region ReadString

        //TODO: bound checks, verify it doesn't break on weird scenarios
        //TODO: optimize, not much faster than BinaryReader.ReadString(); check ReadBytes();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ReadString()
        {
            int len = Read7BitEncodedInt();
            return Encoding.UTF8.GetString(ReadBytes(len));
        }

        #endregion

        #region Read(char[] buffer, int index, int count)

        public override int Read(char[] buffer, int index, int count)
        {
            // Terraria does not currently use this method, so there is no need to implement it.
            // But if Terraria is going to use this method, we'll recognize it by mistake and add an implementation right away.
            throw new NotImplementedException();
        }

        #endregion
        #region ReadChars

        public override char[] ReadChars(int count)
        {
            // Terraria does not currently use this method, so there is no need to implement it.
            // But if Terraria is going to use this method, we'll recognize it by mistake and add an implementation right away.
            throw new NotImplementedException();
        }

        #endregion
        #region Read(byte[] buffer, int index, int count)

        public override int Read(byte[] buffer, int index, int count)
        {
            // Terraria does not currently use this method, so there is no need to implement it.
            // But if Terraria is going to use this method, we'll recognize it by mistake and add an implementation right away.
            throw new NotImplementedException();
        }

        #endregion
        #region ReadBytes

        //TODO: optimize, not much faster than BinaryReader.Readbytes(); 
        //Frankly have no idea how this can be done faster. 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte[] ReadBytes(int count)
        {
            byte[] value = new byte[count];
            fixed (void* destinationPtr = value)
            {
                Unsafe.CopyBlock(destinationPtr, DataPtr, (uint)count);
            }
            DataPtr += count;
            return value;
        }

        #endregion

        #region FillBuffer

        protected override void FillBuffer(int numBytes)
        {
            // We do not have to BinaryReader to use this method.
            // Let's ignore it.

            // base.FillBuffer(numBytes);
        }

        #endregion

        #region Read7BitEncodedInt

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private new int Read7BitEncodedInt()
        {
            // Read out an Int32 7 bits at a time.  The high bit
            // of the byte when on means to continue reading more bytes.
            int count = 0;
            int shift = 0;
            byte b;
            do
            {
                // Check for a corrupted stream.  Read a max of 5 bytes.
                if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
                    throw new FormatException("Format_Bad7BitInt32");

                // TODO: verify if this can cause read beyond bounds for unexpected behaviour/bound check strings better
                b = ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }

        #endregion
    }
}
