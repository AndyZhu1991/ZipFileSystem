using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace ZipFileSystem.ZipTree
{
    class ZipEntry : IZipEntry
    {
        private ZipArchiveEntry mEntry;

        public ZipEntry(ZipArchiveEntry entry)
        {
            mEntry = entry;
        }

        public long CompressedLength => mEntry.CompressedLength;

        public string FullName => GetFullName();

        public DateTimeOffset LastWriteTime => mEntry.LastWriteTime;

        public long Length => mEntry.Length;

        public string Name => GetName();

        public Stream Open()
        {
            var zipStream = mEntry.Open();
            var memStream = new MemoryStream();
            zipStream.CopyTo(memStream);
            zipStream.Dispose();
            return memStream;
        }

        public string GetFullName()
        {
            if (mEntry.FullName.StartsWith('/'))
            {
                return mEntry.FullName;
            }
            else
            {
                return "/" + mEntry.FullName;
            }
        }

        private string GetName()
        {
            if (mEntry.Name.Length != 0)
                return mEntry.Name;
            return mEntry.FullName.Split('/').LastOrDefault(s => s.Length > 0);
        }
    }

    class CustomZipEntry : IZipEntry
    {
        private long mCompressedLength;
        private string mFullName;
        private DateTimeOffset mLastWriteTime;
        private long mLength;
        private string mName;
        private Stream mStream;

        public CustomZipEntry(long compressedLength, string fullName, DateTimeOffset lastWriteTime,
            long length, string name, Stream stream)
        {
            mCompressedLength = compressedLength;
            mFullName = fullName;
            mLastWriteTime = lastWriteTime;
            mLength = length;
            mName = name;
            mStream = stream;
        }

        public long CompressedLength => mCompressedLength;

        public string FullName => mFullName;

        public DateTimeOffset LastWriteTime => mLastWriteTime;

        public long Length => mLength;

        public string Name => mName;

        public Stream Open()
        {
            return mStream;
        }
    }

    interface IZipEntry
    {
        public long CompressedLength { get; }

        /// <summary>
        /// Always starts with "/", if it is a directory, end with "/".
        /// For example: "/path/to/file.txt" or "/path/to/dir/"
        /// </summary>
        public string FullName { get; }

        public DateTimeOffset LastWriteTime { get; }

        public long Length { get; }

        /// <summary>
        /// The name of file or directory, not include any "/".
        /// But for root directory, the name is "/".
        /// </summary>
        public string Name { get; }

        public Stream Open();
    }
}
