using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class ServersForm : Form
    {
        private readonly Main main;
        public ServersForm(Main main)
        {
            this.main = main;
            InitializeComponent();
            grid.DataSource = main.client.servers;
            main.client.servers.ListChanged += Servers_ListChanged;
            grid.RowsRemoved += Grid_RowesRemoved;
        }
        private async Task UpdateFile()
        {
            await main.client.SaveServers();
            await main.client.LoadServers();
            main.client.servers.ResetBindings(false);
            grid.Refresh();
        }
        private async void Servers_ListChanged(object? sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemChanged)
            {
                await UpdateFile();
            }
        }
        private async void Grid_RowesRemoved(object? sender, DataGridViewRowsRemovedEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex <grid.Rows.Count)
            {
                await UpdateFile();
            }
        }
    }
}
