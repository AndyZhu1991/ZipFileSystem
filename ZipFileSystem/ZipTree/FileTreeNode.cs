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
            if (FullName().Equals(name))
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
                var node = new FileTreeNode(entry);
                mChildren.Add(node);
                node.mParent = this;
                return true;
            }
            else
            {
                return false;
            }
        }

        public static FileTreeNode CreateRootNode()
        {
            IZipEntry entry = new CustomZipEntry(0, "/", DateTimeOffset.FromFileTime(0), 0, "/", null);
            return new FileTreeNode(entry);
        }
    }
}
