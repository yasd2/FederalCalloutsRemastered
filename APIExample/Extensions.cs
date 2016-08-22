using Rage;
using System.Threading;
using System;
using System.Collections.Generic;

namespace FederalCallouts
{
    public static class Extensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="v">Vector from which to start looking for safe spawns</param>
        /// <param name="maxDist">Maximum distance the spawn can be from v</param>
        /// <returns>Safe spawn location, 0,0,0 for no safe spawn, original vector v if spawn is already safe</returns>
        public static Vector3 FindSafePedSpawn(Vector3 v, float maxDist)
        {
            //Vector3 safeSpawn;
            //PATHFIND::GET_SAFE_COORD_FOR_PED(float x, float y, float z, BOOL onGround, Vector3 *outPosition, int flags)
            //Rage does not appear to currently support Vector3 arguments for native functions
            /*
            Rage.Native.NativeFunction.CallByName<bool>("GET_SAFE_COORD_FOR_PED",
                v.X,
                v.Y,
                v.Z,
                true,
                safeSpawn,
                0);
             * */
            return v;
        }
        public static bool IsPlayersVehicle(this Vehicle e)
        {
            if (e == Game.LocalPlayer.Character.LastVehicle | e == Game.LocalPlayer.Character.CurrentVehicle)
                return true;
            else
                return false;
        }
        /// <summary>
        /// Find a close-by-travel-distance vehicle spawn (not guaranteed)
        /// Recommended to run in a different game fiber
        /// </summary>
        /// <param name="origin">The center of where you want the spawn to be. Should be close to destination.</param>
        /// <param name="radius">Max radius of spawn zone</param>
        /// <param name="maxDist">Maximum driving distance acceptable</param>
        /// <returns></returns>
        public static Vector3 FindCloseSpawn(Vector3 origin, float radius, float maxDist)
        {
            bool running = true;
            int timesRun = 0;
            while (running & timesRun < 125)
            {
                Vector3 testPoint = World.GetNextPositionOnStreet(origin.Around(radius));
                //float CALCULATE_TRAVEL_DISTANCE_BETWEEN_POINTS(float x1, float y1, float z1, float x2, float y2, float z2)
                float drivDist = Rage.Native.NativeFunction.CallByName<float>("CALCULATE_TRAVEL_DISTANCE_BETWEEN_POINTS",
                    testPoint.X,
                    testPoint.Y,
                    testPoint.Z,
                    origin.X,
                    origin.Y,
                    origin.Z);
                if (drivDist <= maxDist)
                {
                    running = false;
                    return testPoint;
                }
                timesRun++;
            }
            return World.GetNextPositionOnStreet(origin.Around(radius));
        }
        public static void PlayerRadio(string text)
        {
            Game.DisplayNotification("~b~" + Settings.PlayerName + "~w~: " + text);
        }
        public static void DispatchRadio(string text)
        {
            Game.DisplayNotification("~b~Dispatch~w~: " + text);
        }
        public static Vector3 GetDrugDealLocation(Vector3 origin, float radius, out float heading)
        {
            List<VectorHeading> closeSpawns = new List<VectorHeading>();
            foreach (VectorHeading vh in Settings.DrugDealSpawns)
            {
                if (vh.Position.DistanceTo(origin) < radius &
                    vh.Position.DistanceTo(origin) > 100f)
                    closeSpawns.Add(vh);
            }
            if (closeSpawns.Count > 0)
            {
                VectorHeading selected = closeSpawns[new Random().Next(0, closeSpawns.Count)];
                heading = selected.Heading;
                return selected.Position;
            }
            else
            {
                heading = 0f;
                return World.GetNextPositionOnStreet(origin.Around(radius));
            }
        }
        public static Vector3 GetHVTInfo(Vector3 origin, float radius, out string name)
        {
            List<VectorHeadingTag> closeSpawns = new List<VectorHeadingTag>();
            foreach (VectorHeadingTag vh in Settings.ImportantBuildingSpawns)
            {
                if (vh.Position.DistanceTo(origin) < radius &
                    vh.Position.DistanceTo(origin) > 100f)
                    closeSpawns.Add(vh);
            }
            if (closeSpawns.Count > 0)
            {
                VectorHeadingTag selected = closeSpawns[new Random().Next(0, closeSpawns.Count)];
                name = selected.Tag;
                return selected.Position;
            }
            else
            {
                name = "Unkown";
                return World.GetNextPositionOnStreet(origin.Around(radius));
            }
        }
        /// <summary>
        /// Removed any blips and marks entity not persisten
        /// </summary>
        /// <param name="e">Target entity</param>
        public static void CleanUp(this Entity e)
        {
            if (e.IsValid())
            {
                Blip b = e.GetAttachedBlip();
                if (b.Exists())
                    b.Delete();
                e.IsPersistent = false;
            }
        }
        enum CalloutType
        {
            DrugDeal,
            Trafficking,
            StingOp
        }
        public enum StingState
        {
            EnRoute,
            Surveillance,
            CrimeObserved,
            Execution
        }
    }
}