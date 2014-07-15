using System.ComponentModel;

namespace CampahApp
{
    public class ItemRequest : INotifyPropertyChanged
    {
        public ItemRequest(int min, int max, int inc, int qnty, bool stack, AhItem itemdata)
        {
            Minimum = min;
            Maximum = max;
            Increment = inc;
            Quantity = qnty;
            Stack = stack;
            ItemData = itemdata;
            _boughtCost = 0;
            _boughtCount = 0;
        }

        public int Minimum { get; set; }

        public int Maximum { get; set; }

        public int Increment { get; set; }

        public int Quantity { get; set; }

        public bool Stack { get; set; }

        public AhItem ItemData { get; private set; }

        private int _boughtCost;

        public int BoughtCount
        {
            get
            {
                return _boughtCount;
            }
            set
            {
                _boughtCount = value;
                NotifyPropertyChanged("BoughtCount");
            }
        }

        private int _boughtCount;
        public int BoughtCost
        {
            get
            {
                return _boughtCost;
            }
            set
            {
                _boughtCost = value;
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
}