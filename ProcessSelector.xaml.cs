using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
            Process[] ps = Process.GetProcessesByName("pol");
            List<Process> p = new List<Process>();
            foreach (Process process in ps)
            {
                if (!Regex.IsMatch(process.MainWindowTitle, "(Final Fantasy XI)|(PlayOnline)"))
                {
                    p.Add(process);
                }
            }
            this.processes = p.ToArray();
            processList.ItemsSource = p.ToArray();
        }

        private void selectItem()
        {
            CampahStatus.Instance.Process = (Process)processList.SelectedValue;
        }

        public void Dispose() 
        {
            processes = null;
            this.Close();
        }

        private void processList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (processList.SelectedItems.Count > 0)
            {
                selectItem();
                this.Close();
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && processList.SelectedItems.Count > 0)
            {
                e.Handled = true;
                selectItem();
                this.Close();
            }
        }

        private void Border_MouseUp(object sender, MouseButtonEventArgs e)
        {
            loadList();
        }
    }
}