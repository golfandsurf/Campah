using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Net;
using System.ComponentModel;
using System.Xml;
using FFACETools;
using Microsoft.Win32;

namespace CampahApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        Interaction interactionManager = new Interaction();
        FileIO settingsManager;

        public MainWindow()
        {
            InitializeComponent();

            Height = Properties.Settings.Default.WindowSize.Height;
            Width = Properties.Settings.Default.WindowSize.Width;
            Top = Properties.Settings.Default.WindowLocation.Y;
            Left = Properties.Settings.Default.WindowLocation.X;
            CampahStatus.SetStatus("Loading Settings", Modes.Stopped);
            settingsManager = new FileIO(TbBuyItemSelect);
            settingsManager.loadSettingsXML();

            if (File.Exists("Updater.exe"))
            {
                File.Delete("Updater.exe");
            }

            SelectProcess();
            rtb_chatlog.Document = Chatlog.Instance.ChatLog;            
        }

        private void SetMaxBid(int maxbid)
        {
            nb_maxBid.Value = maxbid;
        }

        private void SetMaxBidTag(bool isThinking)
        {
            ffxiah_button.Focusable = isThinking;
        }

        private void SetMinInc(int maxbid)
        {
            if (CampahStatus.Instance.OneClickMin < 100)
            {
                double minCoEff = CampahStatus.Instance.OneClickMin / 100.0;
                nb_minBid.Value = maxbid * minCoEff;
                nb_bidInc.Value = (int)((maxbid * (1 - minCoEff)) / CampahStatus.Instance.OneClickInc);
                if (nb_bidInc.Value > 99 && nb_bidInc.Value.ToString(CultureInfo.InvariantCulture).EndsWith("99"))
                {
                    nb_bidInc.Value++;
                }
            }
        }

        private void Deleteold()
        {
            RemoveOldFiles(Environment.CurrentDirectory + @"\"+UpdateConstants.UpdateFolder);
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.WindowSize = new System.Drawing.Size((int)Width, (int)Height);
            Properties.Settings.Default.WindowLocation = new System.Drawing.Point((int)Left, (int)Top);
            
            if (CampahStatus.Instance.CurrentPath != Environment.CurrentDirectory)
            {
                Properties.Settings.Default.lastfile = CampahStatus.Instance.CurrentPath;
            }

            Properties.Settings.Default.Save();
        }

        public void RemoveOldFiles(String updateFileDir)
        {
            // Root directory 
            var rootDirectory = new DirectoryInfo(Environment.CurrentDirectory);

            foreach (FileInfo oldFile in rootDirectory.GetFiles("*.old"))
            {
                oldFile.Delete();
            }

            // Sub directories
            foreach (var directory in rootDirectory.GetDirectories())
            {
                foreach (var oldFile in directory.GetFiles("*.old"))
                {
                    oldFile.Delete();
                }

                // Delete our temp directory
                if (directory.FullName == updateFileDir)
                {
                    try
                    {
                        directory.Delete(true);
                    }
                    catch (IOException)
                    {
                    }
                }
            }
        }

        public void OpenUpdater(StreamReader info)
        {
            var ud = new Updater(info);
            ud.ShowDialog();
            ud.Close();
            Close();
        }

        public bool DownloadUpdate(string file, string hash)
        {
            try
            {
                var client = new WebClient();
                var tempDir = new DirectoryInfo(UpdateConstants.UpdateFolder);
                if (!tempDir.Exists)
                {
                    tempDir.Create();
                }

                client.DownloadFile(UpdateConstants.UpdateUrl + "\\" + file, string.Format("{0}\\Package", tempDir));
                client.Dispose();
                return DTHasher.GetMD5Hash(string.Format("{0}\\Package\\", tempDir)) == hash;
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
                var req = (HttpWebRequest)WebRequest.Create(UpdateConstants.UpdateUrl+"update.txt");
                var response = (HttpWebResponse)req.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var cu = new StreamReader(response.GetResponseStream(), System.Text.Encoding.GetEncoding("utf-8"));
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
                        int subverL;
                        int subverC;
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
                }

            }
            catch (WebException)
            {
                CampahStatus.Instance.Status = "Could not contact update server";
            }
        }

        public void SelectProcess()
        {
            using (var p = new ProcessSelector())
            {
                switch (p.Processes.Length)
                {
                    case 1:
                        CampahStatus.Instance.Process = p.processes[0];
                        break;
                    case 0:
                        CampahStatus.Instance.Status = "No FFXI Processes Found";
                        return;
                    default:
                        p.ShowDialog();
                        break;
                }

                if (CampahStatus.Instance.Process == null)
                {
                    return;
                }

                FFACEInstance.Instance = null; //needs to dispose of old
                FFACEInstance.Instance = new FFACE(CampahStatus.Instance.Process.Id);
                CampahStatus.SetStatus("Attached to " + CampahStatus.Instance.Process.MainWindowTitle);
            }
        }

        private void CreateAhResourcesXml()
        {
            //GotoMenu("1");
            CampahStatus.SetStatus("Updating AH Database.  Please Wait...", Modes.Updating);
            AuctionHouse.Items.Clear();
            interactionManager.TraverseMenu("1");
            interactionManager.CloseMenu();
            var tw = new XmlTextWriter("ahresources.xml", null) {Formatting = Formatting.Indented};
            tw.WriteStartDocument();
            tw.WriteStartElement("AHStructure");
            tw.WriteComment("This XML was automatically generated by Campah.exe");
            tw.WriteComment("Editing this XML may cause Campah to no longer function properly");
            foreach (AhItem item in AuctionHouse.Items.Values)
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
            interactionManager.StopBuying("Finished Updating Successfully", CreateAhResourcesXml);
            settingsManager.loadAHResourcesXML();
        }

        private void TbBuyItemSelectKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Down)
            {
                return;
            }

            var acb = sender as AutoCompleteTextBox;
            if (acb != null && acb.ComboBox.SelectedIndex < 0)
            {
                acb.ComboBox.SelectedIndex = 0;
            }
        }

        private void ButtonAddBuyItemClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbBuyItemSelect.Text))
            {
                return;
            }
            if ((AuctionHouse.GetItem(TbBuyItemSelect.Text.Trim())) != null)
            {
                try
                {
                    RunningData.Instance.BidList.Add(new ItemRequest((int)nb_minBid.Value, (int)nb_maxBid.Value,(int)nb_bidInc.Value, (int)nb_quantity.Value, cb_stackable.IsChecked != null && (bool)cb_stackable.IsChecked, AuctionHouse.GetItem(TbBuyItemSelect.Text)));
                    RunningData.Instance.CalculateProjectedCost();
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
            if (CampahStatus.Instance.Mode != Modes.Stopped)
            {
                return;
            }

            var button = sender as Button;
            if (button != null)
            {
                var i = button.Tag as ItemRequest;
                RunningData.Instance.BidList.Remove(i);
            }

            RunningData.Instance.CalculateProjectedCost();
        }

        private void button_StartStop_Click(object sender, RoutedEventArgs e)
        {
            switch (CampahStatus.Instance.Mode)
            {
                case Modes.Stopped:
                    interactionManager.StartBuying();
                    break;
                case Modes.Buying:
                    interactionManager.StopBuying();
                    break;
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
            var input = (TextBox)sender;
            if (e.Key == Key.Enter)
            {
                FFACEInstance.Instance.Windower.SendString(input.Text);
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
            if (CampahStatus.Instance.Mode != Modes.Stopped)
            {
                return;
            }

            var button = sender as Button;
            if (button != null)
            {
                RunningData.Instance.AhTargetList.Remove(button.Tag as AhTarget);
            }
        }

        private void button_AddAHTarget_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tb_ahtargetname.Text))
            {
                string target = FFACEInstance.Instance.Target.Name;
                if (!string.IsNullOrEmpty(target.Trim()) && !RunningData.Instance.AhTargetList.Contains(new AhTarget(target)))
                    RunningData.Instance.AhTargetList.Add(new AhTarget(target));
            }
            else
                RunningData.Instance.AhTargetList.Add(new AhTarget(tb_ahtargetname.Text));
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
                var result = MessageBox.Show("The process takes about 3-10 mins to complete, depending on your computer's speed.\r\nMake sure you are within click range of an auction house and do not press any keys while it is working\r\n\r\nContinuing this operation will overwrite the current database, would you like to continue?", "Confirm Overwrite", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    ThreadManager.ThreadRunner(CreateAhResourcesXml);
                }
            }
            else
            {
                interactionManager.StopBuying("Stopped updating, reverting to previous database.", CreateAhResourcesXml);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            SaveSettings();
        }

        private void button_ResetWindow_Click(object sender, RoutedEventArgs e)
        {
            Height = 310; 
            Width = 700; 
            Top = 100; 
            Left = 100;
            SaveSettings();
        }

        private void button_DefaultSettings_Click(object sender, RoutedEventArgs e)
        {
            settingsManager.Defaultcampahsettings();
        }

        private void NumericSpinnerControl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var req = (ItemRequest)((NumericSpinnerControl)sender).Tag;
            req.NotifyPropertyChanged("Maximum");
            req.NotifyPropertyChanged("Minimum");
            req.NotifyPropertyChanged("Quantity");
            req.NotifyPropertyChanged("Increment");
        }

        private void ButtonRemoveClick(object sender, RoutedEventArgs e)
        {
            RunningData.Instance.BidList.Clear();
        }

        private void ButtonLoadClick(object sender, RoutedEventArgs e)
        {
            FileDialog dlg = new OpenFileDialog();
            dlg.Filter = Constants.CAMPAH_EXTENSION;
            dlg.InitialDirectory = CampahStatus.Instance.CurrentPath; // FileName;
            dlg.Title = "Open File";
            dlg.AddExtension = true;
            var showDialog = dlg.ShowDialog();
            if (showDialog != null && showDialog.Value)
            {
                settingsManager.LoadBidList(dlg.FileName);
            }
        }

        private void ButtonSaveClick(object sender, RoutedEventArgs e)
        {
            FileDialog dlg = new SaveFileDialog();
            dlg.Filter = Constants.CAMPAH_EXTENSION;
            dlg.InitialDirectory = CampahStatus.Instance.CurrentPath; // FileName;
            dlg.Title = "Save File";
            dlg.AddExtension = true;
            var showDialog = dlg.ShowDialog();
            if (showDialog != null && showDialog.Value)
            {
                settingsManager.saveBidList(dlg.FileName);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.lastfile))
            {
                CampahStatus.Instance.CurrentPath = Properties.Settings.Default.lastfile;
                if (CampahStatus.Instance.OpenLast)
                    settingsManager.LoadBidList(CampahStatus.Instance.CurrentPath);
            }
            if (App.mArgs != null && App.mArgs.Length > 0 && App.mArgs[0] == "updated")
            {
                ThreadManager.ThreadRunner(Deleteold);
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
                SelectProcess();
        }

        private void rtb_chatlog_MouseLeave(object sender, MouseEventArgs e)
        {
            ((RichTextBox)sender).ScrollToEnd();
        }

        private delegate void ChangeTagDelegate(bool isThinking);
        private delegate void ChangeMaxDelegate(int maxbid);
        private delegate void ChangeMinIncDelegate(int maxbid);
        private KeyValuePair<AhItem, bool> _lastlookupitem;
        private void LookupPriceXiAh()
        {
            AhItem item;
            var price = -1;
            if ((item = AuctionHouse.GetItem(RunningData.Instance.CurrentItemText.Trim())) != null)
            {
                CampahStatus.SetStatus("Looking up price for " + item.Name + "...");
                this.Dispatcher.BeginInvoke(new ChangeTagDelegate(SetMaxBidTag), new object[] { true });
                int sid;
                try
                {
                    sid = FFACEInstance.Instance.Player.GetSID;
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
                if (_lastlookupitem.Key == item && _lastlookupitem.Value == RunningData.Instance.CurrentItemStackable)
                {
                    Dispatcher.BeginInvoke(new ChangeMinIncDelegate(SetMinInc), new object[] { price });
                }
                else
                {
                    CampahStatus.SetStatus("Current median price for " + item.Name + " is " + price + "g.");
                    Dispatcher.BeginInvoke(new ChangeMaxDelegate(SetMaxBid), new object[] { price });
                }
            }
            _lastlookupitem = new KeyValuePair<AhItem, bool>(item, RunningData.Instance.CurrentItemStackable);
            Dispatcher.BeginInvoke(new ChangeTagDelegate(SetMaxBidTag), new object[] { false });
        }

        private void ffxiAH_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!ffxiah_button.Focusable)
            {
                ThreadManager.ThreadRunner(LookupPriceXiAh);
            }
        }
    }

    public static class FFACEInstance
    {
        public static FFACE Instance { get; set; }
    }
}
