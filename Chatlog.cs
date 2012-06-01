using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Documents;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using FFACETools;

namespace CampahApp
{
    class ChatAlert
    {
        private Regex Condition { get; set; }
        public Match Result { get; private set; }
        public bool Completed { get; private set; }
        public ChatMode Mode { get; private set; }

        public ChatAlert(Regex condition) : this(condition, ChatMode.Generic) { }
        public ChatAlert(Regex condition, ChatMode mode)
        {
            Condition = condition;
            Completed = false;
        }
       
        public bool ParseLine(FFACE.ChatTools.ChatLine line)
        {
            Result = Condition.Match(line.Text);            
            return (Completed = Condition.IsMatch(line.Text));
        }
    }

    class Chatlog : INotifyPropertyChanged
    {
        //public static FFACE FFACE_INSTANCE.Instance { get; set; }
        public List<FFACE.ChatTools.ChatLine> Lines;
        public FlowDocument chatlog;
        public static Chatlog Instance { get; private set; }
        private List<ChatAlert> alerts;
        private List<string> lasttells;
        private List<ChatMode> filters;
        static Chatlog()
        {
            Instance = new Chatlog();
                        
        }
        public Chatlog()
        {
            Lines = new List<FFACE.ChatTools.ChatLine>();            
            chatlog = new FlowDocument();
            alerts = new List<ChatAlert>();
            filters = new List<ChatMode>();
            System.Windows.Threading.DispatcherTimer chatWorker = new System.Windows.Threading.DispatcherTimer();
            chatWorker.Interval = TimeSpan.FromMilliseconds(100);
            chatWorker.Tick += new EventHandler(chatWorker_Tick);
            chatWorker.Start();
            lasttells = new List<string>();
            //lasttell = "";
        }

        public string LastTell(string name)
        {
            if (lasttells.Count == 0)
                return "";
            return (lasttells[(lasttells.IndexOf(name)+1)%lasttells.Count]);
        }

        public void addAlert(ChatAlert alert)
        {
            alerts.Add(alert);
        }

        public void ClearChatAlerts()
        {
            alerts.Clear();
        }

        public void removeAlert(ChatAlert alert)
        {
            alerts.Remove(alert);
        }

        void chatWorker_Tick(object sender, EventArgs e)
        {
            Update();
        }

        private System.Windows.Media.Brush chatColorConverter(System.Drawing.Color chatColor)
        {
            return colorToBrush(chatColor);
        }

        private bool isColorClose(System.Drawing.Color color1, System.Drawing.Color color2, int tolerance)
        {
            if (Math.Abs(color1.GetHue() - color2.GetHue()) % (360 - tolerance) < tolerance)
                if (Math.Abs(color1.GetSaturation() - color2.GetSaturation()) % (360 - tolerance) < tolerance)
                    if (Math.Abs(color1.GetBrightness() - color2.GetBrightness()) % (360 - tolerance) < tolerance)
                        return true;
            return false;
        }

        private System.Windows.Media.Brush colorToBrush(System.Drawing.Color color)
        {
            System.Windows.Media.BrushConverter bc = new System.Windows.Media.BrushConverter();
            if (color.IsNamedColor)
            {
                return (System.Windows.Media.Brush)bc.ConvertFromString(color.Name);
            }
            else
            {
                return (System.Windows.Media.Brush)bc.ConvertFromString("#" + color.Name);
            }
        }

        public void rewrite()
        {
            chatlog.Blocks.Clear();
            foreach (FFACE.ChatTools.ChatLine line in Lines)
            {
                addline(line);
            }
        }

        private void addline(FFACE.ChatTools.ChatLine line)
        {
            Paragraph para = new Paragraph();
            para = ProcessLine(line, para);
            if (para != null)
                chatlog.Blocks.Add(para);
        }

        public void Update()
        {
            if (FFACE_INSTANCE.Instance == null)
                return;
            if (chatlog.Blocks.Count > 1000)
                while (chatlog.Blocks.Count > 500)
                    chatlog.Blocks.Remove(chatlog.Blocks.FirstBlock);
            FFACE.ChatTools.ChatLine chatline;
            List<FFACE.ChatTools.ChatLine> lines = new List<FFACE.ChatTools.ChatLine>();
            while ((chatline = FFACE_INSTANCE.Instance.Chat.GetNextLine()) != null)
            {
                if (lines.Count > 0 && chatline.RawString[4] == lines[0].RawString[4])
                {
                    lines[0].Text += chatline.Text;
                }
                else
                {
                    if (chatline.Type == ChatMode.RcvdTell)
                    {
                        Regex findname = new Regex(@"(.*)>>.*");
                        lasttells.Remove(findname.Matches(chatline.Text)[0].Groups[1].Value);
                        lasttells.Insert(0, findname.Matches(chatline.Text)[0].Groups[1].Value);
                        //LastTell = findname.Matches(chatline.Text)[0].Groups[1].Value;
                    }
                    lines.Add(chatline);
                }
            }
            
            Lines.AddRange(lines);
            if (Lines.Count > 500)
                Lines.RemoveRange(0, 200);

            foreach (FFACE.ChatTools.ChatLine line in lines)
            {
                addline(line);
            }
            foreach (ChatAlert alert in alerts)
            {
                foreach (FFACE.ChatTools.ChatLine line in lines)
                {
                    if (alert.Mode == ChatMode.Generic || alert.Mode == line.Type)
                    {
                        alert.ParseLine(line);
                    }
                }
            }
        }

        public void UpdateFilters(string rawstring)
        {
            rawstring = "," + rawstring + ",";
            rawstring = rawstring.ToLower().Replace(",yell,", ",RcvdYell,SentYell,").Replace(",party,", ",RcvdParty,SentParty,").Replace(",tell,", ",RcvdTell,SentTell,").Replace(",linkshell,", ",RcvdLinkShell,SentLinkShell,").Replace(",say,", ",RcvdSay,SentSay,").Replace(",shout,", ",RcvdShout,SentShout,").Replace(",emote,", ",RcvdEmote,SentEmote,");
            rawstring = rawstring.Substring(1, rawstring.Length - 2);
            string[] rawfilters = rawstring.Split(',');
            filters.Clear();
            foreach (string raw in rawfilters)
            {
                //if (Enum.IsDefined(typeof(ChatMode), raw.Trim()))
                Int16 chatcode = 1;
                if (Int16.TryParse(raw,System.Globalization.NumberStyles.HexNumber,System.Globalization.CultureInfo.InvariantCulture, out chatcode) && Enum.IsDefined(typeof(ChatMode),chatcode))
                    filters.Add((ChatMode)chatcode);
                else if (Enum.GetNames(typeof(ChatMode)).Any(x => x.ToLower() == raw.Trim().ToLower()))
                {
                    filters.Add((ChatMode)Enum.Parse(typeof(ChatMode), raw.Trim(), true));
                }
                else
                {
                    if (rawstring.Length > 0)
                    {
                        return;
                    }
                    filters.Clear();
                }
            }
            rewrite();            
        }
     

        public Paragraph ProcessLine(FFACE.ChatTools.ChatLine chatline, Paragraph para)
        {
            if (filters.Contains(chatline.Type) || filters.Count == 0)
            {

                TextRange range;
                TextPointer EndOfPrefix = null; ;
                para = new Paragraph();
                range = new TextRange(para.ContentStart, para.ContentEnd);//log.Document.ContentEnd, log.Document.ContentEnd);
                range.Text += "("+((int)chatline.Type).ToString("X2") + ")";
                range.Text += chatline.Now;
                //                    para.Inlines.Add(chatline.Now);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, System.Windows.Media.Brushes.SteelBlue);
                range.ApplyPropertyValue(TextElement.FontWeightProperty, System.Windows.FontWeights.Bold);
                EndOfPrefix = range.End;

                para.Inlines.Add(chatline.Text);
                range = new TextRange(EndOfPrefix, para.ContentEnd);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, chatColorConverter(chatline.Color));
                range.ApplyPropertyValue(TextElement.FontWeightProperty, System.Windows.FontWeights.Bold);
                para.LineHeight = 0.5;
                return para;
            }
            return null;
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
            }
        }

        /*
        private int Index
        {
            get
            {
                return FFACE_INSTANCE.Instance.Chat.GetLineCount;
            }
        }

       
        public FFACE.ChatTools.ChatLogEntry[] getlines()
        {
            List<int> indexes = new List<int>();
            List<FFACE.ChatTools.ChatLogEntry> lines = new List<FFACE.ChatTools.ChatLogEntry>();
            FFACE.ChatTools.ChatLogEntry testline = FFACE_INSTANCE.Instance.Chat.GetLineRaw(0);

            for (int i = 0; i < FFACE_INSTANCE.Instance.Chat.GetLineCount+22; i++)
            {
                short m = (short)i;
                FFACE.ChatTools.ChatLogEntry line = FFACE_INSTANCE.Instance.Chat.GetLineRaw(m);

                if (line.LineType != ChatMode.Error)
                {
                    indexes.Add(i);
                    lines.Add(line);
                }
            }
            for (int i = FFACE_INSTANCE.Instance.Chat.GetLineCount - 0; i < 0; i++)
            {
                short m = (short)i;
                FFACE.ChatTools.ChatLogEntry line = FFACE_INSTANCE.Instance.Chat.GetLineRaw(m);

                if (line.LineType != ChatMode.Error)
                {
                    indexes.Add(i);
                    lines.Add(line);
                }
            }
            lines.Sort();
            return lines.ToArray();
        }
         */
    }
}
