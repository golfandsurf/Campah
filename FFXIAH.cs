using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace CampahApp
{
    public static class FFXIAH
    {
        public static int LookupMedian(int itemID, int serverID, bool stack,int timeout)
        {
            var item = FFXiAhItem.GetItemByID(itemID, stack);
            if (item.Price > -1)
            {
                return item.Price;
            }

            var url =  string.Format("http://www.ffxiah.com/item/{0}/?sid={1}&stack={2}", itemID, serverID, IsStack(stack));
            var cookieContainer = new CookieContainer();
            var serverCookie = new Cookie();
            var serverCookie2 = new Cookie();

            serverCookie.Name = "sid";
            serverCookie2.Name = "sid";
            serverCookie.Domain = "ffxiah.com";
            serverCookie2.Domain = "www.ffxiah.com";
            serverCookie.Value = serverID.ToString(CultureInfo.InvariantCulture);
            serverCookie2.Value = serverID.ToString(CultureInfo.InvariantCulture);
            cookieContainer.Add(serverCookie);
            cookieContainer.Add(serverCookie2);
            var webReq = (HttpWebRequest)WebRequest.Create(url);
            webReq.CookieContainer = cookieContainer;
            webReq.Timeout = timeout;
            webReq.ReadWriteTimeout = timeout;

            var buf = new byte[8192];
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

            string tempString;
            int count = 0;
            const string match = @".*<td>Median</td><td><span class=number-format>([0-9]*)</span></td>.*";
            
            do
            {
                // fill the buffer with data
                if (resStream != null)
                {
                    count = resStream.Read(buf, 0, buf.Length);
                }

                // make sure we read some data
                if (count != 0)
                {
                    // translate from ASCII bytes to Unicode text
                    tempString = Encoding.ASCII.GetString(buf, 0, count);

                    // continue building the string
                    var r = new Regex(match);
                    Match found = r.Match(tempString);
                    int median;
                    if (int.TryParse(found.Groups[1].ToString(), out median))
                    {
                        item.Price = median;
                        return median;
                    }
                }
            } while (count > 0); // any more data to read?

            CampahStatus.SetStatus("Price request returned 0");
            return -1;
        }

        private static int IsStack(bool stack)
        {
            return stack ? 1 : 0;
        }
    }
}
