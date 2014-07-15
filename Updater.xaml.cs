using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Threading;
using System.Xml;
using System.Net;
using System.IO;
using SharpCompress.Archive;

namespace CampahApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Updater : Window
    {
        DispatcherTimer worker;
        StreamReader _info;
        public Updater()
        {
            InitializeComponent();
        }

        public delegate void ChangeTextBoxDelegate(String NewText);

        public string Arg2
        {
            get { return _arg2; }
            set { _arg2 = value; }
        }

        public void ChangeTextBox(String NewText)
        {
            UpdateData.Instance.Status += "\r\n" + NewText;
            TbUpdate.InvalidateVisual();
        }

        public void addText(string text)
        {
            Dispatcher.BeginInvoke(new ChangeTextBoxDelegate(ChangeTextBox), new object[] { text });
        }

        public Updater(StreamReader cu)
        {
            InitializeComponent();
            _info = cu;
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
            var TempDir = new DirectoryInfo(UpdateData.Instance.RelPath);
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
            Arg2 = TempDir.FullName;
            UpdateData.Instance.Completed = true;
            RestartMainApp();
            
        }

        private String _arg2 = "";

        public bool CheckUpdate()
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)System.Net.WebRequest.Create(UpdateData.Instance.Url + "update.txt");
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
                var Client = new WebClient();
                if (!TempDir.Exists)
                {
                    TempDir.Create();
                }
                addText(UpdateData.Instance.Url + UpdateData.Instance.File);
                Client.DownloadFile(UpdateData.Instance.Url + UpdateData.Instance.File, string.Format("{0}\\Package", TempDir.FullName));
                return DTHasher.GetMD5Hash(string.Format("{0}\\Package", TempDir.FullName)) == UpdateData.Instance.Hash;
            }
            catch
            {
                return false;
            }
        }


        private void DoFileUpdate(string tempPath, string package)
        {
            var filesToReplace = new List<string>();
            IArchive archive = ArchiveFactory.Open(package, SharpCompress.Common.Options.KeepStreamsOpen);
            bool HaveUpdateFile = false;
            foreach (var entry in archive.Entries)
            {
                if (!entry.IsDirectory)
                {
                    if (entry.FilePath.ToLower().Contains("update.xml"))
                    {
                        entry.WriteToDirectory(tempPath);
                        filesToReplace = GetFilesToReplace(new FileInfo(string.Format("{0}\\Update.xml", tempPath)).OpenRead());
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
                        var oldFile = new FileInfo(entry.FilePath);

                        // if in update.xml or doesn't exist and isn't the update.xml
                        if (filesToReplace.Contains(entry.FilePath) || !(oldFile.Exists) && !(entry.FilePath.ToLower().Contains("update.xml")))
                        {
                            addText(string.Format("Updating file [{0}]", entry.FilePath));


                            if (oldFile.Exists && !File.Exists(oldFile.FullName + ".old"))
                            {
                                oldFile.MoveTo(string.Format("{0}.old", oldFile.Name));
                                entry.WriteToDirectory(".\\", SharpCompress.Common.ExtractOptions.Overwrite | SharpCompress.Common.ExtractOptions.ExtractFullPath);
                            }
                            
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
            var rootDirectory = new DirectoryInfo(Environment.CurrentDirectory);

            foreach (var oldFile in rootDirectory.GetFiles("*.old"))
            {
                oldFile.Delete();
            }
            
            foreach (var directory in rootDirectory.GetDirectories())
            {
                foreach (var oldFile in directory.GetFiles("*.old"))
                {
                    oldFile.Delete();
                }

                // Delete our temp directory
                if (directory.FullName == UpdateFileDir)
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

        private void RestartMainApp()
        {
            if ((bool) CbClosewhencomplete.IsChecked)
            {
                Close();
            }
        }

        void MainContainer_Loaded(object sender, RoutedEventArgs e)
        {
                worker.Start();
        }

        private List<string> GetFilesToReplace(Stream AutoUpdateFile)
        {
            var xDoc = new XmlDocument();
            xDoc.Load(AutoUpdateFile);
            XmlNodeList Files = xDoc.GetElementsByTagName("File");
            var result = (from XmlNode File in Files select File.Attributes["Filename"].Value).ToList();
            AutoUpdateFile.Close();
            return result;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            if (!tb.IsMouseOver)
            {
                tb.ScrollToEnd();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            var info = new ProcessStartInfo();
            info.FileName = "Campah.exe";
            info.Arguments = "updated " + Process.GetCurrentProcess().Id; //+arg2;
            Process.Start(info);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
