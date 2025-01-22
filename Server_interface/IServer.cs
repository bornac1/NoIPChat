namespace Server_interface
{
    public delegate Task WriteLogAsync(string message);
    public interface IServer
    {
        TaskCompletionSource<bool> Closed { get; set; }
        Task Close();
        public WriteLogAsync? Writelogasync { get; set; }
        public void LoadPatch(string path);
        public Task SaveSneakernet(string path, string name);
        public Task LoadSneakernet(string path);
        public Task StartDiscovery();
    }
}
