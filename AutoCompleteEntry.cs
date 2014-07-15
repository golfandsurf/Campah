namespace CampahApp
{
    public class AutoCompleteEntry
    {
        private string[] _keywordStrings;
        private string _displayString;

        public string[] KeywordStrings
        {
            get { return _keywordStrings ?? (_keywordStrings = new[] {_displayString}); }
        }

        public string DisplayName
        {
            get { return _displayString; }
            set { _displayString = value; }
        }

        public AutoCompleteEntry(string name, params string[] keywords)
        {
            _displayString = name;
            _keywordStrings = keywords;
        }

        public override string ToString()
        {
            return _displayString;
        }
    }
}
