using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;

namespace FederalCallouts.Tools
{
    class PreviewVehicle
    {
        public PreviewVehicle(string model, Vector3 pos, int heading)
        {

        }
        public void test()
        {
            
            Vehicle v = new Vehicle("", new Vector3());
            v.IsDeformationEnabled = false;
            v.NeedsCollision = false;
            v.IsDriveable = false;
            v.LockStatus = VehicleLockStatus.Locked;
            v.IsInvincible = true;
            v.Opacity = 50f;

        }
    }
}
