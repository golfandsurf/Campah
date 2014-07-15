namespace CampahApp
{
    public class AhTarget
    {
        public AhTarget(string targetname)
        {
            TargetName = targetname;
        }

        public string TargetName { get; set; }
        
        public override bool Equals(object obj)
        {
            return ((AhTarget)obj).TargetName == TargetName;
        }

        protected bool Equals(AhTarget other)
        {
            return string.Equals(TargetName, other.TargetName);
        }

        public override int GetHashCode()
        {
            return (TargetName != null ? TargetName.GetHashCode() : 0);
        }
    }
}