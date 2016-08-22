using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;

namespace FederalCallouts.Callouts
{
#if DEBUG
    [CalloutInfo("GangShakedown", CalloutProbability.Always)]
#else
    [CalloutInfo("GangShakedown", CalloutProbability.Medium)]
#endif
    class GangShakedown : Callout
    {
    }
}
