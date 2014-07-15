using System;
using System.ComponentModel;
using System.Diagnostics;
using FFACETools;

namespace CampahApp
{
    public class CampahStatus : INotifyPropertyChanged
    {
        private CampahStatus(string status, Modes mode)
        {
            Status = status;
            Mode = mode;
            ProjectedCost = 1000;
            TotalSpent = 2000;
            Globaldelay = 500;
            Buycyclewait = 1;
            Cheapo = false;
            Blockcommands = false;
            Openlast = false;
            Automaticupdates = true;
            _lowballbid = "";
            AllowCycleRandom = true;
            Chatfilter = "";
            FileVersionInfo fileVer = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Version = fileVer.FileVersion;
            Title = "Campah" + " v" + fileVer.FileMajorPart + "." + fileVer.FileMinorPart + "." + fileVer.FileBuildPart + " Beta - Revision " + fileVer.FilePrivatePart;
            _history = "Campah Operation Log - " + DateTime.Now.ToShortDateString() + "\r\n";
        }

        public Process process;

        private String _status;

        private String _title;

        private Modes _mode;

        public double Globaldelay;

        public double Buycyclewait;

        public bool AllowCycleRandom { get; set; }

        public bool Blockcommands;

        public bool Automaticupdates;

        private string _lowballbid;

        public bool Alwaysontop;

        public string Currentpath;

        public string Chatfilter;

        public bool Cheapo;
        
        public static string price;

        public int ProjectedCost { get; set; }

        public bool Openlast;

        private string _history;

        public int TotalSpent { get; set; }

        public string Version { get; private set; }

        public int OneClickMin { get; set; }

        public int OneClickInc { get; set; }

        public int WebTimeout { get; set; }

        public static CampahStatus Instance { get; set; }
        
        static CampahStatus()
        {
            Instance = new CampahStatus("", Modes.Stopped);
        }

        public static void SetStatus(string status)
        {
            if (!String.IsNullOrEmpty(status))
            {
                Instance.Status = status;
            }
        }

        public static void SetStatus(string status, Modes mode)
        {
            SetStatus(status);
            Instance.Mode = mode;
        }

        public bool BlockCommands
        {
            get
            {
                return Blockcommands;
            }
            set
            {
                Blockcommands = value;
                NotifyPropertyChanged("BlockCommands");
            }
        }

        public string LowballBid
        {
            get
            {
                return _lowballbid;
            }
            set
            {
                _lowballbid = value;
                NotifyPropertyChanged("LowballBid");
            }
        }

        public string History
        {
            get
            {
                return _history;
            }
            set
            {
                _history = value;
                NotifyPropertyChanged("History");
            }
        }

        public string CurrentPath
        {
            get
            {
                return Currentpath;
            }
            set
            {
                Currentpath = value;
                NotifyPropertyChanged("CurrentPath");
            }
        }

        public string ChatFilter
        {
            get
            {
                return Chatfilter;
            }
            set
            {
                Chatfilter = value;
                NotifyPropertyChanged("ChatFilter");
            }
        }

        public bool AutomaticUpdates
        {
            get
            {
                return Automaticupdates;
            }
            set
            {
                Automaticupdates = value;
                NotifyPropertyChanged("AutomaticUpdates");
            }
        }
        
        public bool CheapO
        {
            get
            {
                return Cheapo;
            }
            set
            {
                Cheapo = value;
                NotifyPropertyChanged("CheapO");
            }
        }

        public bool OpenLast
        {
            get
            {
                return Openlast;
            }
            set
            {
                Openlast = value;
                NotifyPropertyChanged("OpenLast");
            }
        }

        public bool AlwaysOnTop
        {
            get
            {
                return Alwaysontop;
            }
            set
            {
                Alwaysontop = value;
                NotifyPropertyChanged("AlwaysOnTop");
            }
        }

        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
                NotifyPropertyChanged("Title");
            }
        }

        public double GlobalDelay
        {
            get
            {
                return Globaldelay;
            }
            set
            {
                Globaldelay = value;
                NotifyPropertyChanged("GlobalDelay");
            }
        }

        public double BuyCycleWait
        {
            get
            {
                return Buycyclewait;
            }
            set
            {
                Buycyclewait = value;
                NotifyPropertyChanged("BuyCycleWait");
            }
        }

        public string Price
        {
            get
            {
                return price;
            }
            set
            {
                price = value;
                NotifyPropertyChanged("Price");
            }
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

        public Process Process
        {
            get
            {
                return process;
            }

            set
            {
                process = value;
                
                FFACE.ParseResources.UseFFXIDatFiles = true;
                FFACE.ParseResources.LanguagePreference = FFACE.ParseResources.Languages.English;
                NotifyPropertyChanged("Process");
            }
        }

        private bool _waiting;
        public String Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                NotifyPropertyChanged("Status");
                if (value.Contains("Beginning next cycle in"))
                {
                    if (_waiting)
                    {
                        return;
                    }
                    _waiting = true;
                }
                else if (_waiting)
                {
                    _waiting = false;
                }
                History += DateTime.Now.ToShortTimeString() + " : " + value + "\r\n";
            }
        }

        public Modes Mode
        {
            get
            {
                return _mode;
            }
            set
            {
                _mode = value;
                NotifyPropertyChanged("Mode");
            }
        }       
    }
}