using System;
using System.Collections.Generic;

namespace FederalCallouts.Tools
{
    [Serializable]
    public class ExtendedSpawnPoint
    {
        public Rage.Vector3 Position;
        public float Heading;

        public string Area;
        public string Street;
        public List<string> Tags;

        public ExtendedSpawnPoint()
        { }
    }
}