namespace Phf.Net
{
    public class PhfSettings
    {
        public uint ItemsPerBucket { get; set; } = 4;
        
        public uint Alpha { get; set; } = 80;
        
        public uint Seed { get; set; } = 31337;
        
        public bool NoDivision { get; set; } = true;
    }
}