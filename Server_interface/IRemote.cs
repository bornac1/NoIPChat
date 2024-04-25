namespace Server_interface
{
    public interface IRemote
    {
        public void Close();
        public Task SendLog(string message);
    }
}
