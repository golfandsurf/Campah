using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;
using System.Net;
using System.ComponentModel;
using System.Xml;
using WPFAutoCompleteTextbox;
using FFACETools;
using PandyProductions;
using Microsoft.Win32;

namespace CampahApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Interaction interactionManager = new Interaction();
        FileIO settingsManager;

        public MainWindow()
        {
            InitializeComponent();
            int[] REQUIRE_FFACE_VER = { 4, 1, 0, 24 };


            this.Height = Properties.Settings.Default.WindowSize.Height;
            this.Width = Properties.Settings.Default.WindowSize.Width;
            this.Top = Properties.Settings.Default.WindowLocation.Y;
            this.Left = Properties.Settings.Default.WindowLocation.X;
            CampahStatus.SetStatus("Loading Settings", Modes.Stopped);
            settingsManager = new FileIO(tbBuyItemSelect);
            settingsManager.loadSettingsXML();
            if (CampahStatus.Instance.AutomaticUpdates && (App.mArgs.Length == 0 || App.mArgs[0] != "updated"))
            {
                //CheckUpdate();  //No longer supported
            }
            String[] ffacever = FileVersionInfo.GetVersionInfo("FFACE.dll").FileVersion.Split(',');
            for (int i = 0; i < ffacever.Length; i++)
            {
                if(int.Parse(ffacever[i]) < REQUIRE_FFACE_VER[i])
                {
                    MessageBox.Show("Campah Requires FFACE.dll version 4.1.0.24 or higher.  Please download the latest version from ffevo forums");
                    Application.Current.Shutdown();
                }
            }
            if (File.Exists("Updater.exe"))
                File.Delete("Updater.exe");
            selectProcess();
            rtb_chatlog.Document = Chatlog.Instance.chatlog;            
        }

        private void setMaxBid(int maxbid)
        {
            nb_maxBid.Value = maxbid;
        }

        private void setMaxBidTag(bool isThinking)
        {
            ffxiah_button.Focusable = isThinking;
        }

        private void setMinInc(int maxbid)
        {
            if (CampahStatus.Instance.OneClickMin < 100)
            {
                double minCoEff = CampahStatus.Instance.OneClickMin / 100.0;
                nb_minBid.Value = maxbid * minCoEff;
                nb_bidInc.Value = (int)((maxbid * (1 - minCoEff)) / CampahStatus.Instance.OneClickInc);
                if (nb_bidInc.Value > 99 && nb_bidInc.Value.ToString().EndsWith("99"))
                    nb_bidInc.Value++;
            }
        }

        private void deleteold()
        {
            RemoveOldFiles(Environment.CurrentDirectory + @"\"+UpdateConstants.UPDATE_FOLDER);
        }

        private void savesettings()
        {
            Properties.Settings.Default.WindowSize = new System.Drawing.Size((int)this.Width, (int)this.Height);
            Properties.Settings.Default.WindowLocation = new System.Drawing.Point((int)this.Left, (int)this.Top);
            if (CampahStatus.Instance.CurrentPath != Environment.CurrentDirectory)
                Properties.Settings.Default.lastfile = CampahStatus.Instance.CurrentPath;
            Properties.Settings.Default.Save();
        }

        public void RemoveOldFiles(String UpdateFileDir)
        {
            /*
                Root directory
             */
            DirectoryInfo RootDirectory = new DirectoryInfo(Environment.CurrentDirectory);

            foreach (FileInfo OldFile in RootDirectory.GetFiles("*.old"))
                OldFile.Delete();
            /*Sub directories*/
            foreach (DirectoryInfo Directory in RootDirectory.GetDirectories())
            {
                foreach (FileInfo OldFile in Directory.GetFiles("*.old"))
                    OldFile.Delete();
                /*Delete our temp directory*/
                if (Directory.FullName == UpdateFileDir)
                {
                    try
                    {
                        Directory.Delete(true);
                    }
                    catch (IOException)
                    {
                        continue;
                    }
                }
            }
        }

        public void OpenUpdater(StreamReader info)
        {
            Updater UD = new Updater(info);
            UD.ShowDialog();
            UD.Close();
            this.Close();
        }

        public bool DownloadUpdate(String File, String Hash)
        {
            try
            {
                
                WebClient Client = new WebClient();
                DirectoryInfo TempDir = new DirectoryInfo(UpdateConstants.UPDATE_FOLDER);
                if (!TempDir.Exists)
                    TempDir.Create();
                Client.DownloadFile(UpdateConstants.UPDATE_URL + "\\" + File, string.Format("{0}\\Package", TempDir));
                Client.Dispose();
                if (DTHasher.GetMD5Hash(string.Format("{0}\\Package\\", TempDir)) == Hash)
                {
                    return true;
                }
                else 
                    return false;
            }
            catch
            {
                return false;
            }
        }
        

        public void CheckUpdate()
        {
            try
            {
                CampahStatus.Instance.Status = "Checking for Updates...";
                HttpWebRequest req = (HttpWebRequest)System.Net.WebRequest.Create(UpdateConstants.UPDATE_URL+"update.txt");
                HttpWebResponse response = (HttpWebResponse)req.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader cu = new StreamReader(response.GetResponseStream(), System.Text.Encoding.GetEncoding("utf-8"));
                    String[] latestversion = cu.ReadLine().Split('.');
                    String[] currentverstion = CampahStatus.Instance.Version.Split('.');
                    cu.Close();
                    if (currentverstion.Length != latestversion.Length)
                    {
                        CampahStatus.Instance.Status = "A problem occured while checking update, canceling update";
                        return;
                    }

                    for (int i = 0; i < latestversion.Length; i++)
                    {
                        int subverL = -2;
                        int subverC = -1;
                        int.TryParse(latestversion[i], out subverL);
                        int.TryParse(currentverstion[i], out subverC);
                        if (subverL > subverC)
                        {
                            OpenUpdater(cu);
                            break;
                        }
                    }
                    CampahStatus.Instance.Status = "No update needed";
                }
                else
                {
                    CampahStatus.Instance.Status = "Could not contact update server";
                    return;
                }

            }
            catch (WebException)
            {
                CampahStatus.Instance.Status = "Could not contact update server";
                return;
            }
        }

        int check = 0;
        string lastitem = "";
        void ffxiah_lookup_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            String cursel = CurrentSelection.Name;
            if (AuctionHouse.items.ContainsKey(CurrentSelection.Name))
            {
                if (lastitem == CurrentSelection.Name)
                    check++;
                else
                    check = 0;
                if (check == 3)
                {
                   // AHItem item = AuctionHouse.GetItem(CurrentSelection.Name);
                    //CampahStatus.Instance.Price = FFXIAH.LookupMedian(item.ID, FFACE_INSTANCE.Instance.Player.PlayerServerID, item.Stackable).ToString() + "g";
                }
                lastitem = CurrentSelection.Name;
            }
        }

        public void selectProcess()
        {
            using (ProcessSelector p = new ProcessSelector())
            {
                //this.Topmost = false;
                if (p.Processes.Length == 1)// && autoAttach)
                {
                    CampahStatus.Instance.Process = p.processes[0];
                }
                else if (p.Processes.Length == 0)// && autoAttach)
                {
                    CampahStatus.Instance.Status = "No FFXI Processes Found";
                    return;
                }
                else
                    p.ShowDialog();
                //this.Topmost = (bool)cb_alwaystop.IsChecked;
                if (CampahStatus.Instance.Process != null)
                {
                    if (FFACE_INSTANCE.Instance != null)
                        FFACE_INSTANCE.Instance = null;  //needs to dispose of old
                    FFACE_INSTANCE.Instance = new FFACE(CampahStatus.Instance.Process.Id);
                    CampahStatus.SetStatus("Attached to " + CampahStatus.Instance.Process.MainWindowTitle);
                }
            }
        }

        private void createAHResourcesXML()
        {
//            GotoMenu("1");
            CampahStatus.SetStatus("Updating AH Database.  Please Wait...", Modes.Updating);
            AuctionHouse.items.Clear();
            interactionManager.TraverseMenu("1");
            interactionManager.CloseMenu();
            XmlTextWriter tw = new XmlTextWriter("ahresources.xml", null);
            tw.Formatting = Formatting.Indented;
            tw.WriteStartDocument();
            tw.WriteStartElement("AHStructure");
            tw.WriteComment("This XML was automatically generated by Campah.exe");
            tw.WriteComment("Editing this XML may cause Campah to no longer function properly");
            foreach (AHItem item in AuctionHouse.items.Values)
            {
                tw.WriteStartElement("item");
                tw.WriteAttributeString("id", item.ID.ToString("X2"));
                tw.WriteAttributeString("name", item.Name);
                tw.WriteAttributeString("stackable", item.Stackable.ToString());
                tw.WriteAttributeString("address", item.Address);
                tw.WriteEndElement();
            }
            tw.WriteEndElement();
            tw.WriteEndDocument();
            tw.Flush();
            tw.Close();
            interactionManager.StopBuying("Finished Updating Successfully", createAHResourcesXML);
            settingsManager.loadAHResourcesXML();
        }

        private void tbBuyItemSelect_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                AutoCompleteTextBox acb = sender as AutoCompleteTextBox;
                if (acb.comboBox.SelectedIndex < 0)
                    acb.comboBox.SelectedIndex = 0;
            }
        }

        private void button_AddBuyItem_Click(object sender, RoutedEventArgs e)
        {
            AHItem item;
            if (string.IsNullOrEmpty(tbBuyItemSelect.Text))
                return;
            if ((item = AuctionHouse.GetItem(tbBuyItemSelect.Text.Trim())) != null)
            {
                try
                {
                    RunningData.Instance.BidList.Add(new ItemRequest((int)nb_minBid.Value, (int)nb_maxBid.Value,
                            (int)nb_bidInc.Value, (int)nb_quantity.Value, (bool)cb_stackable.IsChecked,
                            AuctionHouse.GetItem(tbBuyItemSelect.Text)));
                    RunningData.Instance.calculateProjectedCost();
                }
                catch
                {
                    CampahStatus.Instance.Status = "Invalid item selections, item not added.";
                }
            }
            else
            {
                CampahStatus.Instance.Status = "Invalid item name, item not added.";
            }
        }

        private void button_RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (CampahStatus.Instance.Mode == Modes.Stopped)
            {
                ItemRequest m = (sender as Button).Tag as ItemRequest;
                RunningData.Instance.BidList.Remove(m);
                RunningData.Instance.calculateProjectedCost();
            }
        }

        private void button_StartStop_Click(object sender, RoutedEventArgs e)
        {
            Button ctrl = sender as Button;
            if (CampahStatus.Instance.Mode == Modes.Stopped)
            {
                interactionManager.StartBuying();
            }
            else if (CampahStatus.Instance.Mode == Modes.Buying)
            {
                interactionManager.StopBuying();
            }
        }

        private void rtb_chatlog_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!((RichTextBox)sender).IsMouseOver)
            {
                ((RichTextBox)sender).ScrollToEnd();
            }
        }

        private void textBox_ChatlogInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox input = (TextBox)sender;
            if (e.Key == Key.Enter)
            {
                FFACE_INSTANCE.Instance.Windower.SendString(input.Text);
                ChatInputBuffer.AddLine(input.Text);
            }
            
            if (e.Key == Key.Escape||e.Key == Key.Enter)
            {
                input.Clear();
                e.Handled = true;
            }

            if (e.Key == Key.Up)
                input.Text = ChatInputBuffer.Up(input.Text);
            if (e.Key == Key.Down)
                input.Text = ChatInputBuffer.Down();
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control || (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                Regex findname;
                int fields;
                string name = "";
                if (input.Text.Length >= 2 && input.Text.Substring(0, 2).ToLower() == "/t")
                {
                    findname = new Regex(@"/\S* (\S*)\s?(.*)");
                    fields = 2;
                }
                else
                {
                    findname = new Regex(@"/.*? (.*)");
                    fields = 1;
                }
                string text = input.Text.Trim();
                if (findname.IsMatch(input.Text))
                {
                    text = findname.Matches(input.Text)[0].Groups[fields].Value.Trim();
                    if (fields > 1)
                        name = findname.Matches(input.Text)[0].Groups[1].Value.Trim();
                }

                if (e.Key == Key.R)
                    input.Text = "/t " + Chatlog.Instance.LastTell(name) + " " + text;
                else if (e.Key == Key.T)
                    input.Text = "/t " + text;
                else if (e.Key == Key.P)
                    input.Text = "/p " + text;
                else if (e.Key == Key.L)
                    input.Text = "/l " + text;
                else if (e.Key == Key.S)
                    input.Text = "/s " + text;


                input.CaretIndex = input.Text.Length;
            }
        }

        private void button_RemoveAHTarget_Click(object sender, RoutedEventArgs e)
        {
            if (CampahStatus.Instance.Mode == Modes.Stopped)
                RunningData.Instance.AHTargetList.Remove((sender as Button).Tag as AHTarget);
        }

        private void button_AddAHTarget_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tb_ahtargetname.Text))
            {
                string target = FFACE_INSTANCE.Instance.Target.Name;
                if (!string.IsNullOrEmpty(target.Trim()) && !RunningData.Instance.AHTargetList.Contains(new AHTarget(target)))
                    RunningData.Instance.AHTargetList.Add(new AHTarget(target));
            }
            else
                RunningData.Instance.AHTargetList.Add(new AHTarget(tb_ahtargetname.Text));
            tb_ahtargetname.Clear();
        }

        private void button_SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                settingsManager.SaveCampahSettings();
                CampahStatus.Instance.Status = "Saved Settings";
            }
            catch
            {
                CampahStatus.Instance.Status = "Error! A problem occured while saving settings.";
            }
        }

        private void button_UpdateAHdatabase_Click(object sender, RoutedEventArgs e)
        {
            if (CampahStatus.Instance.Mode == Modes.Stopped)
            {
                MessageBoxResult result = MessageBox.Show("The process takes about 3-10 mins to complete, depending on your computer's speed.\r\nMake sure you are within click range of an auction house and do not press any keys while it is working\r\n\r\nContinuing this operation will overwrite the current database, would you like to continue?", "Confirm Overwrite", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                    ThreadManager.threadRunner(createAHResourcesXML);
            }
            else
            {
                interactionManager.StopBuying("Stopped updating, reverting to previous database.", createAHResourcesXML);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            savesettings();
        }

        private void button_ResetWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Height = 310; this.Width = 700; this.Top = 100; this.Left = 100;
            savesettings();
        }

        private void button_DefaultSettings_Click(object sender, RoutedEventArgs e)
        {
            settingsManager.defaultcampahsettings();
        }

        private void NumericSpinnerControl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ItemRequest req = (ItemRequest)((NumericSpinnerControl)sender).Tag;
            req.NotifyPropertyChanged("Maximum");
            req.NotifyPropertyChanged("Minimum");
            req.NotifyPropertyChanged("Quantity");
            req.NotifyPropertyChanged("Increment");
        }

        private void button_Remove_Click(object sender, RoutedEventArgs e)
        {
            RunningData.Instance.BidList.Clear();
        }

        private void button_Load_Click(object sender, RoutedEventArgs e)
        {
            FileDialog dlg = new OpenFileDialog();
            dlg.Filter = Constants.CAMPAH_EXTENSION;
            dlg.InitialDirectory = CampahStatus.Instance.CurrentPath; // FileName;
            dlg.Title = "Open File";
            dlg.AddExtension = true;
            if ((bool)dlg.ShowDialog())
                settingsManager.loadBidList(dlg.FileName);
        }

        private void button_Save_Click(object sender, RoutedEventArgs e)
        {
            FileDialog dlg = new SaveFileDialog();
            dlg.Filter = Constants.CAMPAH_EXTENSION;
            dlg.InitialDirectory = CampahStatus.Instance.CurrentPath; // FileName;
            dlg.Title = "Save File";
            dlg.AddExtension = true;
            if ((bool)dlg.ShowDialog())
                settingsManager.saveBidList(dlg.FileName);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.lastfile))
            {
                CampahStatus.Instance.CurrentPath = Properties.Settings.Default.lastfile;
                if (CampahStatus.Instance.OpenLast)
                    settingsManager.loadBidList(CampahStatus.Instance.CurrentPath);
            }
            if (App.mArgs != null && App.mArgs.Length > 0 && App.mArgs[0] == "updated")
            {
                ThreadManager.threadRunner(deleteold);
            }
        }

        private void TextBox_HistoryLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            ((TextBox)sender).ScrollToEnd();
        }

        private void TextBox_ChatFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            Chatlog.Instance.UpdateFilters(CampahStatus.Instance.ChatFilter);
        }

        private void button_Attack_Click(object sender, RoutedEventArgs e)
        {
                selectProcess();
        }

        private void rtb_chatlog_MouseLeave(object sender, MouseEventArgs e)
        {
            ((RichTextBox)sender).ScrollToEnd();
        }

        private delegate void ChangeTagDelegate(bool isThinking);
        private delegate void ChangeMaxDelegate(int maxbid);
        private delegate void ChangeMinIncDelegate(int maxbid);
        private KeyValuePair<AHItem, bool> lastlookupitem;
        private void lookupPriceXIAH()
        {
            AHItem item;
            int price = -1;
            if ((item = AuctionHouse.GetItem(RunningData.Instance.CurrentItemText.Trim())) != null)
            {
                CampahStatus.SetStatus("Looking up price for " + item.Name + "...");
                this.Dispatcher.BeginInvoke(new ChangeTagDelegate(setMaxBidTag), new object[] { true });
                int sid;
                try
                {
                    sid = FFACE_INSTANCE.Instance.Player.GetSID;
                }
                catch
                {
                    CampahStatus.SetStatus("Unable to resolve player server, using default server. Requires FFACE v4.0.1.18");
                    sid = 0;
                }
                price = FFXIAH.LookupMedian(item.ID, sid + 1, 
                    (RunningData.Instance.CurrentItemStackable && item.Stackable), CampahStatus.Instance.WebTimeout);
            }
            if (price > -1)
            {
                if (lastlookupitem.Key == item && lastlookupitem.Value == RunningData.Instance.CurrentItemStackable)
                {
                    this.Dispatcher.BeginInvoke(new ChangeMinIncDelegate(setMinInc), new object[] { price });
                }
                else
                {
                    CampahStatus.SetStatus("Current median price for " + item.Name + " is " + price + "g.");
                    this.Dispatcher.BeginInvoke(new ChangeMaxDelegate(setMaxBid), new object[] { price });
                }
            }
            lastlookupitem = new KeyValuePair<AHItem, bool>(item, RunningData.Instance.CurrentItemStackable);
            this.Dispatcher.BeginInvoke(new ChangeTagDelegate(setMaxBidTag), new object[] { false });
        }

        private void ffxiAH_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!ffxiah_button.Focusable)
                ThreadManager.threadRunner(lookupPriceXIAH);
        }
    }

    public static class FFACE_INSTANCE
    {
        public static FFACE Instance { get; set; }
    }

    public class RunningData : INotifyPropertyChanged
    {
        public ObservableCollection<ItemRequest> BidList { get; set; }
        public ObservableCollection<AHTarget> AHTargetList { get; set; }
        public Queue<ItemRequest> TrashCan { get; set; }
        int projectedCost;
        int totalSpent;
        DispatcherTimer trashcollector;
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
            AHTargetList = new ObservableCollection<AHTarget>();
            TrashCan = new Queue<ItemRequest>();
            trashcollector = new DispatcherTimer();
            trashcollector.Interval = TimeSpan.FromMilliseconds(1000);
            trashcollector.Tick += new EventHandler(trashcollector_Tick);
            trashcollector.Start();
        }

        void trashcollector_Tick(object sender, EventArgs e)
        {
            while (TrashCan.Count > 0)
            {
                BidList.Remove(TrashCan.Dequeue());
            }
        }

        public void calculateProjectedCost()
        {
            ProjectedCost = 0;
            foreach (ItemRequest item in BidList)
                ProjectedCost += (item.Maximum*item.Quantity);
        }

        public int ProjectedCost
        {
            get
            {
                return projectedCost;
            }
            set
            {
                projectedCost = value;
                NotifyPropertyChanged("ProjectedCost");
            }
        }

        public int TotalSpent
        {
            get
            {
                return totalSpent;
            }
            set
            {
                totalSpent = value;
                NotifyPropertyChanged("TotalSpent");
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
