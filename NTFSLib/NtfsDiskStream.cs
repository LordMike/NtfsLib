using System;
using System.Diagnostics;
using System.IO;
using NTFSLib.Objects;

namespace NTFSLib
{
    public class NtfsDiskStream : Stream
    {
        private readonly NTFS _ntfs;
        private readonly DataFragment[] _fragments;
        private int _positionFragment;
        private long _position;
        private long _length;

        internal NtfsDiskStream(NTFS ntfs, DataFragment[] fragments, long length)
        {
            _ntfs = ntfs;
            _fragments = fragments;
            _length = length;

            SetPosition(0);
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition = _position;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = offset;
                    break;
                case SeekOrigin.Current:
                    newPosition += offset;
                    break;
                case SeekOrigin.End:
                    newPosition = _length - offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("origin");
            }

            if (IsLocationValid(newPosition))
            {
                SetPosition(newPosition);
            }
            else if (newPosition < 0)
            {
                SetPosition(0);
            }
            else if (newPosition >= _length)
            {
                SetPosition(_length);
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count > _length - _position)
                throw new ArgumentOutOfRangeException("count");

            if (count <= 0)
                throw new ArgumentOutOfRangeException("count");

            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (offset + count > buffer.Length)
                throw new ArgumentException("count");

            // Read fragments
            int read = 0;
            while (count > 0)
            {
                DataFragment frag = _fragments[_positionFragment];
                ulong fragOffset = frag.StartingVCN * _ntfs.BytesPrCluster - (ulong)_position;
                int getLength = Math.Min((int)(frag.ClusterCount * _ntfs.BytesPrCluster), count);

                ulong diskOffset = frag.LCN * _ntfs.BytesPrCluster + fragOffset;

                // Get 
                if (!_ntfs.Provider.CanReadBytes(diskOffset, getLength))
                    throw new InvalidOperationException("Unable to read bytes " + diskOffset + "->" + (diskOffset + (ulong)getLength));

                // TODO: Make reader that can take a target byte array as input
                byte[] data = _ntfs.Provider.ReadBytes(diskOffset, getLength);

                Array.Copy(data, 0, buffer, offset, getLength);

                count -= getLength;
                offset += getLength;

                read += getLength;

                SetPosition(_position + getLength);
            }

            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _length; }
        }

        public override long Position
        {
            get { return _position; }
            set
            {
                if (IsLocationValid(value))
                    throw new ArgumentOutOfRangeException("position");

                SetPosition(value);
            }
        }

        private bool IsLocationValid(long position)
        {
            return 0 <= position && position <= _length;
        }

        private void SetPosition(long newPosition)
        {
            Debug.Assert(IsLocationValid(newPosition));

            ulong position = (ulong)newPosition;
            _positionFragment = -1;

            // Find fragment
            for (int i = 0; i < _fragments.Length; i++)
            {
                DataFragment frag = _fragments[i];
                ulong start = frag.StartingVCN * _ntfs.BytesPrCluster;
                ulong length = frag.ClusterCount * _ntfs.BytesPrCluster;

                if (start <= position && start + length > position)
                {
                    // Found it
                    _position = newPosition;
                    _positionFragment = i;
                    return;
                }
            }

            Debug.Fail("Unreachable!");
        }
    }
}
