using System;
using System.Diagnostics;
using System.IO;
using NTFSLib.Objects;
using System.Linq;

namespace NTFSLib
{
    public class NtfsDiskStream : Stream
    {
        private readonly NTFS _ntfs;
        private readonly DataFragment[] _fragments;
        private int _positionFragment;
        private long _position;
        private long _length;

        private bool IsEof
        {
            get { return _position >= _length; }
        }

        internal NtfsDiskStream(NTFS ntfs, DataFragment[] fragments, long length)
        {
            _ntfs = ntfs;
            _fragments = fragments.OrderBy(s => s.StartingVCN).ToArray();
            _length = length;

            long vcn = 0;
            for (int i = 0; i < _fragments.Length; i++)
            {
                Debug.Assert(_fragments[i].StartingVCN == vcn);
                vcn += _fragments[i].Clusters;// +_fragments[i].CompressedClusters;     // Todo: Handle compressed clusters
            }

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
            if (count <= 0)
                throw new ArgumentOutOfRangeException("count");

            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (offset + count > buffer.Length)
                throw new ArgumentException("count");

            // Read fragments
            int read = 0;
            while (count > 0 && !IsEof)
            {
                DataFragment frag = _fragments[_positionFragment];
                long fragOffset = _position - frag.StartingVCN * _ntfs.BytesPrCluster;
                int getLength = (int)Math.Min(_length - _position, Math.Min((int)(frag.Clusters * _ntfs.BytesPrCluster), count));

                if (getLength <= 0)
                    break;

                if (_fragments[_positionFragment].IsSparseFragment)
                {
                    // Simulate reading
                }
                else
                {
                    // Actually read
                    long diskOffset = frag.LCN * _ntfs.BytesPrCluster + fragOffset;

                    // Get 
                    if (!_ntfs.Provider.CanReadBytes((ulong)diskOffset, getLength))
                        throw new InvalidOperationException("Unable to read bytes " + diskOffset + "->" + (diskOffset + getLength));

                    _ntfs.Provider.ReadBytes(buffer, offset, (ulong)diskOffset, getLength);
                }

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
                if (!IsLocationValid(value))
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

            if (newPosition == _length)
            {
                // EOF
                _position = newPosition;
                _positionFragment = -1;
            }
            else
            {
                ulong position = (ulong)newPosition;
                _positionFragment = -1;

                // Find fragment
                for (int i = 0; i < _fragments.Length; i++)
                {
                    DataFragment frag = _fragments[i];
                    ulong start = (ulong)(frag.StartingVCN * _ntfs.BytesPrCluster);
                    ulong length = (ulong)(frag.Clusters * _ntfs.BytesPrCluster);

                    if (start <= position && position < start + length)
                    {
                        // Found it
                        _position = newPosition;
                        _positionFragment = i;
                        return;
                    }
                }

                Debug.Fail("Unreachable code!");
            }
        }
    }
}
