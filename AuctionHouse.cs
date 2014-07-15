using System;
using System.Collections.Generic;
using System.Linq;
using PandyProductions;

namespace CampahApp
{
    public static class AuctionHouse
    {
        public static Dictionary<string, AhItem> Items;

        private static ProcessMemoryReader _preader;
        private static IntPtr AhStructPointer { get; set; }
        private static IntPtr BidValPointer { get; set; }

        static AuctionHouse()
        {
            Items = new Dictionary<String, AhItem>();
        }

        public static String[] GetItemStrings()
        {
            return Items.Keys.ToArray();
        }

        public static int BidValue
        {
            get
            {
                if (BidValPointer == IntPtr.Zero)
                {
                    SetProcessMemoryReader();
                }
                return Read2Bytes(BidValPointer, Constants.BIDVAL_OFFSET);
            }
            set
            {
                if (BidValPointer == IntPtr.Zero)
                {
                    SetProcessMemoryReader();
                }
                int byteswritten;
                _preader.WriteProcessMemory((IntPtr)( (int)BidValPointer + Constants.BIDVAL_OFFSET ), BitConverter.GetBytes(value), out byteswritten);
            }
        } 

        public static int MenuIndex
        {
            
            get
            {
                return FFACEInstance.Instance.Menu.MenuIndex;
            }
            set
            {
                FFACEInstance.Instance.Menu.MenuIndex = value;
            }
        }

        public static int MenuLength
        {
            get
            {
                return 0;
            }
        }

        public static AhItem Add(AhItem item)          
        {
            if (Items.ContainsKey(item.Name))
            {
                return Items[item.Name];
            }

            Items.Add(item.Name, item);
            return null;
        }

        public static AhItem GetItem(String name)
        {
            return (from pair in Items where String.Equals(pair.Key, name, StringComparison.CurrentCultureIgnoreCase) select pair.Value).FirstOrDefault();
        }

        private static ProcessMemoryReader Preader
        {
            get
            {
                if (_preader == null)
                {
                    SetProcessMemoryReader();
                }
                return _preader;
            }
        }

        public static AhItem GetItem(int id)
        {
            return Items.Values.FirstOrDefault(item => item.ID == id);
        }

        private static IntPtr SigScan(string sig)
        {
            var pointer = Preader.FindSignature(sig, Constants.READ_LOC);
            if (pointer == IntPtr.Zero)
            {
                CampahStatus.Instance.Status = "Signature Failed";
            }
            return pointer;
        }

        public static void SetProcessMemoryReader()
        {
            _preader = new ProcessMemoryReader(CampahStatus.Instance.process.Id);
            var pointer = SigScan(Constants.AH_SIG);//+Constants.AH_OFFSETS[0];

            pointer = Constants.AH_OFFSETS.Aggregate(pointer, ReadPointer);

            AhStructPointer = pointer;
            BidValPointer = ReadPointer(ReadPointer(SigScan(Constants.BIDVAL_SIG),0),0);
        }

        private static IntPtr ArrayPointer
        {
            get
            {
                return ReadPointer((IntPtr)((int)AhStructPointer + Constants.AH_OFFSET_ARRAYSTRUCT),0);
            }
        }


        private static int Read2Bytes(IntPtr pointer, int offset)
        {
            int readcount;
            return BitConverter.ToInt16(Preader.ReadProcessMemory((IntPtr)((int)pointer + offset), 2, out readcount), 0);
        }

        private static IntPtr ReadPointer(IntPtr pointer, int offset)
        {
            int readcount;
            return (IntPtr)BitConverter.ToInt32(Preader.ReadProcessMemory((IntPtr)((int)pointer + offset), 4, out readcount), 0);
        }

        public static int LoadedCount
        {
            get
            {
                return Read2Bytes((IntPtr)((int)AhStructPointer + Constants.AH_OFFSET_ARRAYLOADED), 0);
            }
        }

        public static int UniqueCount
        {
            get
            {
                return Read2Bytes((IntPtr)((int)AhStructPointer + Constants.AH_OFFSET_ARRAYUNIQUELENGTH), 0);
            }
        }

        public static int[] ReadIDArray()
        {
            var itemids = new List<int>();
            var memloc = (int)ArrayPointer;
            var n = LoadedCount;
            MemoryBuffer buffer = Preader.createSearchBuffer(memloc, (Constants.AH_OFFSET_FIRSTITEMID
                                                                      + Constants.AH_OFFSET_ITEMID_INCREMENT * n));
            for (var i = 0; i < n; i++)
            {
                itemids.Add((int) buffer.Read2Bytes(memloc + Constants.AH_OFFSET_FIRSTITEMID + Constants.AH_OFFSET_ITEMID_INCREMENT*i));
            }

            CampahStatus.SetStatus("Item list read complete.");
            return itemids.ToArray();
        }
    }
}