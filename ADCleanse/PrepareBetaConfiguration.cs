namespace ADCleanse
{
    public class PrepareBetaConfiguration
    {
        public virtual string LdapRoot { get; set; }
        public virtual string Username { get; set; }
        public virtual string Password { get; set; }
        public virtual string InputFilePath { get; set; }
        public virtual string OutputFilePath { get; set; }
        public virtual string BetaGroupName { get; set; }
    }
}