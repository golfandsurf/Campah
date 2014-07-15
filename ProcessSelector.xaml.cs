using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CampahApp
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ProcessSelector : IDisposable
    {
        public bool Running { get; set; }
        public Process[] processes;

        internal ProcessSelector()
        {
            InitializeComponent();
            Running = true;            
            loadList();
            processList.Focus();
        }

        public Process[] Processes
        {
            get
            {
                return processes.ToArray();
            }
        }

        private void loadList()
        {
            String[] processTitles = {"pol", "ffxi-boot"};
            var p = (from processTitle in processTitles
                from process in Process.GetProcessesByName(processTitle)
                where !Regex.IsMatch(process.MainWindowTitle, "(Final Fantasy XI)|(PlayOnline)")
                select process).ToArray();
            processes = p;
            processList.ItemsSource = p;
        }

        private void SelectItem()
        {
            CampahStatus.Instance.Process = (Process)processList.SelectedValue;
        }

        public void Dispose() 
        {
            processes = null;
            Close();
        }

        private void processList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (processList.SelectedItems.Count > 0)
            {
                SelectItem();
                Close();
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && processList.SelectedItems.Count > 0)
            {
                e.Handled = true;
                SelectItem();
                Close();
            }
        }

        private void Border_MouseUp(object sender, MouseButtonEventArgs e)
        {
            loadList();
        }
    }
}