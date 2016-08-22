using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using FederalCallouts.UI;

namespace FederalCallouts.Callouts
{
#if DEBUG
    [CalloutInfo("OrganizedRetailCrime", CalloutProbability.Always)]
#else 
    [CalloutInfo("OrganizedRetailCrime", CalloutProbability.VeryHigh)]
#endif
    class OrganizedRetailCrime : Callout
    {

    }
}
