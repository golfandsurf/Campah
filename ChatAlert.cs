using System.Text.RegularExpressions;
using FFACETools;

namespace CampahApp
{
    class ChatAlert
    {
        public ChatAlert(Regex condition)
        {
            Condition = condition;
            Completed = false;
            Mode = ChatMode.Generic;
        }

        private Regex Condition { get; set; }

        public Match Result { get; private set; }

        public bool Completed { get; private set; }

        public ChatMode Mode { get; private set; }
       
        public bool ParseLine(FFACE.ChatTools.ChatLine line)
        {
            Result = Condition.Match(line.Text);            
            return (Completed = Condition.IsMatch(line.Text));
        }
    }
}