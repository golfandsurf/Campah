using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;

namespace CampahApp
{
    public class RunningData : INotifyPropertyChanged
    {
        int _projectedCost;

        int _totalSpent;

        public ObservableCollection<ItemRequest> BidList { get; set; }

        public ObservableCollection<AhTarget> AhTargetList { get; set; }

        public Queue<ItemRequest> TrashCan { get; set; }
        
        public static RunningData Instance { get;set; }

        public string CurrentItemText { get; set; }

        public bool CurrentItemStackable { get; set; }

        static RunningData()
        {
            Instance = new RunningData();
        }
        
        public RunningData()
        {
            CurrentItemStackable = false;
            CurrentItemText = "";
            BidList = new ObservableCollection<ItemRequest>();
            AhTargetList = new ObservableCollection<AhTarget>();
            TrashCan = new Queue<ItemRequest>();
            var trashcollector = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            trashcollector.Tick += TrashCollector_Tick;
            trashcollector.Start();
        }

        void TrashCollector_Tick(object sender, EventArgs e)
        {
            while (TrashCan.Count > 0)
            {
                BidList.Remove(TrashCan.Dequeue());
            }
        }

        public void CalculateProjectedCost()
        {
            ProjectedCost = 0;
            foreach (var item in BidList)
            {
                ProjectedCost += (item.Maximum*item.Quantity);
            }
        }

        public int ProjectedCost
        {
            get
            {
                return _projectedCost;
            }
            set
            {
                _projectedCost = value;
                NotifyPropertyChanged("ProjectedCost");
            }
        }

        public int TotalSpent
        {
            get
            {
                return _totalSpent;
            }
            set
            {
                _totalSpent = value;
                NotifyPropertyChanged("TotalSpent");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}