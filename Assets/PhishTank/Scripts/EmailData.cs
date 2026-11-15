using System;
using System.Collections.Generic;

namespace PhishTank
{
    /// <summary>One email that becomes one fish. Loaded from Data/emails.json.</summary>
    [Serializable]
    public class EmailData
    {
        public string id;
        public string fromName;
        public string fromAddress;
        public string subject;
        public string body;
        public string linkText;      // what the link SAYS
        public string linkUrl;       // where the link REALLY goes (shown when you inspect it)
        public string attachment;    // empty = no attachment
        public bool isScam;
        public string[] clues;       // e.g. "Lookalike domain", "Urgency" — shown after you sort it
        public string explanation;   // one short sentence teaching the player
        public int difficulty;       // 1 = obvious, 2 = medium, 3 = tricky
    }

    [Serializable]
    public class EmailList
    {
        public List<EmailData> emails;
    }
}
