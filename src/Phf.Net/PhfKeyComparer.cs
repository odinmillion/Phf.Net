using System.Collections.Generic;

namespace Phf.Net
{
    public class PhfKeyComparer : IComparer<PhfKey>
    {
        private readonly uint[] _bZ;

        public PhfKeyComparer(uint[] bZ)
        {
            _bZ = bZ;
        }

        public int Compare(PhfKey a, PhfKey b)
        {
            if (_bZ[a.G] > _bZ[b.G])
                return -1;
            if (_bZ[a.G] < _bZ[b.G])
                return 1;
            if (a.G > b.G)
                return -1;
            if (a.G < b.G)
                return 1;

            return 0;
        }
    }
}