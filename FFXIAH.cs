using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;

namespace CampahApp
{
    public static class FFXIAH
    {
        public static int LookupMedian(int itemID, int serverID, bool stack,int timeout)
        {
            FFXIAHItem item = FFXIAHItem.GetItemByID(itemID, stack);
            if (item.Price > -1)
                return item.Price;
            String url = "http://www.ffxiah.com/item/" + itemID + "/?sid=" + (serverID) + "&stack=" + isStack(stack);
            CookieContainer cookieContainer = new CookieContainer();
            Cookie serverCookie = new Cookie();
            Cookie serverCookie2 = new Cookie();

            serverCookie.Name = "sid";
            serverCookie2.Name = "sid";
            serverCookie.Domain = "ffxiah.com";
            serverCookie2.Domain = "www.ffxiah.com";
            serverCookie.Value = serverID.ToString();
            serverCookie2.Value = serverID.ToString();
            cookieContainer.Add(serverCookie);
            cookieContainer.Add(serverCookie2);
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(url);
            webReq.CookieContainer = cookieContainer;
            webReq.Timeout = timeout;
            webReq.ReadWriteTimeout = timeout;



            byte[] buf = new byte[8192];
            // execute the request
            WebResponse response;
            try
            {
                response = webReq.GetResponse();
            }
            catch
            {
                CampahStatus.SetStatus("Price request timed out, try again");
                return -1;
            }
            // we will read data via the response stream
            Stream resStream = response.GetResponseStream();

            string tempString = null;
            int count = 0;
            String match = @".*<td>Median</td>
        <td><span class=number-format>([0-9]*)</span></td>.*";
            //String match = @".*Average</td><td><span class=number-format>(.*)</span>.*";
            do
            {
                // fill the buffer with data
                count = resStream.Read(buf, 0, buf.Length);
                // make sure we read some data
                if (count != 0)
                {
                    // translate from ASCII bytes to Unicode text
                    tempString = Encoding.ASCII.GetString(buf, 0, count);

                    // continue building the string
                    Regex r = new Regex(match);
                    Match found = r.Match(tempString);
                    int median;
                    if (int.TryParse(found.Groups[1].ToString(), out median))
                    {
                        item.Price = median;
                        return median;
                    }
                }
            }
            while (count > 0); // any more data to read?
            CampahStatus.SetStatus("Price request returned 0");
            return -1;
        }

        private static int isStack(bool stack)
        {
            if (stack)
                return 1;
            return 0;
        }
    }

    internal class FFXIAHItem
    {
        private int price;
        private DateTime priceset;
        public int ID { get; private set; }
        public bool Stack { get; private set; }

        private static List<FFXIAHItem> ItemList;

        static FFXIAHItem()
        {
            ItemList = new List<FFXIAHItem>();
        }

        private static FFXIAHItem AddItem(int item, bool stack)
        {
            FFXIAHItem newitem = new FFXIAHItem(item, stack);
            ItemList.Add(newitem);
            return newitem;
        }

        public static FFXIAHItem GetItemByID(int item, bool stack)
        {
            foreach (FFXIAHItem Item in ItemList)
            {
                if (Item.ID == item && Item.Stack == stack)
                    return Item;
            }
            return AddItem(item, stack);
        }

        private FFXIAHItem(int item, bool stack)
        {
            ID = item;
            Stack = stack;
            price = -1;
            priceset = DateTime.Now;
        }

        public int Price
        {
            get
            {
                if (price > -1 && (DateTime.Now - priceset) > TimeSpan.FromHours(1))
                    price = -1;
                return price;
            }
            set
            {
                price = value;
                priceset = DateTime.Now;
            }
        }
    }
}
