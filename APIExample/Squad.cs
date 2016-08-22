using Rage;
using LSPD_First_Response.Mod.API;

namespace FederalCallouts
{
    public class Squad
    {
        Vehicle veh;
        Ped m1;
        Ped m2;
        Ped m3;
        Ped m4;
        Ped[] squad;
        /// <summary>
        /// Spawns a new squad in the specified car (must have 4 spaces)
        /// </summary>
        /// <param name="vehicleModel">Model of the vehicle</param>
        /// <param name="squadModel">Model of the Peds</param>
        /// <param name="spawn">Spawn location</param>
        public Squad(string vehicleModel, string squadModel, Vector3 spawn)
        {
            //TODO: Use rage "group" class
            veh = new Vehicle(vehicleModel, spawn);
            m1 = new Ped(squadModel, spawn, 0f);
            m2 = new Ped(squadModel, spawn, 0f);
            m3 = new Ped(squadModel, spawn, 0f);
            m4 = new Ped(squadModel, spawn, 0f);
            m1.WarpIntoVehicle(veh, -1);
            m2.WarpIntoVehicle(veh, 0);
            m3.WarpIntoVehicle(veh, 1);
            m4.WarpIntoVehicle(veh, 2);
            squad = new Ped[] { m1, m2, m3, m4 };
            foreach(Ped m in squad)
            {
                m.Armor = 100;
                m.MakePersistent();
                Functions.SetPedAsCop(m);
            }
            veh.AttachBlip().Sprite = BlipSprite.Police2to16;
        }
        /// <summary>
        /// Drives the squad to location (not guaranteed)
        /// </summary>
        /// <param name="loc">Location to move to</param>
        /// <param name="heading">Optional heading</param>
        /// <param name="onScene">Becomes true when the squad gets out of the car</param>
        public void DriveTo(Vector3 loc, float heading = 0f)
        {
            GameFiber.StartNew((() =>
            {
                bool readyToRoll = false;
                bool readyToDeploy = false;
                uint nextCommandTime = Game.GameTime;
                while (!readyToRoll)
                {
                    if (m1.IsInAnyVehicle(false) | !m1.IsAlive
                    & m2.IsInAnyVehicle(false) | !m2.IsAlive
                    & m3.IsInAnyVehicle(false) | !m3.IsAlive
                    & m4.IsInAnyVehicle(false) | !m4.IsAlive)
                    {
                        readyToRoll = true;
                        continue;
                    }
                    if (Game.GameTime >= nextCommandTime)
                    {
                        int index = -1;
                        foreach (Ped m in squad)
                        {
                            if (m.IsAlive & !m.IsInAnyVehicle(true))
                            {
                                m.Tasks.EnterVehicle(veh, index);
                                index++;
                            }
                        }
                        nextCommandTime += 15 * 1000;
                    }
                }
                while (!readyToDeploy)
                {
                    if (!veh.Exists())
                    {
                        Disenfranchise();
                        return;
                    }
                    if (Game.GameTime >= nextCommandTime)
                    {
                        if (veh.HasDriver)
                        {
                            veh.Driver.Tasks.DriveToPosition(loc,
                                30f,
                                VehicleDrivingFlags.DriveAroundVehicles |
                                VehicleDrivingFlags.DriveAroundPeds |
                                VehicleDrivingFlags.DriveAroundObjects |
                                VehicleDrivingFlags.Emergency |
                                VehicleDrivingFlags.StopAtDestination,
                                13f);
                        }
                    }
                    if (veh.DistanceTo(loc) <= 13f)
                        readyToDeploy = true;
                }
                foreach (Ped m in squad)
                    if (m.Exists() & m.IsAlive)
                        m.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen);
            }));
        }
        public void Disenfranchise()
        {
            Game.LogTrivialDebug("[FC] Disenfranchised a squad");
            if (veh.Exists())
                veh.CleanUp();
            foreach(Ped m in squad)
            {
                m.CleanUp();
            }
        }
    }
}
