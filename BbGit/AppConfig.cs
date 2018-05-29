﻿namespace BbGit
{
    public class AppConfig
    {
        public enum AuthTypes
        {
            Basic
        }

        public AuthTypes AuthType { get; set; }
        public string Username { get; set; }
        public string AppPassword { get; set; }
        public string DefaultAccount { get; set; }
        public bool SetOriginToSsh { get; set; }
    }
}