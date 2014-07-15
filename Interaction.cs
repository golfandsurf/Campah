using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using FFACETools;
using System.Text.RegularExpressions;

namespace CampahApp
{
    class Interaction
    {
        Stack<int> _currentAddress = new Stack<int>();

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
            if (FFACEInstance.Instance.Menu.Selection == "Bid")
                return true;
            int max = AuctionHouse.MenuLength;

            for (int i = 1; i <= max; i++)
            {
                if (TraverseMenu(address + "," + i))
                {
                    int[] ids = ReadAHItems();
                    foreach (int id in ids)
                    {
                        
                        var item = new AhItem(id, id.ToString(CultureInfo.InvariantCulture), false, address + "," + i);
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
            if (address == "" || _currentAddress.Count < 1)
            {
                GotoBidMenu();
                if (address == "")
                    return false;
            }
            var adrStr = address.Split(',');
            var adr = new int[adrStr.Length];
            for (int i = 0; i < adrStr.Length; i++)
            {
                adr[i] = int.Parse(adrStr[i]);
            }
            while (_currentAddress.Count > adr.Length)
            {
                _currentAddress.Pop();
                FFACEInstance.Instance.Windower.SendKeyPress(KeyCode.EscapeKey);
                Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
            }
            while (_currentAddress.Count > 0 && !IsMenuEqual(_currentAddress.ToArray(), adr))
            {
                _currentAddress.Pop();
                FFACEInstance.Instance.Windower.SendKeyPress(KeyCode.EscapeKey);
                Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
            }
            for (int i = _currentAddress.Count; i < adr.Length; i++)
            {
                var helptxt = FFACEInstance.Instance.Menu.Selection;
                if (helptxt != "Bid")
                {
                    _currentAddress.Push(adr[i]);
                    AuctionHouse.MenuIndex = adr[i];
                    Thread.Sleep((int) CampahStatus.Instance.GlobalDelay);
                    FFACEInstance.Instance.Windower.SendKeyPress(KeyCode.NP_EnterKey);
                    Thread.Sleep((int) CampahStatus.Instance.GlobalDelay);
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsMenuEqual(int[] a, int[] b)
        {
            return !a.Where((t, i) => a[(a.Length - 1) - i] != b[i]).Any();
        }

        private bool IsTargetValid(string currenttarget)
        {
            return RunningData.Instance.AhTargetList.Any(target => currenttarget == target.TargetName);
        }

        private void GotoBidMenu()
        {
            CloseMenu();
            while (!IsTargetValid(FFACEInstance.Instance.Target.Name) || FFACEInstance.Instance.NPC.Distance((short)FFACEInstance.Instance.Target.ID) >= 6.0)
            {
                FFACEInstance.Instance.Windower.SendKeyPress(KeyCode.TabKey);
                Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
            }
            FFACEInstance.Instance.Windower.SendKeyPress(KeyCode.NP_EnterKey);
            Thread.Sleep((int)CampahStatus.Instance.GlobalDelay * 4);
            AuctionHouse.MenuIndex = 1;
            Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
            FFACEInstance.Instance.Windower.SendKeyPress(KeyCode.NP_EnterKey);
            Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
            _currentAddress.Clear();
            _currentAddress.Push(1);
        }

        public void CloseMenu()
        {
            while (FFACEInstance.Instance.Menu.IsOpen)
            {
                FFACEInstance.Instance.Windower.SendKeyPress(KeyCode.EscapeKey);
                Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
            }
            _currentAddress.Clear();
        }

        private void BidOnItem(ItemRequest item)
        {
            if (FFACEInstance.Instance.Item.InventoryMax == FFACEInstance.Instance.Item.InventoryCount)
                StopBuying("Inventory Full");
            if (item.BoughtCount >= item.Quantity)
                return;
            {
                string strstack = ".";
                if (item.Stack)
                    strstack = " stack.";
                CampahStatus.Instance.Status = "Finding item: " + item.ItemData.Name + strstack;
            }
            if (!GotoMenu(item.ItemData.Address))
            {
                GotoBidMenu();
                return;
            }
            Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
            int[] ids = ReadAHItems();
            if (ids.Length < 3 || ids[0] == ids[2])
            {
                //StopBuying("Error! AH item array could not be read. Try zoning or logging out");
                CampahStatus.SetStatus("Error! AH item array could not be read. Try zoning or logging out\r\n\t\tSkipping to the next item.");
                return;
            }
            int index = Array.IndexOf(ids, item.ItemData.ID) + 1;
            int stack = 0;
            if (item.Stack && item.ItemData.Stackable)
            {
                stack = 1;
            }
            AuctionHouse.MenuIndex = index + stack;
            Thread.Sleep((int)CampahStatus.Instance.GlobalDelay);
            int bid;
            {
                var parselowball = new Regex("[^0-9]*([0-9]+)(%)?.*");
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
                if (FFACEInstance.Instance.Item.InventoryMax == FFACEInstance.Instance.Item.InventoryCount)
                    StopBuying("Inventory Full");
                if (AuctionHouse.MenuIndex != index + stack)
                {
                    CampahStatus.Instance.Status = "Error: Mismatch IDs, Skipping...";
                    break;
                }

                FFACEInstance.Instance.Windower.SendKeyPress(KeyCode.EnterKey);
                Thread.Sleep((int) CampahStatus.Instance.GlobalDelay);
                AuctionHouse.MenuIndex = 2;
                Thread.Sleep((int) CampahStatus.Instance.GlobalDelay);
                FFACEInstance.Instance.Windower.SendKeyPress(KeyCode.EnterKey);
                Thread.Sleep((int) CampahStatus.Instance.GlobalDelay*2);
                if (!hasitems)
                {
                    if (FFACEInstance.Instance.Menu.Selection != "Price Set")
                    {
                        CampahStatus.Instance.Status = item.ItemData.Name + " is unavailble on AH, Skipping...";
                        FFACEInstance.Instance.Windower.SendKeyPress(KeyCode.EscapeKey);
                        Thread.Sleep((int) CampahStatus.Instance.GlobalDelay);
                        break;
                    }

                    hasitems = true;
                }
                while (FFACEInstance.Instance.Menu.Selection != "Price Set")
                {
                    Thread.Sleep(250);
                }

                AuctionHouse.BidValue = bid;
CampahStatus.Instance.Status = string.Format("Bidding {0}g on {1}{2}", bid, item.ItemData.Name, item.Stack ? " stack." : ".");
                
                Thread.Sleep((int) CampahStatus.Instance.GlobalDelay);
                FFACEInstance.Instance.Windower.SendKeyPress(KeyCode.EnterKey);
                Thread.Sleep((int) CampahStatus.Instance.GlobalDelay);
                AuctionHouse.MenuIndex = 1;
                Thread.Sleep((int) CampahStatus.Instance.GlobalDelay);
                var alert = new ChatAlert(new Regex(@".*You(.*)buy the .* for ([0-9,]*) gil\."));
                Chatlog.Instance.AddAlert(alert);
                FFACEInstance.Instance.Windower.SendKeyPress(KeyCode.EnterKey);
                Thread.Sleep((int) CampahStatus.Instance.GlobalDelay);
                FFACEInstance.Instance.Item.GetInventoryItemCount((ushort) item.ItemData.ID);
                var overrideAlert = false;
                int time = 0;
                while (!overrideAlert && !alert.Completed)
                {
                    Thread.Sleep((int) CampahStatus.Instance.GlobalDelay);
                    time += (int) CampahStatus.Instance.GlobalDelay;
                    if (time >= 20000)
                        overrideAlert = true;
                }
                if (overrideAlert)
                {
                    CampahStatus.SetStatus("An error occurred while parsing bid results\r\n\t\tRemoving item from bid list");
                    item.BoughtCount = item.Quantity;
                    break;
                }
               
               
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
                    {
                        CampahStatus.Instance.Status = "Bid rejected, increasing bid to " + bid + "g.";
                    }
                    else
                    {
                        CampahStatus.Instance.Status = "Bid rejected, skipping to the next item...";
                    }
                }

                else
                {
                    var strstack = "";
                    if (item.Stack)
                    {
                        strstack = " stack";
                    }
                    CampahStatus.Instance.Status = string.Format("You bought the {0}{1} for {2}g.", item.ItemData.Name, strstack, bid);
                    item.BoughtCount++;
                    item.BoughtCost += bid;
                    RunningData.Instance.TotalSpent += bid;
                    if (item.Minimum >= bid && firstbid && CampahStatus.Instance.CheapO)
                    {
                        bid -= item.Increment;
                        if (bid < 1)
                        {
                            bid = 1;
                        }
                    }
                }
                Chatlog.Instance.ClearChatAlerts();
            }
        }

        public void StartBuying()
        {
            CampahStatus.Instance.Status = "Beginning Buy Procedure";
            CampahStatus.Instance.Mode = Modes.Buying;
            RunningData.Instance.CalculateProjectedCost();
            RunningData.Instance.TotalSpent = 0;
            if (CampahStatus.Instance.BlockCommands)
            {
                FFACEInstance.Instance.Windower.SendString("//mouse_blockinput;keyboard_blockinput;");
            }
            ThreadManager.ThreadRunner(BuyProceedure);
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
                FFACEInstance.Instance.Windower.SendString("//mouse_blockinput off;keyboard_blockinput off;");
            }
            ThreadManager.StopThread(thread);
        }

        private void StartWaitCycle(TimeSpan time)
        {
            if (CampahStatus.Instance.AllowCycleRandom)
            {
                var seconds = (int)time.TotalSeconds;
                var rand = new Random();
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
                var trashcan = new List<ItemRequest>();

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
