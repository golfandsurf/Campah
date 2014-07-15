using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Documents;
using System.Linq;
using System.Text.RegularExpressions;
using FFACETools;
using System.Windows.Media;

namespace CampahApp
{
    class Chatlog : INotifyPropertyChanged
    {
        static Chatlog()
        {
            Instance = new Chatlog();

        }
        public Chatlog()
        {
            Lines = new List<FFACE.ChatTools.ChatLine>();
            ChatLog = new FlowDocument();
            _alerts = new List<ChatAlert>();
            _filters = new List<ChatMode>();
            var chatWorker = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            chatWorker.Tick += ChatWorkerTick;
            chatWorker.Start();
            _lasttells = new List<string>();
        }

        public List<FFACE.ChatTools.ChatLine> Lines { get; set; }

        public FlowDocument ChatLog { get; set; }

        public static Chatlog Instance { get; private set; }

        private List<ChatAlert> _alerts;
        private List<string> _lasttells;
        private List<ChatMode> _filters;
        

        public string LastTell(string name)
        {
            return _lasttells.Count == 0 ? "" : (_lasttells[(_lasttells.IndexOf(name)+1)%_lasttells.Count]);
        }

        public void AddAlert(ChatAlert alert)
        {
            _alerts.Add(alert);
        }

        public void ClearChatAlerts()
        {
            _alerts.Clear();
        }

        public void RemoveAlert(ChatAlert alert)
        {
            _alerts.Remove(alert);
        }

        void ChatWorkerTick(object sender, EventArgs e)
        {
            Update();
        }

        private Brush ChatColorConverter(System.Drawing.Color chatColor)
        {
            return colorToBrush(chatColor);
        }

        private Brush colorToBrush(System.Drawing.Color color)
        {
            var bc = new BrushConverter();
            if (color.IsNamedColor)
            {
                return (Brush)bc.ConvertFromString(color.Name);
            }

            return (Brush)bc.ConvertFromString("#" + color.Name);
        }

        public void Rewrite()
        {
            ChatLog.Blocks.Clear();
            foreach (var line in Lines)
            {
                AddLine(line);
            }
        }

        private void AddLine(FFACE.ChatTools.ChatLine line)
        {
            var para = new Paragraph();
            
            para = ProcessLine(line, para);
            if (para != null)
            {
                ChatLog.Blocks.Add(para);
            }
        }

        public void Update()
        {
            if (FFACEInstance.Instance == null)
            {
                return;
            }

            if (ChatLog.Blocks.Count > 1000)
            {
                while (ChatLog.Blocks.Count > 500)
                {
                    ChatLog.Blocks.Remove(ChatLog.Blocks.FirstBlock);
                }
            }

            FFACE.ChatTools.ChatLine chatline;
            var lines = new List<FFACE.ChatTools.ChatLine>();

            while ((chatline = FFACEInstance.Instance.Chat.GetNextLine()) != null)
            {
                if (lines.Count > 0 && chatline.RawString[4] == lines[0].RawString[4])
                {
                    lines[0].Text += chatline.Text;
                }
                else
                {
                    if (chatline.Type == ChatMode.RcvdTell)
                    {
                        var findname = new Regex(@"(.*)>>.*");
                        _lasttells.Remove(findname.Matches(chatline.Text)[0].Groups[1].Value);
                        _lasttells.Insert(0, findname.Matches(chatline.Text)[0].Groups[1].Value);
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
                AddLine(line);
            }
            foreach (ChatAlert alert in _alerts)
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
            _filters.Clear();
            foreach (string raw in rawfilters)
            {
                //if (Enum.IsDefined(typeof(ChatMode), raw.Trim()))
                Int16 chatcode;
                if (Int16.TryParse(raw,System.Globalization.NumberStyles.HexNumber,System.Globalization.CultureInfo.InvariantCulture, out chatcode) && Enum.IsDefined(typeof(ChatMode),chatcode))
                {
                    _filters.Add((ChatMode)chatcode);
                }
                else if (Enum.GetNames(typeof(ChatMode)).Any(x => x.ToLower() == raw.Trim().ToLower()))
                {
                    _filters.Add((ChatMode)Enum.Parse(typeof(ChatMode), raw.Trim(), true));
                }
                else
                {
                    if (rawstring.Length > 0)
                    {
                        return;
                    }
                    _filters.Clear();
                }
            }
            Rewrite();            
        }
     

        public Paragraph ProcessLine(FFACE.ChatTools.ChatLine chatline, Paragraph para)
        {
            if (para == null)
            {
                throw new ArgumentNullException("para");
            }

            if (!_filters.Contains(chatline.Type) && _filters.Count != 0)
            {
                return null;
            }

            para = new Paragraph();
            var range = new TextRange(para.ContentStart, para.ContentEnd);
            range.Text += "("+((int)chatline.Type).ToString("X2") + ")";
            range.Text += chatline.Now;
            
            range.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.SteelBlue);
            range.ApplyPropertyValue(TextElement.FontWeightProperty, System.Windows.FontWeights.Bold);
            var endOfPrefix = range.End;

            para.Inlines.Add(chatline.Text);
            range = new TextRange(endOfPrefix, para.ContentEnd);
            range.ApplyPropertyValue(TextElement.ForegroundProperty, ChatColorConverter(chatline.Color));
            range.ApplyPropertyValue(TextElement.FontWeightProperty, System.Windows.FontWeights.Bold);
            para.LineHeight = 0.5;
            return para;
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
