using DokanNet;
using DokanNet.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Text;
using ZipFileSystem.ZipTree;
using FileAccess = DokanNet.FileAccess;
using static DokanNet.FormatProviders;
using System.Runtime.InteropServices;
using System.Linq;

namespace ZipFileSystem.Dokans
{
    class ZipDokanOperation : IDokanOperations
    {
        private ConsoleLogger logger = new ConsoleLogger("[ZipFileSystem] ");

        private const FileAccess DataAccess = FileAccess.ReadData | FileAccess.WriteData | FileAccess.AppendData |
                                      FileAccess.Execute |
                                      FileAccess.GenericExecute | FileAccess.GenericWrite |
                                      FileAccess.GenericRead;

        private const FileAccess DataWriteAccess = FileAccess.WriteData | FileAccess.AppendData |
                                                   FileAccess.Delete |
                                                   FileAccess.GenericWrite;

        private const FileAccess WriteChangeAccess = FileAccess.WriteData | FileAccess.AppendData |
            FileAccess.WriteExtendedAttributes | FileAccess.DeleteChild | FileAccess.WriteAttributes |
            FileAccess.Delete | FileAccess.ChangePermissions | FileAccess.SetOwnership | FileAccess.GenericWrite;

        private static readonly DateTime DefaultCreationTime = new DateTime(0);
        private static readonly DateTime DefaultLastAccessTime = new DateTime(0);

        private FileTreeNode mRoot;

        public ZipDokanOperation(FileTreeNode root)
        {
            mRoot = root;
        }

        public void Cleanup(string fileName, IDokanFileInfo info)
        {
            (info.Context as Stream)?.Close();
            info.Context = null;
            Trace(nameof(Cleanup), fileName, info, DokanResult.Success);
        }

        public void CloseFile(string fileName, IDokanFileInfo info)
        {
            (info.Context as Stream)?.Dispose();
            info.Context = null;
            Trace(nameof(CloseFile), fileName, info, DokanResult.Success);
        }

        public NtStatus CreateFile(string fileName, FileAccess access, FileShare share, FileMode mode, FileOptions options,
            FileAttributes attributes, IDokanFileInfo info)
        {
            var result = DokanResult.Success;
            var fileNode = FindFileNode(fileName, info);

            if (info.IsDirectory)
            {
                switch (mode)
                {
                    case FileMode.Open:
                        if (fileNode == null)
                        {
                            return Trace(nameof(CreateFile), fileName, info, access, share, mode, options,
                                attributes, DokanResult.PathNotFound);
                        }
                        else if (!fileNode.IsDirectory())
                        {
                            return Trace(nameof(CreateFile), fileName, info, access, share, mode, options,
                                attributes, DokanResult.NotADirectory);
                        }

                        break;

                    case FileMode.CreateNew:
                        return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes,
                            DokanResult.AccessDenied);
                }
            }
            else
            {
                var pathExists = fileNode != null;
                var pathIsDirectory = fileNode == null ? info.IsDirectory : fileNode.IsDirectory();

                var readWriteAttributes = (access & DataAccess) == 0;
                var readAccess = (access & DataWriteAccess) == 0;
                var writeOrChange = (access & WriteChangeAccess) != 0;

                switch (mode)
                {
                    case FileMode.Open:

                        if (pathExists)
                        {
                            info.IsDirectory = pathIsDirectory;
                            info.Context = new object();

                            if (writeOrChange)
                            {
                                return Trace(nameof(CreateFile), fileName, info, access, share, mode, options,
                                        attributes, DokanResult.AccessDenied);
                            }
                        }
                        else
                        {
                            return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes,
                                DokanResult.FileNotFound);
                        }
                        break;

                    case FileMode.CreateNew:
                        if (pathExists)
                            return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes,
                                DokanResult.FileExists);
                        else
                            return Trace(nameof(CreateFile), fileName, info, access, share, mode, options,
                                        attributes, DokanResult.AccessDenied);

                    case FileMode.Truncate:
                        if (!pathExists)
                            return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes,
                                DokanResult.FileNotFound);
                        else
                            return Trace(nameof(CreateFile), fileName, info, access, share, mode, options,
                                        attributes, DokanResult.AccessDenied);
                }

                info.Context = fileNode.mNodeEntry.Open();

                if (pathExists && (mode == FileMode.OpenOrCreate || mode == FileMode.Create))
                    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, DokanResult.AlreadyExists);

                bool fileCreated = mode == FileMode.CreateNew || mode == FileMode.Create || (!pathExists && mode == FileMode.OpenOrCreate);
                if (fileCreated)
                {
                    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, DokanResult.AccessDenied);
                }
            }
            return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, result);
        }

        public NtStatus DeleteDirectory(string fileName, IDokanFileInfo info)
        {
            return Trace(nameof(DeleteDirectory), fileName, info, DokanResult.AccessDenied);
        }

        public NtStatus DeleteFile(string fileName, IDokanFileInfo info)
        {
            return Trace(nameof(DeleteFile), fileName, info, DokanResult.AccessDenied);
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, IDokanFileInfo info)
        {
            // This function is not called because FindFilesWithPattern is implemented
            // Return DokanResult.NotImplemented in FindFilesWithPattern to make FindFiles called
            files = FindFilesHelper(fileName, info, "*");

            return Trace(nameof(FindFiles), fileName, info, DokanResult.Success);
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, IDokanFileInfo info)
        {
            files = FindFilesHelper(fileName, info, searchPattern);

            return Trace(nameof(FindFilesWithPattern), fileName, info, DokanResult.Success);
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
        {
            streams = new FileInformation[0];
            return Trace(nameof(FindStreams), fileName, info, DokanResult.NotImplemented);
        }

        public NtStatus FlushFileBuffers(string fileName, IDokanFileInfo info)
        {
            return Trace(nameof(FlushFileBuffers), fileName, info, DokanResult.DiskFull);
        }

        public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes, IDokanFileInfo info)
        {
            freeBytesAvailable = 0;
            totalNumberOfBytes = 1024 * 1024 * 1024; // TODO: use zip file uncompressed total size
            totalNumberOfFreeBytes = 0;
            return Trace(nameof(GetDiskFreeSpace), null, info, DokanResult.Success);
        }

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, IDokanFileInfo info)
        {
            // may be called with info.Context == null, but usually it isn't
            var fileNode = FindFileNode(fileName, info).mNodeEntry;

            fileInfo = new FileInformation
            {
                FileName = fileName,
                Attributes = info.IsDirectory ? FileAttributes.Directory : FileAttributes.ReadOnly,
                CreationTime = DefaultCreationTime,
                LastAccessTime = DefaultLastAccessTime,
                LastWriteTime = new DateTime(fileNode.LastWriteTime.Ticks),
                Length = fileNode.Length,
            };
            return Trace(nameof(GetFileInformation), fileName, info, DokanResult.Success);
        }

        NtStatus IDokanOperations.GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
        {
            security = null;
            return DokanResult.NotImplemented;
        }

        public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, out uint maximumComponentLength, IDokanFileInfo info)
        {
            volumeLabel = "ZipFileSystem";
            fileSystemName = "NTFS";
            maximumComponentLength = 256;

            features = FileSystemFeatures.CasePreservedNames | FileSystemFeatures.CaseSensitiveSearch |
                       FileSystemFeatures.PersistentAcls | FileSystemFeatures.SupportsRemoteStorage |
                       FileSystemFeatures.UnicodeOnDisk;

            return Trace(nameof(GetVolumeInformation), null, info, DokanResult.Success, "out " + volumeLabel,
                "out " + features.ToString(), "out " + fileSystemName);
        }

        public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            return Trace(nameof(LockFile), fileName, info, DokanResult.Success, offset, length);
        }

        public NtStatus Mounted(IDokanFileInfo info)
        {
            return Trace(nameof(Mounted), null, info, DokanResult.Success);
        }

        public NtStatus MoveFile(string oldName, string newName, bool replace, IDokanFileInfo info)
        {
            return Trace(nameof(MoveFile), oldName, info, DokanResult.AccessDenied, newName, replace.ToString());
        }

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
        {
            Stream stream;

            if (info.Context == null)
            {
                var fileNode = FindFileNode(fileName, info);

                stream = fileNode.mNodeEntry.Open();

                // not sure if we can do this
                info.Context = stream;
            }
            else
            {
                stream = info.Context as Stream;
            }

            lock (stream) //Protect from overlapped read
            {
                if (stream.CanSeek)
                {
                    stream.Position = offset;
                }
                else
                {
                    if (offset > 0)
                    {
                        var discard = new byte[offset];
                        stream.Read(discard, 0, (int)offset);
                    }
                }

                bytesRead = stream.Read(buffer, 0, buffer.Length);
            }

            return Trace(nameof(ReadFile), fileName, info, DokanResult.Success, "out " + bytesRead.ToString(),
                offset.ToString());
        }

        public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info)
        {
            return Trace(nameof(SetAllocationSize), fileName, info, ChangeFile(fileName, info), length);
        }

        public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info)
        {
            return Trace(nameof(SetEndOfFile), fileName, info, ChangeFile(fileName, info), length);
        }

        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info)
        {
            return Trace(nameof(SetFileAttributes), fileName, info, ChangeFile(fileName, info), attributes.ToString());
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
        {
            return DokanResult.NotImplemented;
        }

        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, IDokanFileInfo info)
        {
            var result = ChangeFile(fileName, info);
            return Trace(nameof(SetFileTime), fileName, info, result, creationTime, lastAccessTime, lastWriteTime);
        }

        private NtStatus ChangeFile(string fileName, IDokanFileInfo info)
        {
            var fileNode = FindFileNode(fileName, info);
            if (fileNode == null && info.IsDirectory)
                return DokanResult.PathNotFound;
            else if (fileNode == null && !info.IsDirectory)
                return DokanResult.FileNotFound;
            else
                return DokanResult.AccessDenied;
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            return Trace(nameof(UnlockFile), fileName, info, DokanResult.Success, offset, length);
        }

        public NtStatus Unmounted(IDokanFileInfo info)
        {
            return Trace(nameof(Unmounted), null, info, DokanResult.Success);
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, IDokanFileInfo info)
        {
            bytesWritten = 0;
            return Trace(nameof(WriteFile), fileName, info, DokanResult.DiskFull, "out " + bytesWritten.ToString(), offset.ToString());
        }

        private NtStatus Trace(string method, string fileName, IDokanFileInfo info,
            FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes,
            NtStatus result)
        {
#if TRACE
            logger.Debug(
                DokanFormat(
                    $"{method}('{fileName}', {info}, [{access}], [{share}], [{mode}], [{options}], [{attributes}]) -> {result}"));
#endif

            return result;
        }

        protected NtStatus Trace(string method, string fileName, IDokanFileInfo info, NtStatus result, params object[] parameters)
        {
#if TRACE
            var extraParameters = parameters != null && parameters.Length > 0
                ? ", " + string.Join(", ", parameters.Select(x => string.Format(DefaultFormatProvider, "{0}", x)))
                : string.Empty;

            logger.Debug(DokanFormat($"{method}('{fileName}', {info}{extraParameters}) -> {result}"));
#endif

            return result;
        }

        /// <summary>
        /// File a node from mRoot.
        /// </summary>
        /// <param name="dokanPath">The file path from dokan</param>
        /// <param name="info">The file info object from dokan</param>
        /// <returns></returns>
        private FileTreeNode FindFileNode(string dokanPath, IDokanFileInfo info)
        {
            var generalSlashPath = dokanPath.Replace('\\', '/');
            generalSlashPath = info.IsDirectory && !generalSlashPath.EndsWith("/") ? generalSlashPath + "/" : generalSlashPath;
            return mRoot.FindByName(generalSlashPath);
        }

        public IList<FileInformation> FindFilesHelper(string fileName, IDokanFileInfo info, string searchPattern)
        {
            return FindFileNode(fileName, info)
                .mChildren
                .Where(finfo => DokanHelper.DokanIsNameInExpression(searchPattern, finfo.mNodeEntry.Name, true))
                .Select(finfo => new FileInformation
                {
                    Attributes = finfo.IsDirectory() ? FileAttributes.Directory : FileAttributes.ReadOnly,
                    CreationTime = new DateTime(0),
                    LastAccessTime = DefaultCreationTime,
                    LastWriteTime = DefaultLastAccessTime,
                    Length = finfo.mNodeEntry.Length,
                    FileName = finfo.mNodeEntry.Name
                }).ToArray();
        }
    }
}
