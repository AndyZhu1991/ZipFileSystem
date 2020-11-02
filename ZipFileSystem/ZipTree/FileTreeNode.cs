using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;

namespace ZipFileSystem.ZipTree
{
    class FileTreeNode
    {
        public ZipArchiveEntry mNodeEntry;
        public List<FileTreeNode> mChildren;
        public FileTreeNode mParent;

        public FileTreeNode(ZipArchiveEntry entry)
        {
            mNodeEntry = entry;
            if (IsDirectory())
            {
                mChildren = new List<FileTreeNode>();
            }
        }

        public FileTreeNode FindByName(string name)
        {
            if (mNodeEntry != null && FullName() == name)
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

        public FileTreeNode FindByEntry(ZipArchiveEntry entry)
        {
            if (mNodeEntry == entry)
            {
                return this;
            }
            else
            {
                foreach (FileTreeNode child in mChildren)
                {
                    var finded = child.FindByEntry(entry);
                    if (finded != null)
                    {
                        return finded;
                    }
                }
                return null;
            }
        }

        public bool IsDirectory()
        {
            if (mNodeEntry == null)
            {
                return true;
            }
            return mNodeEntry.FullName.EndsWith("/");
        }

        private string FullName()
        {
            if (mNodeEntry == null)
            {
                return "";
            }
            return mNodeEntry.FullName;
        }

        public bool Insert(ZipArchiveEntry entry)
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
    }
}
