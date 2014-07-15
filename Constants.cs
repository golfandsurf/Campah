using System;

namespace CampahApp
{
    public static class Constants
    {
        public const String BIDVAL_SIG = "8b0d????????33??84db";
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
}