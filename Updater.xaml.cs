using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Windows.Shapes;
using System.Xml;
using System.Net;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.IO.Compression;
using System.IO;
using SharpCompress.Archive;
using SharpCompress.Reader;

namespace CampahApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Updater : Window
    {
        System.Windows.Threading.DispatcherTimer worker;
        StreamReader Info;
        public Updater()
        {
            InitializeComponent();
        }

        public delegate void ChangeTextBoxDelegate(String NewText);
        public void ChangeTextBox(String NewText)
        {
            UpdateData.Instance.Status += "\r\n" + NewText;
            tb_update.InvalidateVisual();
        }

        public void addText(String text)
        {
            this.Dispatcher.BeginInvoke(new ChangeTextBoxDelegate(ChangeTextBox), new object[] { text });
        }

        public Updater(StreamReader cu)
        {
            InitializeComponent();
            Info = cu;
            addText("Downloading version information...");
            if (!CheckUpdate())
            {
                addText("Update Failed. 001");
                return;
            }

            addText("Downloading update package...");
            worker = new DispatcherTimer();
            worker.Interval = TimeSpan.FromMilliseconds(300);
            worker.Tick += new EventHandler(worker_Tick);
        }

        void worker_Tick(object sender, EventArgs e)
        {
            if (!working)
                Manager();
            UpdateData.Instance.NotifyPropertyChanged("Status");            
        }
        bool working;
        public void Manager()
        {
            working = true;
            //CloseProcesses();
            DirectoryInfo TempDir = new DirectoryInfo(UpdateData.Instance.RelPath);
            if (DownloadUpdate(TempDir))
            {
                addText("Downloaded Complete.");
            }
            else
            {
                addText("Update Failed. 002");
                return;
            }
            addText("Unpacking update package...");
            DoFileUpdate(TempDir.FullName, string.Format("{0}\\Package", TempDir.FullName));
            //RemoveOldFiles(TempDir.FullName);
            arg2 = TempDir.FullName;
            UpdateData.Instance.Completed = true;
            RestartMainApp();
            
        }
        String arg2 = "";

        private void CloseProcesses()
        {
            
            Process[] ps = Process.GetProcessesByName("Campah");
            List<Process> p = new List<Process>();
            foreach (Process process in ps)
            {
                process.CloseMainWindow();
            }
            
        }

        public bool CheckUpdate()
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)System.Net.WebRequest.Create(UpdateData.Instance.URL + "update.txt");
                HttpWebResponse response = (HttpWebResponse)req.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader cu = new StreamReader(response.GetResponseStream(), System.Text.Encoding.GetEncoding("utf-8"));
                    UpdateData.Instance.Version = cu.ReadLine();
                    UpdateData.Instance.File = cu.ReadLine();
                    UpdateData.Instance.Hash = cu.ReadLine();
                    UpdateData.Instance.Name = cu.ReadLine();
                    UpdateData.Instance.Comment = cu.ReadToEnd();
                    cu.Close();
                }
                return true;
            }
            catch (WebException)
            {
                return false;
            }
        }

        public bool DownloadUpdate(DirectoryInfo TempDir)
        {
            try
            {
                WebClient Client = new WebClient();
                if (!TempDir.Exists)
                    TempDir.Create();
                addText(UpdateData.Instance.URL + UpdateData.Instance.File);
                Client.DownloadFile(UpdateData.Instance.URL + UpdateData.Instance.File, string.Format("{0}\\Package", TempDir.FullName));
                if (DTHasher.GetMD5Hash(string.Format("{0}\\Package", TempDir.FullName)) == UpdateData.Instance.Hash)
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


        private void DoFileUpdate(string TempPath, string Package)
        {
            List<string> FilesToReplace = new List<string>();
            IArchive archive = ArchiveFactory.Open(Package, SharpCompress.Common.Options.KeepStreamsOpen);
            bool HaveUpdateFile = false;
            foreach (var entry in archive.Entries)
            {
                if (!entry.IsDirectory)
                {
                    if (entry.FilePath.ToLower().Contains("update.xml"))
                    {
                        entry.WriteToDirectory(TempPath, SharpCompress.Common.ExtractOptions.Overwrite);
                        FilesToReplace = this.GetFilesToReplace(new FileInfo(string.Format("{0}\\Update.xml", TempPath)).OpenRead());
                        HaveUpdateFile = true;
                    }
                }
            }

            if (HaveUpdateFile)
            {
                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        FileInfo OldFile = new FileInfo(entry.FilePath);
                        /*if in update.xml or doesn't exist and isn't the update.xml*/
                        if (FilesToReplace.Contains(entry.FilePath) || !(OldFile.Exists) && !(entry.FilePath.ToLower().Contains("update.xml")))
                        {
                            addText(string.Format("Updating file [{0}]", entry.FilePath));
//                            Debug.WriteLine(string.Format("DoFileUpdate -> Update.xml = TRUE; Replacing {0}", entry.FilePath));

                            if (OldFile.Exists && !File.Exists(OldFile.FullName + ".old"))
                            {
                                OldFile.MoveTo(string.Format("{0}.old", OldFile.Name));
                                entry.WriteToDirectory(".\\", SharpCompress.Common.ExtractOptions.Overwrite | SharpCompress.Common.ExtractOptions.ExtractFullPath);
                            }
                            //else
                                //fail
                        }
                    }
                }
            }
            else
            {
                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        /*no update.xml in main folder*/
                        if (!entry.FilePath.ToLower().Contains("update.xml"))
                        {
                            addText(string.Format("Updating file [{0}]", entry.FilePath));
                            //Debug.WriteLine(string.Format("DoFileUpdate -> Update.xml = FALSE; Replacing {0}", entry.FilePath));
                            FileInfo OldFile = new FileInfo(entry.FilePath);
                            if (OldFile.Exists)
                                OldFile.MoveTo(string.Format("{0}.old", OldFile.Name));
                            entry.WriteToDirectory(".\\", SharpCompress.Common.ExtractOptions.ExtractFullPath | SharpCompress.Common.ExtractOptions.Overwrite);
                        }
                    }
                }
            }
            archive.Dispose();
        }

        public void RemoveOldFiles(String UpdateFileDir)
        {
            /*
                Root directory
                Note: foreach is slow, may need to replace with for
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

        private void RestartMainApp()
        {
            if ((bool)cb_closewhencomplete.IsChecked)
                this.Close();
        }

        void MainContainer_Loaded(object sender, RoutedEventArgs e)
        {
                worker.Start();
        }

        private List<string> GetFilesToReplace(Stream AutoUpdateFile)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(AutoUpdateFile);
            XmlNodeList Files;
            Files = xDoc.GetElementsByTagName("File");
            List<string> Result = new List<string>();
            foreach (XmlNode File in Files)
            {
                Result.Add(File.Attributes["Filename"].Value);
            }
            AutoUpdateFile.Close();
            return Result;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (!tb.IsMouseOver)
                tb.ScrollToEnd();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "Campah.exe";
            info.Arguments = "updated " + Process.GetCurrentProcess().Id; //+arg2;
            Process.Start(info);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    
    public class UpdateData : INotifyPropertyChanged
    {
        private String name;
        private String comment;
        public String File { get; set; }
        public String RelPath { get; set; }
        public String URL { get; set; }
        public String Hash { get; set; }
        public String Version { get; set; }
        private String status;
        private bool completed;

        public static UpdateData Instance { get; private set; }

        static UpdateData()
        {
            Instance = new UpdateData();
        }

        public UpdateData()
        {
            name = "";
            comment = "";
            File = "";
            RelPath = UpdateConstants.UPDATE_FOLDER;
            status = "Beginning Update";
            completed = false;
            URL = UpdateConstants.UPDATE_URL;
        }



        public String Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                NotifyPropertyChanged("Name");
            }
        }

        public bool Completed
        {
            get
            {
                return completed;
            }
            set
            {
                completed = value;
                NotifyPropertyChanged("Completed");
            }
        }

        public String Status
        {
            get
            {
                return status;
            }
            set
            {
                status = value;
                NotifyPropertyChanged("Status");
            }
        }

        public String Comment
        {
            get
            {
                return comment;
            }
            set
            {
                comment = value;
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

    public sealed class DTHasher
    {
        private DTHasher() { }

        private static byte[] ConvertStringToByteArray(string data)
        {
            return (new System.Text.UnicodeEncoding()).GetBytes(data);
        }

        private static System.IO.FileStream GetFileStream(string pathName)
        {
            return (new System.IO.FileStream(pathName, System.IO.FileMode.Open,
                      System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite));
        }

        public static string GetSHA1Hash(string pathName)
        {
            string strResult = "";
            string strHashData = "";

            byte[] arrbytHashValue;
            System.IO.FileStream oFileStream = null;

            System.Security.Cryptography.SHA1CryptoServiceProvider oSHA1Hasher =
                       new System.Security.Cryptography.SHA1CryptoServiceProvider();

            try
            {
                oFileStream = GetFileStream(pathName);
                arrbytHashValue = oSHA1Hasher.ComputeHash(oFileStream);
                oFileStream.Close();

                strHashData = System.BitConverter.ToString(arrbytHashValue);
                strHashData = strHashData.Replace("-", "");
                strResult = strHashData;
            }
            catch (System.Exception)
            {
            }

            return (strResult);
        }

        public static string GetMD5Hash(string pathName)
        {
            string strResult = "";
            string strHashData = "";

            byte[] arrbytHashValue;
            System.IO.FileStream oFileStream = null;

            System.Security.Cryptography.MD5CryptoServiceProvider oMD5Hasher =
                       new System.Security.Cryptography.MD5CryptoServiceProvider();

            try
            {
                oFileStream = GetFileStream(pathName);
                arrbytHashValue = oMD5Hasher.ComputeHash(oFileStream);
                oFileStream.Close();

                strHashData = System.BitConverter.ToString(arrbytHashValue);
                strHashData = strHashData.Replace("-", "");
                strResult = strHashData;
                oMD5Hasher.Clear();
            }
            catch (System.Exception)
            {
                oMD5Hasher.Clear();
            }
            return (strResult);
        }
    }

}
