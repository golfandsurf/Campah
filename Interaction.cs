using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using FFACETools;
using System.Text.RegularExpressions;

namespace CampahApp
{
    class Interaction
    {
        Stack<int> CurrentAddress = new Stack<int>();

        private int[] ReadAHItems()
        {
            int loaded = -1;
            CampahStatus.SetStatus("Waiting for items to load...");
            Thread.Sleep((int)CampahStatus.Instance.GlobalDelay * 4);
            while (loaded != AuctionHouse.LoadedCount)
            {
                loaded = AuctionHouse.LoadedCount;
                Thread.Sleep((int)CampahStatus.Instance.GlobalDelay * 2);
            }
            CampahStatus.SetStatus("Reading item list...");
            return AuctionHouse.ReadIDArray();
        }

        //Will Not Work until MenuLength is restored and a resource parsing method restored.
        public bool TraverseMenu(String address)
        {
            GotoMenu(address);
            Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
            if (FFACE_INSTANCE.Instance.Menu.Selection == "Bid")
                return true;
            int max = AuctionHouse.MenuLength;

            for (int i = 1; i <= max; i++)
            {
                if (TraverseMenu(address + "," + i))
                {
                    int[] ids = ReadAHItems();
                    foreach (int id in ids)
                    {
                        //if (string.IsNullOrEmpty(FFACE.ParseResources.GetItemName(id)))
                        //MessageBox.Show(id + " : " + FFACE.ParseResources.GetItemName(id));
                        AHItem item = new AHItem(id, /*FFACE.ParseResources.GetItemName(id)*/id.ToString(), false, address + "," + i);
                        if ((item = AuctionHouse.Add(item)) != null)
                        {
                            item.Stackable = true;
                        }
                    }
                    AuctionHouse.MenuIndex = 1;
                }
            }
            return false;
        }

        private bool GotoMenu(string address)
        {
            if (address == "" || CurrentAddress.Count < 1)
            {
                GotoBidMenu();
                if (address == "")
                    return false;
            }
            string[] adrStr = address.Split(',');
            int[] adr = new int[adrStr.Length];
            for (int i = 0; i < adrStr.Length; i++)
            {
                adr[i] = int.Parse(adrStr[i]);
            }
            while (CurrentAddress.Count > adr.Length)
            {
                CurrentAddress.Pop();
                FFACE_INSTANCE.Instance.Windower.SendKeyPress(KeyCode.EscapeKey);
                Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
            }
            while (CurrentAddress.Count > 0 && !isMenuEqual(CurrentAddress.ToArray(), adr))
            {
                CurrentAddress.Pop();
                FFACE_INSTANCE.Instance.Windower.SendKeyPress(KeyCode.EscapeKey);
                Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
            }
            for (int i = CurrentAddress.Count; i < adr.Length; i++)
            {
                String helptxt = FFACE_INSTANCE.Instance.Menu.Selection;
                if (helptxt != "Bid")// && AuctionHouse.MenuLength >= adr[i])
                {
                    CurrentAddress.Push(adr[i]);
                    AuctionHouse.MenuIndex = adr[i];
                    Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
                    FFACE_INSTANCE.Instance.Windower.SendKeyPress(KeyCode.NP_EnterKey);
                    Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
                }
                else
                    return false;
            }
            return true;
        }

        private bool isMenuEqual(int[] a, int[] b)
        {
            for (int i = 0; i < a.Length; i++)
                if (a[(a.Length - 1) - i] != b[i])
                    return false;
            return true;
        }

        private bool istargetvalid(string currenttarget)
        {
            foreach (AHTarget target in RunningData.Instance.AHTargetList)
                if (currenttarget == target.TargetName)
                    return true;
            return false;
        }

        private void GotoBidMenu()
        {
            CloseMenu();
            while (!istargetvalid(FFACE_INSTANCE.Instance.Target.Name) || FFACE_INSTANCE.Instance.NPC.Distance((short)FFACE_INSTANCE.Instance.Target.ID) >= 6.0)
            {
                FFACE_INSTANCE.Instance.Windower.SendKeyPress(KeyCode.TabKey);
                Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
            }
            FFACE_INSTANCE.Instance.Windower.SendKeyPress(KeyCode.NP_EnterKey);
            Thread.Sleep((int)CampahStatus.Instance.GlobalDelay * 4);
            AuctionHouse.MenuIndex = 1;
            Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
            FFACE_INSTANCE.Instance.Windower.SendKeyPress(KeyCode.NP_EnterKey);
            Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
            CurrentAddress.Clear();
            CurrentAddress.Push(1);
        }

        public void CloseMenu()
        {
            while (FFACE_INSTANCE.Instance.Menu.IsOpen)
            {
                FFACE_INSTANCE.Instance.Windower.SendKeyPress(KeyCode.EscapeKey);
                Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
            }
            CurrentAddress.Clear();
        }

        private int BidOnItem(ItemRequest item)
        {
            if (FFACE_INSTANCE.Instance.Item.InventoryMax == FFACE_INSTANCE.Instance.Item.InventoryCount)
                StopBuying("Inventory Full");
            if (item.BoughtCount >= item.Quantity)
                return 0;
            {
                string strstack = ".";
                if (item.Stack)
                    strstack = " stack.";
                CampahStatus.Instance.Status = "Finding item: " + item.ItemData.Name + strstack;
            }
            if (!GotoMenu(item.ItemData.Address))
            {
                GotoBidMenu();
                return 0;
            }
            Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
            int[] ids = ReadAHItems();
            if (ids.Length < 3 || ids[0] == ids[2])
            {
                //StopBuying("Error! AH item array could not be read. Try zoning or logging out");
                CampahStatus.SetStatus("Error! AH item array could not be read. Try zoning or logging out\r\n\t\tSkipping to the next item.");
                return 0;
            }
            int index = Array.IndexOf(ids, item.ItemData.ID) + 1;
            int stack = 0;
            if (item.Stack && item.ItemData.Stackable)
                stack = 1;
            AuctionHouse.MenuIndex = index + stack;
            Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
            int bid;
            {
                Regex parselowball = new Regex("[^0-9]*([0-9]+)(%)?.*");
                Match matches = parselowball.Match(CampahStatus.Instance.LowballBid);
                int lowballamount;
                if (matches.Groups.Count > 1 && !string.IsNullOrEmpty(matches.Groups[1].Value) && int.TryParse(matches.Groups[1].Value, out lowballamount))
                {
                    //int lowballamount = int.Parse(matches.Groups[1].Value);
                    if (matches.Groups[2].Value == "%" && lowballamount < 100)
                        bid = item.Minimum * lowballamount / 100;
                    else
                        bid = lowballamount;
                    if (bid > item.Minimum)
                        bid = item.Minimum;
                }
                else
                    bid = item.Minimum;
            }
            if (bid < 1)  //safety check on bid
                bid = 1;
            bool firstbid = true;
            bool hasitems = false;
            Chatlog.Instance.ClearChatAlerts();

            while (bid <= item.Maximum && item.BoughtCount < item.Quantity)
            {
                if (FFACE_INSTANCE.Instance.Item.InventoryMax == FFACE_INSTANCE.Instance.Item.InventoryCount)
                    StopBuying("Inventory Full");
                if (AuctionHouse.MenuIndex != index + stack)
                {
                    CampahStatus.Instance.Status = "Error: Mismatch IDs, Skipping...";
                    break;
                }

                FFACE_INSTANCE.Instance.Windower.SendKeyPress(KeyCode.EnterKey);
                Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
                AuctionHouse.MenuIndex = 2;
                Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
                FFACE_INSTANCE.Instance.Windower.SendKeyPress(KeyCode.EnterKey);
                Thread.Sleep((int)CampahStatus.Instance.GlobalDelay * 2);
                if (!hasitems)
                {
                    if (FFACE_INSTANCE.Instance.Menu.Selection != "Price Set")
                    {
                        CampahStatus.Instance.Status = item.ItemData.Name + " is unavailble on AH, Skipping...";
                        FFACE_INSTANCE.Instance.Windower.SendKeyPress(KeyCode.EscapeKey);
                        Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
                        break;
                    }
                    else
                        hasitems = true;
                }
                while (FFACE_INSTANCE.Instance.Menu.Selection != "Price Set")
                    Thread.Sleep(250);
                /*                if (FFACE_INSTANCE.Instance.Item.SelectedItemName.ToLower() != item.ItemData.Name.ToLower() ||
                                    AuctionHouse.MenuIndex != index + stack)
                                {
                                    CampahStatus.Instance.Status = "Error: Mismatch IDs, Skipping...";
                                    break;
                                }
                  */
                AuctionHouse.BidValue = bid;
                {
                    string strstack = ".";
                    if (item.Stack)
                        strstack = " stack.";
                    CampahStatus.Instance.Status = "Bidding " + bid + "g on " + item.ItemData.Name + strstack;
                }
                Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
                FFACE_INSTANCE.Instance.Windower.SendKeyPress(KeyCode.EnterKey);
                Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
                AuctionHouse.MenuIndex = 1;
                Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
                ChatAlert alert = new ChatAlert(new Regex(@".*You(.*)buy the .* for ([0-9,]*) gil\."), ChatMode.SynthResult);
                Chatlog.Instance.addAlert(alert);
                FFACE_INSTANCE.Instance.Windower.SendKeyPress(KeyCode.EnterKey);
                Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
                uint prev_inv_amount = FFACE_INSTANCE.Instance.Item.GetInventoryItemCount((ushort)item.ItemData.ID);
                {
                    bool override_alert = false;
                    int time = 0;
                    while (!override_alert && !alert.Completed)
                    {
                        Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
                        time += (int)CampahStatus.Instance.GlobalDelay;
                        if (time >= 20000)
                            override_alert = true;
                    }
                    if (override_alert)
                    {
                        CampahStatus.SetStatus("An error occurred while parsing bid results\r\n\t\tRemoving item from bid list");
                        item.BoughtCount = item.Quantity;
                        break;
                    }
                }
                //                Chatlog.Instance.ClearChatAlerts();
                int curval = (int)FFACE_INSTANCE.Instance.Item.GetInventoryItemCount((ushort)item.ItemData.ID);
                //                if (alert.Result == null)
                //                    break;
                if (alert.Result.Groups[1].Value.Contains("unable"))
                {
                    if (bid < item.Minimum)
                        bid = item.Minimum;
                    else
                        bid += item.Increment;
                    if (bid > item.Maximum && bid < (item.Maximum + item.Increment))
                        bid = item.Maximum;
                    firstbid = false;
                    if (bid <= item.Maximum)
                        CampahStatus.Instance.Status = "Bid rejected, increasing bid to " + bid + "g.";
                    else
                        CampahStatus.Instance.Status = "Bid rejected, skipping to the next item...";
                }
                //                else if(FFACE_INSTANCE.Instance.Item.GetInventoryItemCount((ushort)item.ItemData.ID) == prev_inv_amount)
                //                {
                //                    CampahStatus.SetStatus("Error! Could not detect buy result.");
                //                    break;
                //                }
                else
                {
                    string strstack = "";
                    if (item.Stack)
                        strstack = " stack";
                    CampahStatus.Instance.Status = "You bought the " + item.ItemData.Name + strstack + " for " + bid + "g.";
                    item.BoughtCount++;
                    item.BoughtCost += bid;
                    RunningData.Instance.TotalSpent += bid;
                    if (item.Minimum >= bid && firstbid && CampahStatus.Instance.CheapO)
                    {
                        bid -= item.Increment;
                        if (bid < 1)
                            bid = 1;
                    }
                }
                Chatlog.Instance.ClearChatAlerts();
            }
            return item.BoughtCount;
        }//

        public void StartBuying()
        {
            CampahStatus.Instance.Status = "Beginning Buy Procedure";
            CampahStatus.Instance.Mode = Modes.Buying;
            RunningData.Instance.calculateProjectedCost();
            RunningData.Instance.TotalSpent = 0;
            if (CampahStatus.Instance.BlockCommands)
            {
                FFACE_INSTANCE.Instance.Windower.SendString("//mouse_blockinput;keyboard_blockinput;");
            }
            ThreadManager.threadRunner(BuyProceedure);
            /*
            BuyThread = new Thread(BuyProceedure);
            BuyThread.IsBackground = true;
            BuyThread.Start();\
             */
        }

        public void StopBuying()
        {
            StopBuying("Stopped");
        }

        public void StopBuying(string status)
        {
            StopBuying(status, BuyProceedure);
        }

        public void StopBuying(string status, ThreadStart thread)
        {
            CampahStatus.Instance.Mode = Modes.Stopped;
            CampahStatus.Instance.Status = status;
            CloseMenu();
            Chatlog.Instance.ClearChatAlerts();
            if (CampahStatus.Instance.BlockCommands)
            {
                FFACE_INSTANCE.Instance.Windower.SendString("//mouse_blockinput off;keyboard_blockinput off;");
            }
            ThreadManager.stopThread(thread);
            //if (thread.IsAlive)
            //    thread.Abort();
        }

        private void StartWaitCycle(TimeSpan time)
        {
            if (CampahStatus.Instance.AllowCycleRandom)
            {
                int seconds = (int)time.TotalSeconds;
                Random rand = new Random();
                seconds += rand.Next((int)(seconds * .1));
                time = TimeSpan.FromSeconds(seconds);
            }
            while (time > TimeSpan.Zero)
            {
                if (CampahStatus.Instance.Mode == Modes.Stopped)
                    break;
                Thread.Sleep(1000);
                time -= TimeSpan.FromSeconds(1);
                CampahStatus.Instance.Status = "Beginning next cycle in " + time.Minutes.ToString("#") + ":" + time.Seconds.ToString("00") + "...";
            }
        }

        private void BuyProceedure()
        {
            while (RunningData.Instance.BidList.Count > 0 && CampahStatus.Instance.Mode == Modes.Buying)
            {
                List<ItemRequest> trashcan = new List<ItemRequest>();
                foreach (ItemRequest item in RunningData.Instance.BidList)
                {
                    BidOnItem(item);
                    if (item.Quantity <= item.BoughtCount)
                    {
                        trashcan.Add(item);
                    }
                }
                foreach (ItemRequest trash in trashcan)
                {
                    RunningData.Instance.TrashCan.Enqueue(trash);
                }
                trashcan.Clear();
                CloseMenu();
                if (CampahStatus.Instance.BuyCycleWait > 0 && RunningData.Instance.BidList.Count > 0)
                {

                    StartWaitCycle(TimeSpan.FromMinutes(CampahStatus.Instance.BuyCycleWait));
                }
            }
            StopBuying();
        }
    }
}
