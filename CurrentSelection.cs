namespace CampahApp
{
    public static class CurrentSelection
    {
        public static string Name { get; set; }

        public static string Price { get; set; }

        static CurrentSelection()
        {
            Name = "";
            Price = "0g";
        }
    }
}