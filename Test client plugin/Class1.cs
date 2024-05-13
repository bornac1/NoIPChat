using System.Windows.Forms;
using Client;

namespace Test_client_plugin
{
    public class Class1 : IPlugin
    {
        public Client.Client? Client { get; set; }
        public void Initialize()
        {
            ToolStripMenuItem menuitem = new()
            {
                Text = "Test"
            };
            menuitem.Click += Menuitem_Click;
            Client?.AddMainMenu(menuitem);
        }

        private void Menuitem_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Test plugin");
        }
    }
}
