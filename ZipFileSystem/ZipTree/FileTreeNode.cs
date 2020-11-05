using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;

namespace ZipFileSystem.ZipTree
{
    class FileTreeNode
    {
        public IZipEntry mNodeEntry;
        public List<FileTreeNode> mChildren = new List<FileTreeNode>();
        public FileTreeNode mParent;

        public FileTreeNode(ZipArchiveEntry entry)
        {
            mNodeEntry = new ZipEntry(entry);
        }

        public FileTreeNode(IZipEntry entry)
        {
            mNodeEntry = entry;
        }

        /// <summary>
        /// Find a node by name.
        /// </summary>
        /// <param name="name">Standard full name used in this project, look IZipEntry.FullName</param>
        /// <returns>The FileTreeNode instance or null</returns>
        public FileTreeNode FindByName(string name)
        {
            if (IsSamePath(FullName(), name))
            {
                return this;
            }
            else if (!IsDirectory())
            {
                return null;
            }
            else
            {
                foreach (FileTreeNode child in mChildren)
                {
                    if (IsSamePath(name, child.FullName()))
                    {
                        return child;
                    }
                    if (name.StartsWith(child.FullName()))
                    {
                        return child.FindByName(name);
                    }
                }
                return null;
            }
        }

        public bool IsDirectory()
        {
            return mNodeEntry.FullName.EndsWith("/");
        }

        private string FullName()
        {
            return mNodeEntry.FullName;
        }

        private bool CreateNode(IZipEntry entry)
        {
            // create internal node
            var currentNode = this;
            var pathBuilder = new StringBuilder(FullName());
            var nameBuilder = new StringBuilder();

            foreach (char c in entry.FullName.Substring(FullName().Length))
            {
                pathBuilder = pathBuilder.Append(c);
                if (c == '/')
                {
                    var dirEntry = new CustomZipEntry(
                        0,
                        pathBuilder.ToString(),
                        entry.LastWriteTime,
                        0,
                        nameBuilder.ToString(),
                        null
                    );

                    var node = new FileTreeNode(dirEntry);
                    currentNode.mChildren.Add(node);
                    node.mParent = currentNode;
                    currentNode = node;

                    nameBuilder = nameBuilder.Clear();
                }
                else
                {
                    nameBuilder = nameBuilder.Append(c);
                }
            }

            // create leaf node for the zip entry
            if (!entry.FullName.EndsWith('/'))
            {
                var node = new FileTreeNode(entry);
                currentNode.mChildren.Add(node);
                node.mParent = currentNode;
            }

            return true;
        }

        public bool Insert(IZipEntry entry)
        {
            if (IsDirectory() && entry.FullName.StartsWith(FullName()))
            {
                foreach (FileTreeNode child in mChildren)
                {
                    if (child.Insert(entry))
                    {
                        return true;
                    }
                }

                return CreateNode(entry);
            }
            else
            {
                return false;
            }
        }


        private static bool IsSamePath(string path1, string path2)
        {
            return RemoveEnddingSlash(path1).Equals(RemoveEnddingSlash(path2));
        }

        private static string RemoveEnddingSlash(string path)
        {
            if (path.EndsWith("/"))
                return path.Substring(0, path.Length - 1);
            else
                return path;
        }

        public static FileTreeNode CreateRootNode()
        {
            IZipEntry entry = new CustomZipEntry(0, "/", DateTimeOffset.FromFileTime(0), 0, "/", null);
            return new FileTreeNode(entry);
        }
    }
}
