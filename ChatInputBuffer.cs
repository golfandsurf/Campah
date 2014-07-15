using System.Collections.Generic;

namespace CampahApp
{
    public static class ChatInputBuffer
    {
        private static int _cursor;

        private static List<string> Lines = new List<string>();

        static ChatInputBuffer()
        {
            Lines.Add("");
        }

        public static void AddLine(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            Lines.Insert(1, line);

            if (Lines.Count == 25)
            {
                Lines.RemoveRange(20, 5);
            }

            _cursor = 0;
        }

        public static string Up(string line)
        {            
            Lines[_cursor % Lines.Count] = line;
            _cursor++;
            return Lines.Count > 0 ? Lines[_cursor%Lines.Count] : "";
        }
        
        public static string Down()
        {
            if (_cursor%Lines.Count == 0)
            {
                _cursor = 0;
            }
            else
            {
                _cursor--;
            }
            return Lines.Count > 0 ? Lines[_cursor%Lines.Count] : "";
        }
    }
}