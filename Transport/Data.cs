namespace Transport
{
    /// <summary>
    /// Available protocols.
    /// </summary>
    public enum Protocol
    {
        /// <summary>
        /// TCP
        /// </summary>
        TCP = 0,
        /// <summary>
        /// Reticulum network stack.
        /// Not implemented.
        /// </summary>
        Reticulum = 1,
        /// <summary>
        /// Unknown protocol.
        /// </summary>
        Other = -1
    }
}
