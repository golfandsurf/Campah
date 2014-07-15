using System;
using System.ComponentModel;

namespace CampahApp
{
    public class UpdateData : INotifyPropertyChanged
    {
        public UpdateData()
        {
            _name = "";
            _comment = "";
            File = "";
            RelPath = UpdateConstants.UpdateFolder;
            _status = "Beginning Update";
            _completed = false;
            Url = UpdateConstants.UpdateUrl;
        }

        public string File { get; set; }

        public string RelPath { get; set; }

        public string Url { get; set; }

        public string Hash { get; set; }

        public string Version { get; set; }

        public static UpdateData Instance { get; private set; }

        static UpdateData()
        {
            Instance = new UpdateData();
        }

        private string _name;
        public String Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                NotifyPropertyChanged("Name");
            }
        }

        private bool _completed;

        public bool Completed
        {
            get
            {
                return _completed;
            }
            set
            {
                _completed = value;
                NotifyPropertyChanged("Completed");
            }
        }

        private string _status;
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
            }
        }

        private string _comment;
        public String Comment
        {
            get
            {
                return _comment;
            }
            set
            {
                _comment = value;
                NotifyPropertyChanged("Comment");
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