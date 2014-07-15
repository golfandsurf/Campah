using System;

namespace CampahApp
{
    public class AhItem
    {
        public AhItem(int id, String name, bool stackable, string address)
        {
            ID = id;
            Name = name;
            Stackable = stackable;
            Address = address;
        }

        public int ID { get; private set; }

        public string Name { get; protected set; }

        public bool Stackable { get; set; }

        public string Address { get; private set; }

        public override bool Equals(object obj)
        {
            return ((AhItem)obj).ID == ID;
        }

        protected bool Equals(AhItem other)
        {
            return ID == other.ID;
        }

        public override int GetHashCode()
        {
            return ID;
        }
    }
}