using System;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

namespace SanityArchiver
{
    public partial class SanityExplorer : Form
    {
        protected DirectoryInfo currentDir;
        protected string fileToRename;

        public SanityExplorer()
        {
            InitializeComponent();
            contextMenuStrip1.Hide();
            PopulateTreeView();
            this.treeView1.NodeMouseClick +=
                new TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseClick);
            this.listView1.MouseClick += new MouseEventHandler(this.listView1_MouseClick);
        }

        private void PopulateTreeView()
        {
            TreeNode rootNode;
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach(DriveInfo drive in drives)
            {
                string dirString = (string) drive.Name;
                DirectoryInfo info = new DirectoryInfo(@dirString);
                if (info.Exists)
                {
                    rootNode = new TreeNode(info.Name);
                    rootNode.Tag = info;
                    treeView1.Nodes.Add(rootNode);
                }
            }
        }

        private void GetDirectories(DirectoryInfo[] subDirs, TreeNode nodeToAddTo)
        {
            TreeNode aNode;
            DirectoryInfo[] subSubDirs;
            foreach (DirectoryInfo subDir in subDirs)
            {
                aNode = new TreeNode(subDir.Name, 0, 0);
                aNode.Tag = subDir;
                aNode.ImageKey = "folder";
                try
                {
                    subSubDirs = subDir.GetDirectories();
                }
                catch (UnauthorizedAccessException exc) { }
                nodeToAddTo.Nodes.Add(aNode);
            }
        }

        void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeNode newSelected = e.Node;
            currentDir = new DirectoryInfo(newSelected.FullPath);
            listView1.Items.Clear();
            DirectoryInfo nodeDirInfo = (DirectoryInfo) newSelected.Tag;
            ListViewItem.ListViewSubItem[] subItems;
            ListViewItem item = null;

            DirectoryInfo[] nodeDirectories = null;
            FileInfo[] nodeFiles = null;

            try
            {
                if (newSelected.Nodes.Count == 0)
                {
                    GetDirectories(currentDir.GetDirectories(), newSelected);
                }
                nodeDirectories = nodeDirInfo.GetDirectories();
                nodeFiles = nodeDirInfo.GetFiles();
            }
            catch (UnauthorizedAccessException ex) { }

            if(nodeDirectories != null)
            {
                foreach (DirectoryInfo dir in nodeDirectories)
                {
                    item = new ListViewItem(dir.Name, 0);
                    item.Tag = dir;
                    subItems = new ListViewItem.ListViewSubItem[]
                              {new ListViewItem.ListViewSubItem(item, "Directory"),
                       new ListViewItem.ListViewSubItem(item,
                    dir.LastAccessTime.ToShortDateString())};
                    item.SubItems.AddRange(subItems);
                    listView1.Items.Add(item);
                }
            }

            if(nodeFiles != null)
            {
                foreach (FileInfo file in nodeFiles)
                {
                    item = new ListViewItem(file.Name, 1);
                    item.Tag = file;
                    subItems = new ListViewItem.ListViewSubItem[]
                              { new ListViewItem.ListViewSubItem(item, "File"),
                       new ListViewItem.ListViewSubItem(item,
                    file.LastAccessTime.ToShortDateString())};

                    item.SubItems.AddRange(subItems);
                    listView1.Items.Add(item);
                }
            }

            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var selected = listView1.SelectedItems[0].Tag;
                if (selected is FileInfo)
                {
                    if (listView1.FocusedItem.Bounds.Contains(e.Location) == true)
                    {
                        contextMenuStrip1.Show(Cursor.Position);
                    }
                }
            }
        }

        private void compressToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string selectedFile = listView1.SelectedItems[0].Text;
            FileInfo fileToCompress = new FileInfo(currentDir+"\\"+selectedFile);

            if (selectedFile.Substring(selectedFile.LastIndexOf('.')) != ".gz")
            {
                using (FileStream input = fileToCompress.OpenRead())
                {
                    using (FileStream output = File.Create(currentDir + "\\" + fileToCompress.Name + ".gz"))
                    {
                        using (GZipStream Compressor = new GZipStream(output, CompressionMode.Compress))
                        {
                            int b = input.ReadByte();
                            while (b != -1)
                            {
                                Compressor.WriteByte((byte)b);
                                b = input.ReadByte();
                            }
                        }
                    }
                }
                try
                {
                    fileToCompress.Delete();
                }
                catch(Exception exc) { }
            }
        }

        private void decompressToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string selectedFile = listView1.SelectedItems[0].Text;
            if (selectedFile.Substring(selectedFile.LastIndexOf('.')) == ".gz")
            {
                FileInfo fileToDecompress = new FileInfo(currentDir + "\\" + selectedFile);

                using (FileStream originalFileStream = fileToDecompress.OpenRead())
                {
                    string currentFileName = fileToDecompress.FullName;
                    string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

                    using (FileStream decompressedFileStream = File.Create(newFileName))
                    {
                        using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                        {
                            decompressionStream.CopyTo(decompressedFileStream);
                        }
                    }
                }
                try
                {
                    fileToDecompress.Delete();
                }
                catch(Exception exc) { }
            }
        }

        private void RenameFile(string fileToRename, string newName)
        {
            File.Move(fileToRename, newName);
        }

        private void renameFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.SelectedItems[0].BeginEdit();
        }

        private void listView1_BeforeLabelEdit(object sender, LabelEditEventArgs e)
        {
            string selectedFile = listView1.SelectedItems[0].Text;
            fileToRename = currentDir + "\\" + selectedFile;
        }

        private void listView1_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            string selectedFile = e.Label;
            string newName = currentDir + "\\" + selectedFile;
            try
            {
                RenameFile(fileToRename, newName);
            }
            catch(Exception ex)
            {
                e.CancelEdit = true;
            }
        }
    }
}
