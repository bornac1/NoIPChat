namespace Client
{
    /// <summary>
    /// FileInfo object.
    /// </summary>
    public struct FileInfo
    {
        /// <summary>
        /// Name of the file.
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// Path.
        /// </summary>
        public required string Path { get; set; }
    }
}
