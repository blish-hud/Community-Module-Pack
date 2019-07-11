using Flurl;
using System;
using System.Collections.Generic;

namespace Kill_Proof_Module
{
    public class KillProof
    {
        public Dictionary<string, string> titles { get; set; }
        public string account_name { get; set; }
        public DateTime last_refresh { get; set; }
        public Url proof_url { get; set; }
        public string kpid { get; set; }
        public Dictionary<string, int> tokens { get; set; }
        public Dictionary<string, int> killproofs { get; set; }

        public KillProof() { /** NOOP **/ }
    }
}
