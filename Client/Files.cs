using System.ComponentModel;

namespace Client
{
    public partial class Files : Form
    {
        private readonly Main main;
        private readonly BindingSource files;
        int rowindex = -1;
        public Files(Main main)
        {
            this.main = main;
            files = [];
            InitializeComponent();
            grid.DataSource = files;
            files.ListChanged += Files_ListChanged;
            UpdateFiles();
        }
        private void UpdateFiles()
        {
            files.Clear();
            string[] filesname = Directory.GetFiles("Data");
            foreach (string file in filesname)
            {
                files.Add(new FileInfo() { Name = Path.GetFileName(file), Path = Path.GetFullPath(file) });
            }
            files.ResetBindings(false);
            grid.Refresh();
            rowindex = -1;
        }

        private void DeleteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (rowindex >= 0)
            {
                FileInfo file = (FileInfo)files[rowindex];
                File.Delete(file.Path);
                UpdateFiles();
            }
        }

        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            rowindex = e.RowIndex;
        }
        private void Files_ListChanged(object? sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemChanged)
            {
                UpdateFiles();
            }
        }

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FileInfo file = (FileInfo)files[rowindex];
            _ = savefiledialog.ShowDialog();
            string path = savefiledialog.FileName;
            File.Move(file.Path, path);
            UpdateFiles();
        }
    }
}
