using Flurl;
using System;
using System.Collections.Generic;

namespace Kill_Proof_Module
{
    /// <summary>
    /// JSON class for replies from https://killproof.me/api/
    /// </summary>
    public class KillProof
    {
        public string error { get; set; }
        public Dictionary<string, string> titles { get; set; }
        public string account_name { get; set; }
        public DateTime last_refresh { get; set; }
        public Url proof_url { get; set; }
        public string kpid { get; set; }
        public Dictionary<string, int> tokens { get; set; }
        public Dictionary<string, int> killproofs { get; set; }

    }
}
