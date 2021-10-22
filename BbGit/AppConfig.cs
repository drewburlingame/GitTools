namespace BbGit
{
    public class AppConfig
    {
        public enum AuthTypes
        {
            Basic,
            OAuth
        }

        // !!! update config.example when changing properties

        public AuthTypes AuthType { get; set; }
        public string Username { get; set; }
        public string AppPassword { get; set; }
        public string DefaultAccount { get; set; }
        public string BaseUrl { get; set; }
        public int CacheTTLInDays { get; set; } = 1;
    }
}