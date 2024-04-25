using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_interface
{
    public interface IRemote
    {
        public void Close();
        public Task SendLog(string message);
    }
}
