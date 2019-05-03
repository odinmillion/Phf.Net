namespace Phf.Net
{
    public struct PhfKey
    {
        public string Key;
        
        public uint G; /* result of g(k) % r */
        
        //public unsafe uint* N; /* number of keys in bucket g */ =>
    }
}