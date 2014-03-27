using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Diagnostics;
using PandyProductions;
using FFACETools;

namespace CampahApp
{
    public enum Modes
    {
        Stopped,
        Error,
        Buying,
        Selling,
        Updating
    }

    public static class UpdateConstants
    {
        public const String UPDATE_URL = "http://gs.1942.net/downloads/";
        public const String UPDATE_FOLDER = "UpdateFiles";
    }

    //put literals here. It's a work in progress...
    public static class Constants
    {
        public const String BIDVAL_SIG = "8b0d????????33d284db";
        public const int BIDVAL_OFFSET = 0x28;
        
        //public const String AH_SIG = "A3 ?? ?? ?? ?? EB 06 89 1D ?? ?? ?? ?? 39 1D ?? ?? ?? ?? 75 22 6A 24 E8"; //h1pp0 sig
        public const String AH_SIG = "8B 35 ?? ?? ?? ?? 3B F7 74 2C 8B 46 1C";  //"XX 90 F1 XX XX ?? ?? ?? ?? 70 42";
        public const bool READ_LOC = false;
        public static int[] AH_OFFSETS = {0,0};
        public const int AH_OFFSET_ARRAYUNIQUELENGTH = 0x0C;
        public const int AH_OFFSET_ARRAYLOADED = 0x08;
        public const int AH_OFFSET_ARRAYSTRUCT = 0x20;
        public const int AH_OFFSET_FIRSTITEMID = 0x4;
        public const int AH_OFFSET_ITEMID_INCREMENT = 0x40;

        public const String CAMPAH_EXTENSION = "Campah Save Files (*.chs)|*.chs";

        //public const String MENU_SIG = "51A1????????5685c0752BA1";//"583d????????410888"; //"01b9????????e80419"; //"51a1????????563bc1753a";
        //public const int MENU_OFFSET = 0x32;
        public const int MENU_INDEX_OFFSET = 0x4C;
        //public const int MENU_LENGTH_OFFSET = 0x58;
        public const int MENU_LENGTH_OFFSET = 0x5A;
    }

    public class AHTarget
    {
        public String TargetName { get; set; }

        public AHTarget(string targetname)
        {
            TargetName = targetname;
        }

        public override bool Equals(object obj)
        {
            return ((AHTarget)obj).TargetName == TargetName;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public static class ChatInputBuffer
    {
        private static int cursor = 0;
        private static List<string> lines = new List<string>();

        static ChatInputBuffer()
        {
            lines.Add("");
        }

        public static void AddLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return;
            lines.Insert(1, line);
            if (lines.Count == 25)
                lines.RemoveRange(20, 5);
            cursor = 0;
        }

        public static string Up(string line)
        {            
           lines[cursor % lines.Count] = line;
            cursor++;
            if (lines.Count > 0)
                return lines[cursor % lines.Count];
            else
                return "";
        }
        
        public static string Down()
        {
            if (cursor % lines.Count == 0)
                cursor = 0;
            else
                cursor--;
            if (lines.Count > 0)
                return lines[cursor % lines.Count];
            else
                return "";
        }
    }
    

    //Basically everything thats displayed in the UI in some way or form.
    public class CampahStatus : INotifyPropertyChanged
    {
        //private String player;
        public Process process;
        private String status;
        private String title;
        private Modes mode;
        public double globaldelay;
        public double buycyclewait;
        public bool AllowCycleRandom { get; set; }
        public bool blockcommands;
        public bool automaticupdates;
        private string lowballbid;
        public bool alwaysontop;
        public string currentpath;
        public string chatfilter;
        public bool cheapo;
        //public static string Name { get; set; }
        public static string price;
        public int ProjectedCost { get; set; }
        public bool openlast;
        private string history;
        public int TotalSpent { get; set; }
        public String Version { get; private set; }
        public int OneClickMin { get; set; }
        public int OneClickInc { get; set; }
        public int WebTimeout { get; set; }

        public static CampahStatus Instance { get; set; }
        
        static CampahStatus()
        {
            Instance = new CampahStatus("", Modes.Stopped);
        }

        public static void SetStatus(String status)
        {
            if (!String.IsNullOrEmpty(status))
                Instance.Status = status;
        }

        public static void SetStatus(String status, Modes mode)
        {
            SetStatus(status);
            Instance.Mode = mode;
        }

        private CampahStatus(String status, Modes mode)
        {            
//            Player = player;
            Status = status;
            Mode = mode;
            ProjectedCost = 1000;
            TotalSpent = 2000;
            globaldelay = 500;
            buycyclewait = 1;
            cheapo = false;
            blockcommands = false;
            openlast = false;
            automaticupdates = true;
            lowballbid = "";
            AllowCycleRandom = true;
            chatfilter = "";
            FileVersionInfo fileVer = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Version = fileVer.FileVersion;
            Title = "Campah" + " v" + fileVer.FileMajorPart + "." + fileVer.FileMinorPart + "." + fileVer.FileBuildPart + " Beta - Revision " + fileVer.FilePrivatePart;
            history = "Campah Operation Log - " + DateTime.Now.ToShortDateString() + "\r\n";
        }

        public bool BlockCommands
        {
            get
            {
                return blockcommands;
            }
            set
            {
                blockcommands = value;
                NotifyPropertyChanged("BlockCommands");
            }
        }

        public string LowballBid
        {
            get
            {
                return lowballbid;
            }
            set
            {
                lowballbid = value;
                NotifyPropertyChanged("LowballBid");
            }
        }

        public string History
        {
            get
            {
                return history;
            }
            set
            {
                history = value;
                NotifyPropertyChanged("History");
            }
        }

        public string CurrentPath
        {
            get
            {
                return currentpath;
            }
            set
            {
                currentpath = value;
                NotifyPropertyChanged("CurrentPath");
            }
        }

        public string ChatFilter
        {
            get
            {
                return chatfilter;
            }
            set
            {
                chatfilter = value;
                NotifyPropertyChanged("ChatFilter");
            }
        }

        public bool AutomaticUpdates
        {
            get
            {
                return automaticupdates;
            }
            set
            {
                automaticupdates = value;
                NotifyPropertyChanged("AutomaticUpdates");
            }
        }
        
        public bool CheapO
        {
            get
            {
                return cheapo;
            }
            set
            {
                cheapo = value;
                NotifyPropertyChanged("CheapO");
            }
        }

        public bool OpenLast
        {
            get
            {
                return openlast;
            }
            set
            {
                openlast = value;
                NotifyPropertyChanged("OpenLast");
            }
        }

        public bool AlwaysOnTop
        {
            get
            {
                return alwaysontop;
            }
            set
            {
                alwaysontop = value;
                NotifyPropertyChanged("AlwaysOnTop");
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                title = value;
                NotifyPropertyChanged("Title");
            }
        }

        public double GlobalDelay
        {
            get
            {
                return globaldelay;
            }
            set
            {
                globaldelay = value;
                NotifyPropertyChanged("GlobalDelay");
            }
        }

        public double BuyCycleWait
        {
            get
            {
                return buycyclewait;
            }
            set
            {
                buycyclewait = value;
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
/*
        public String Player
        {
            get
            {
                return player;
            }
            set
            {
                player = value;
                NotifyPropertyChanged("Player");
            }
        }
*/
        public Process Process
        {
            get
            {
                return process;
            }

            set
            {
                process = value;
                
                ////////////////////////////SET RESOURCE PATH///////////////////////////
                /*for (int i = 0; i < CampahStatus.Instance.Process.Modules.Count; i++)
                {
                    if (CampahStatus.Instance.Process.Modules[i].ModuleName == "Hook.dll")
                    {
                        FFACE.WindowerPath = CampahStatus.Instance.Process.Modules[i].FileName.Substring(0,
                            CampahStatus.Instance.Process.Modules[i].FileName.Length - 8) + @"plugins";
                        break;
                    }
                }
                 */
                FFACE.ParseResources.UseFFXIDatFiles = true;
                FFACE.ParseResources.LanguagePreference = FFACE.ParseResources.Languages.English;
                ////////////////////////////SET RESOURCE PATH///////////////////////////
                
                NotifyPropertyChanged("Process");
            }
        }

        private bool waiting = false;
        public String Status
        {
            get
            {
                return status;
            }
            set
            {
                status = value;
                NotifyPropertyChanged("Status");
                if (value.Contains("Beginning next cycle in"))
                {
                    if (waiting)
                        return;
                    waiting = true;
                }
                else if (waiting)
                    waiting = false;
                History += DateTime.Now.ToShortTimeString() + " : " + value + "\r\n";
            }
        }

        public Modes Mode
        {
            get
            {
                return mode;
            }
            set
            {
                mode = value;
                NotifyPropertyChanged("Mode");
            }
        }       
    } //CampahStatus End

    public class AHItem
    {
        public int ID { get; private set; }
        public String Name { get; protected set; }
        public bool Stackable { get; set; }
        public string Address { get; private set; }

        public AHItem(int id, String name, bool stackable, string address)
        {
            ID = id;
            Name = name;
            Stackable = stackable;
            Address = address;
        }

        public override bool Equals(object obj)
        {
            return ((AHItem)obj).ID == ID;
        }

        public override int GetHashCode()
        {

            return base.GetHashCode();
        }
    }

    public class ItemRequest : INotifyPropertyChanged
    {
        public int Minimum { get; set; }
        public int Maximum { get; set; }
        public int Increment { get; set; }
        public int Quantity { get; set; }
        public bool Stack { get; set; }
        public AHItem ItemData { get; private set; }
        int boughtCount;
        int boughtCost;

        public ItemRequest(int min, int max, int inc, int qnty, bool stack, AHItem itemdata)
        {
            Minimum = min;
            Maximum = max;
            Increment = inc;
            Quantity = qnty;
            Stack = stack;
            ItemData = itemdata;
            boughtCost = 0;
            boughtCount = 0;
        }

        public int BoughtCount
        {
            get
            {
                return boughtCount;
            }
            set
            {
                boughtCount = value;
                NotifyPropertyChanged("BoughtCount");
            }
        }

        public int BoughtCost
        {
            get
            {
                return boughtCost;
            }
            set
            {
                boughtCost = value;
                NotifyPropertyChanged("BoughtCost");
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
    }

    public static class AuctionHouse
    {
        public static Dictionary<String, AHItem> items;
        private static ProcessMemoryReader preader;
        private static IntPtr AhStructPointer { get; set; }
        private static IntPtr MenuStructPointer { get; set; }
        private static IntPtr BidValPointer { get; set; }

        static AuctionHouse()
        {
            items = new Dictionary<String, AHItem>();
        }

        public static String[] GetItemStrings()
        {
            return items.Keys.ToArray<String>();
        }

        public static int BidValue
        {
            get
            {
                //int m = (int)BidValPointer;
                if (BidValPointer == IntPtr.Zero)
                    SetProcessMemoryReader();
                return read2bytes(BidValPointer, Constants.BIDVAL_OFFSET);
            }
            set
            {
                if (BidValPointer == IntPtr.Zero)
                    SetProcessMemoryReader();
                int byteswritten;
                preader.WriteProcessMemory((IntPtr)( (int)BidValPointer + Constants.BIDVAL_OFFSET ), BitConverter.GetBytes(value), out byteswritten);
            }
        } 

        public static int MenuIndex
        {
            
            get
            {
                return FFACE_INSTANCE.Instance.Menu.MenuIndex;//read2bytes(readPointer(MenuStructPointer, 0), Constants.MENU_INDEX_OFFSET);
            }
            set
            {
                //SetProcessMemoryReader();
                //int byteswritten;
                //Preader.WriteProcessMemory((IntPtr)((int)readPointer(MenuStructPointer, 0) + Constants.MENU_INDEX_OFFSET), BitConverter.GetBytes(Convert.ToInt32(value)), out byteswritten);
                FFACE_INSTANCE.Instance.Menu.MenuIndex = value;
            }
        }

        public static int MenuLength
        {
            get
            {
                return 0; //read2bytes(readPointer(MenuStructPointer, 0), Constants.MENU_LENGTH_OFFSET) - 1;
            }
        }

        public static AHItem Add(AHItem item)          //If duplicate found, returns duplicate
        {
            if (items.ContainsKey(item.Name))
                return items[item.Name];
            items.Add(item.Name, item);
            return null;
        }

        public static AHItem GetItem(String name)
        {
//            AHItem value = null;
//            items.TryGetValue(name, out value);
//            return value;
            foreach (KeyValuePair<String, AHItem> pair in AuctionHouse.items)
                if (pair.Key.ToLower() == name.ToLower())
                    return pair.Value;
            return null;
        }

        private static ProcessMemoryReader Preader
        {
            get
            {
                if (preader == null)
                {
                    SetProcessMemoryReader();
                }
                return preader;
            }
        }

        public static AHItem GetItem(int id)
        {
            foreach (AHItem item in items.Values)
            {
                if (item.ID == id)
                {
                    return item;
                }
            }
            return null;
        }

        private static IntPtr sigscan(string sig)
        {
            IntPtr pointer = Preader.FindSignature(sig, Constants.READ_LOC);
            if (pointer == IntPtr.Zero)
                CampahStatus.Instance.Status = "Signature Failed";
            return pointer;
        }
        
        private static IntPtr sigscan(string sig, bool readloc)
        {
            IntPtr pointer = Preader.FindSignature(sig, readloc);
            if (pointer == IntPtr.Zero)
                CampahStatus.Instance.Status = "Signature Failed";
            return pointer;
        }

        public static void SetProcessMemoryReader()
        {
            preader = new ProcessMemoryReader(CampahStatus.Instance.process.Id);
            IntPtr pointer = sigscan(Constants.AH_SIG);//+Constants.AH_OFFSETS[0];

            
            for (int i = 0; i < Constants.AH_OFFSETS.Length; i++)
                pointer = readPointer(pointer, Constants.AH_OFFSETS[i]);

            AhStructPointer = pointer;
//            IntPtr pointer2 = sigscan(Constants.MENU_SIG, false);
//            BidValue = 1000;
//            String pstr = ((int)pointer2).ToString("X4");
//            IntPtr pointer3 = sigscan(pstr.Substring(6) + pstr.Substring(4, 2) + pstr.Substring(2, 2) + pstr.Substring(0, 2));
//            MenuStructPointer = (IntPtr)((int)pointer2 + Constants.MENU_OFFSET);
            BidValPointer = readPointer(readPointer(sigscan(Constants.BIDVAL_SIG),0),0);
        }

        private static IntPtr ArrayPointer
        {
            get
            {
                //IntPtr pointer = AhStructPointer;
                //for (int i = 0; i < Constants.AH_OFFSETS.Length; i++)
                //    pointer = readPointer(pointer, Constants.AH_OFFSETS[i]);
                return readPointer((IntPtr)((int)AhStructPointer + Constants.AH_OFFSET_ARRAYSTRUCT),0);
            }
        }


        private static int read2bytes(IntPtr pointer, int offset)
        {
            int readcount;
            return BitConverter.ToInt16(Preader.ReadProcessMemory((IntPtr)((int)pointer + offset), 2, out readcount), 0);
        }

        private static IntPtr readPointer(IntPtr pointer, int offset)
        {
            int readcount;
            return (IntPtr)BitConverter.ToInt32(Preader.ReadProcessMemory((IntPtr)((int)pointer + offset), 4, out readcount), 0);
        }

        public static int LoadedCount
        {
            get
            {
                //IntPtr pointer = AhStructPointer;
                //for (int i = 0; i < Constants.AH_OFFSETS.Length; i++)
                //    pointer = readPointer(pointer, Constants.AH_OFFSETS[i]);
                return read2bytes((IntPtr)((int)AhStructPointer + Constants.AH_OFFSET_ARRAYLOADED), 0);
            }
        }

        public static int UniqueCount
        {
            get
            {
                //IntPtr pointer = AhStructPointer;
                //for (int i = 0; i < Constants.AH_OFFSETS.Length; i++)
                //    pointer = readPointer(pointer, Constants.AH_OFFSETS[i]);
                return read2bytes((IntPtr)((int)AhStructPointer + Constants.AH_OFFSET_ARRAYUNIQUELENGTH), 0);
            }
        }

        public static int[] ReadIDArray()
        {
            List<int> itemids = new List<int>();
            MemoryBuffer buffer;
            int memloc = (int)ArrayPointer;
            int n = LoadedCount;
            buffer = Preader.createSearchBuffer(memloc, (Constants.AH_OFFSET_FIRSTITEMID
                    + Constants.AH_OFFSET_ITEMID_INCREMENT * n));
            for (int i = 0; i < n; i++)
                itemids.Add((int)buffer.Read2Bytes(memloc
                    + Constants.AH_OFFSET_FIRSTITEMID
                    + Constants.AH_OFFSET_ITEMID_INCREMENT * i));
            CampahStatus.SetStatus("Item list read complete.");
            return itemids.ToArray();
        }
    }

    public static class CurrentSelection
    {
        public static string Name { get; set; }
        public static string Price { get; set; }
        static CurrentSelection()
        {
            Name = "";
            Price = "0g";
        }
    }
}