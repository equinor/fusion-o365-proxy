namespace Fusion.O365Proxy.Authorization
{
    /// <summary>
    /// Mailbox identifier. Should be a valid mail address.
    /// </summary>
    public struct MailboxIdentifier
    {
        public MailboxIdentifier(string mailAddress)
        {
            Mail = mailAddress;
        }

        /// <summary>
        /// Mailbox identifier. Should be a valid mail address.
        /// </summary>
        public string Mail { get; set; }
    }
}
