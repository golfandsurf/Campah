using System;
using System.Collections.Generic;

namespace CampahApp
{
    internal class FFXiAhItem
    {
        private FFXiAhItem(int item, bool stack)
        {
            ID = item;
            Stack = stack;
            _price = -1;
            _priceset = DateTime.Now;
        }

        private int _price;

        private DateTime _priceset;

        public int ID { get; private set; }

        public bool Stack { get; private set; }

        private static List<FFXiAhItem> ItemList;

        static FFXiAhItem()
        {
            ItemList = new List<FFXiAhItem>();
        }

        private static FFXiAhItem AddItem(int item, bool stack)
        {
            var newitem = new FFXiAhItem(item, stack);
            ItemList.Add(newitem);
            return newitem;
        }

        public static FFXiAhItem GetItemByID(int item, bool stack)
        {
            foreach (var i in ItemList)
            {
                if (i.ID == item && i.Stack == stack)
                {
                    return i;
                }
            }

            return AddItem(item, stack);
        }

        public int Price
        {
            get
            {
                if (_price > -1 && (DateTime.Now - _priceset) > TimeSpan.FromHours(1))
                    _price = -1;
                return _price;
            }
            set
            {
                _price = value;
                _priceset = DateTime.Now;
            }
        }
    }
}