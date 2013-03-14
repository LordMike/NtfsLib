using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NTFSLib.Compression;
using NTFSLib.Objects;

namespace NTFSLib
{
    public class NtfsDiskStream : Stream
    {
        private LZNT1 _compressor;

        private readonly NTFS _ntfs;
        private readonly Stream _diskStream;
        private readonly ushort _compressionClusterCount;
        private readonly DataFragment[] _fragments;
        private long _position;
        private long _length;

        private bool IsEof
        {
            get { return _position >= _length; }
        }

        internal NtfsDiskStream(NTFS ntfs, Stream diskStream, DataFragment[] fragments, ushort compressionClusterCount, long length)
        {
            _ntfs = ntfs;
            _diskStream = diskStream;
            _compressionClusterCount = compressionClusterCount;
            _fragments = fragments.OrderBy(s => s.StartingVCN).ToArray();

            _length = length;
            _position = 0;

            _compressor = new LZNT1();
            _compressor.BlockSize = (int)ntfs.BytesPrCluster;

            long vcn = 0;
            for (int i = 0; i < _fragments.Length; i++)
            {
                Debug.Assert(_fragments[i].StartingVCN == vcn);
                vcn += _fragments[i].Clusters + _fragments[i].CompressedClusters;
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition = offset;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = offset;
                    break;
                case SeekOrigin.Current:
                    newPosition += offset;
                    break;
                case SeekOrigin.End:
                    newPosition = _length + offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("origin");
            }

            if (newPosition < 0 || _length < newPosition)
                throw new ArgumentOutOfRangeException("offset");

            // Set 
            _position = newPosition;

            return _position;
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalRead = 0;

            // Determine fragment
            while (count > 0 && _position < _length)
            {
                long fragmentOffset;
                DataFragment fragment = FindFragment(_position, out fragmentOffset);

                long diskOffset = fragment.LCN * _ntfs.BytesPrCluster;
                long fragmentLength = fragment.Clusters * _ntfs.BytesPrCluster;

                int actualRead;
                if (fragment.IsCompressed)
                {
                    // Read and decompress
                    byte[] compressedData = new byte[fragmentLength];
                    _diskStream.Position = diskOffset;
                    _diskStream.Read(compressedData, 0, compressedData.Length);

                    int decompressedLength = (int)((fragment.Clusters + fragment.CompressedClusters) * _ntfs.BytesPrCluster);
                    int toRead = (int)Math.Min(decompressedLength - fragmentOffset, Math.Min(_length - _position, count));

                    Debug.Assert(decompressedLength == _compressionClusterCount * _ntfs.BytesPrCluster);

                    if (fragmentOffset == 0 && toRead == decompressedLength)
                    {
                        // Decompress directly (we're in the middle of a file and reading a full 16 clusters out)
                        actualRead = _compressor.Decompress(compressedData, 0, compressedData.Length, buffer, offset);
                    }
                    else
                    {
                        // Decompress temporarily
                        byte[] tmp = new byte[decompressedLength];
                        int decompressed = _compressor.Decompress(compressedData, 0, compressedData.Length, tmp, 0);

                        toRead = Math.Min(toRead, decompressed);

                        // Copy wanted data
                        Array.Copy(tmp, fragmentOffset, buffer, offset, toRead);

                        actualRead = toRead;
                    }
                }
                else if (fragment.IsSparseFragment)
                {
                    // Fill with zeroes
                    // How much to fill?
                    int toFill = (int)Math.Min(fragmentLength - fragmentOffset, count);

                    Array.Clear(buffer, offset, toFill);

                    actualRead = toFill;
                }
                else
                {
                    // Read directly
                    // How much can we read?
                    int toRead = (int)Math.Min(fragmentLength - fragmentOffset, Math.Min(_length - _position, count));

                    // Read it
                    _diskStream.Position = diskOffset + fragmentOffset;
                    actualRead = _diskStream.Read(buffer, offset, toRead);
                }

                // Increments
                count -= actualRead;
                offset += actualRead;

                _position += actualRead;

                totalRead += actualRead;

                // Check
                if (actualRead == 0)
                    break;
            }

            return totalRead;
        }

        private DataFragment FindFragment(long fileIndex, out long offsetInFragment)
        {
            for (int i = 0; i < _fragments.Length; i++)
            {
                long fragmentStart = _fragments[i].StartingVCN * _ntfs.BytesPrCluster;
                long fragmentEnd = fragmentStart + (_fragments[i].Clusters + _fragments[i].CompressedClusters) * _ntfs.BytesPrCluster;

                if (fragmentStart <= fileIndex && fileIndex < fragmentEnd)
                {
                    // Found
                    offsetInFragment = fileIndex - fragmentStart;

                    return _fragments[i];
                }
            }

            offsetInFragment = -1;

            return null;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // Not implemented
            throw new InvalidOperationException();
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
            // Not implemented
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
                if (value < 0 || _length < value)
                    throw new ArgumentOutOfRangeException("value");

                _position = value;
            }
        }
    }
}
