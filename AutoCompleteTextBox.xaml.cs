using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CampahApp
{
    /// <summary>
    /// Interaction logic for AutoCompleteTextBox.xaml
    /// </summary>    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public partial class AutoCompleteTextBox
    {
       
        public AutoCompleteTextBox()
        {
            _controls = new VisualCollection(this);
            InitializeComponent();

            _autoCompletionList = new ObservableCollection<AutoCompleteEntry>();
            _searchThreshold = 2;        // default threshold to 2 char

            // set up the key press timer
            _keypressTimer = new System.Timers.Timer();
            _keypressTimer.Elapsed += OnTimedEvent;

            // set up the text box and the combo box
            ComboBox = new ComboBox
            {
                IsSynchronizedWithCurrentItem = true, 
                IsTabStop = false
            };

            ComboBox.SelectionChanged += comboBox_SelectionChanged;

            _textBox = new TextBox();
            _textBox.TextChanged += textBox_TextChanged;
            _textBox.VerticalContentAlignment = VerticalAlignment.Center;

            _controls.Add(ComboBox);
            _controls.Add(_textBox);
        }
        
        private VisualCollection _controls;
        private TextBox _textBox;
        public ComboBox ComboBox;
        private ObservableCollection<AutoCompleteEntry> _autoCompletionList;
        private System.Timers.Timer _keypressTimer;
        private delegate void TextChangedCallback();
        private bool _insertText;
        private int _delayTime;
        private int _searchThreshold;
        
        public string Text
        {
            get
            {
                    return _textBox.Text;
            }
            set 
            {
                _insertText = true;
                _textBox.Text = value;
                RunningData.Instance.CurrentItemText = value;
            }
        }

        public int DelayTime
        {
            get { return _delayTime; }
            set { _delayTime = value; }
        }

        public int Threshold
        {
            get { return _searchThreshold; }
            set { _searchThreshold = value; }
        }

        public void AddItem(AutoCompleteEntry entry)
        {
            _autoCompletionList.Add(entry);
        }

        public void ClearList()
        {
            _autoCompletionList.Clear();
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (null != ComboBox.SelectedItem)
            {
                _insertText = true;
                var cbItem = (ComboBoxItem)ComboBox.SelectedItem;
                _textBox.Text = cbItem.Content.ToString();
            }
        }

        private void TextChanged()
        {
            try
            {
                ComboBox.Items.Clear();
                if (_textBox.Text.Length >= _searchThreshold)
                {
                    foreach (var entry in _autoCompletionList)
                    {
                        if (
                            entry.KeywordStrings.Any(
                                word => word.StartsWith(_textBox.Text, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            var cbItem = new ComboBoxItem
                            {
                                Content = entry.ToString()
                            };
                            ComboBox.Items.Add(cbItem);
                        }
                    }
                    ComboBox.IsDropDownOpen = ComboBox.HasItems;
                }
                else
                {
                    ComboBox.IsDropDownOpen = false;
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private void OnTimedEvent(object source, System.Timers.ElapsedEventArgs e)
        {
            _keypressTimer.Stop();
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new TextChangedCallback(TextChanged));
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RunningData.Instance.CurrentItemText = _textBox.Text;
            // text was not typed, do nothing and consume the flag
            if (_insertText)
            {
                _insertText = false;
            }
            
            // if the delay time is set, delay handling of text changed
            else
            {
                if (_delayTime > 0)
                {
                    _keypressTimer.Interval = _delayTime;
                    _keypressTimer.Start();
                }
                else
                {
                    TextChanged();
                }
            }

            CurrentSelection.Name = _textBox.Text;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            _textBox.Arrange(new Rect(arrangeSize));
            ComboBox.Arrange(new Rect(arrangeSize));
            return base.ArrangeOverride(arrangeSize);
        }

        protected override Visual GetVisualChild(int index)
        {
            return _controls[index];
        }

        protected override int VisualChildrenCount
        {
            get { return _controls.Count; }
        }
    }
}