using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using Rage.Native;
using System;
using System.Threading;

namespace FederalCallouts.Callouts
{
#if DEBUG
    [CalloutInfo("Kidnapping", CalloutProbability.Medium)]
#else
    [CalloutInfo("Kidnapping", CalloutProbability.High)]
#endif
    
    class Kidnapping : Callout
    {


        private Ped suspect;
        private Ped victim = null;
        private Vehicle currentVehicle;
        private Blip suspectBlip;
        private bool currentSuspectGuilty = false;
        private bool currentSuspectStopped = false;
        //private int minimumForGuilty = 20;
        private Vector3 SpawnPoint;
        //private LHandle pursuit;
        //private String[] peds = { "s_m_y_prisoner_01", "u_m_y_prisoner_01" };
        //private KidnappingState state = KidnappingState.Searching;

        /// <summary>
        /// OnBeforeCalloutDisplayed is where we create a blip for the user to see where the pursuit is happening, we initiliaize any variables above and set
        /// the callout message and position for the API to display
        /// </summary>
        /// <returns></returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(450f));
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 15f);
            AddMinimumDistanceCheck(5f, SpawnPoint);
            suspect = new Ped(SpawnPoint);
            currentVehicle = new Vehicle("burrito3", SpawnPoint);
            suspect.WarpIntoVehicle(currentVehicle, -1);
            suspect.Tasks.CruiseWithVehicle(currentVehicle, 13.0f, VehicleDrivingFlags.RespectIntersections | VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.AvoidHighways);
            if (MathHelper.GetRandomInteger(0, 101) < Settings.KidnappingMinimumPercent)
            {
                Game.LogTrivialDebug("[FC] This suspect is guilty");
                currentSuspectGuilty = true;
                victim = new Ped(SpawnPoint);
                //Put em in the back ;)
                suspect.RelationshipGroup = "CRIMINALS";
                Game.SetRelationshipBetweenRelationshipGroups("CIVMALE", "CRIMINALS", Relationship.Dislike);
                Game.SetRelationshipBetweenRelationshipGroups("CIVFEMALE", "CRIMINALS", Relationship.Dislike);
                //Put em in the back ;)
                victim.WarpIntoVehicle(currentVehicle, 1);
            }
            Functions.PlayScannerAudio("ATTENTION WE_HAVE CRIME_KIDNAPPING");
            CalloutMessage = "Kidnapping";
            CalloutPosition = SpawnPoint;
            return base.OnBeforeCalloutDisplayed();
        }


        /// <summary>
        /// OnCalloutAccepted is where we begin our callout's logic. In this instance we create our pursuit and add our ped from eariler to the pursuit as well
        /// </summary>
        /// <returns></returns>
        public override bool OnCalloutAccepted()
        {
            suspectBlip = suspect.AttachBlip();
            GameFiber.StartNew((() =>
            {
                Game.DisplayNotification("~b~" + Settings.PlayerName + "~w~: Copy that dispatch, beginning the search.");
                GameFiber.Wait(750);
                Game.DisplayNotification("~b~Dispatch~w~: We'll let you know if we get updated information.");
                GameFiber.Wait(750);
                Game.DisplayNotification("~b~" + Settings.PlayerName + "~w~: 10-4");
            }));
            return base.OnCalloutAccepted();
        }

        /// <summary>
        /// If you don't accept the callout this will be called, we clear anything we spawned here to prevent it staying in the game
        /// </summary>
        public override void OnCalloutNotAccepted()
        {
            if (suspect.Exists()) { suspect.Delete(); }
            if (victim.Exists()) { victim.Delete(); }
            if (currentVehicle.Exists()) { currentVehicle.Delete(); }
            if (suspectBlip.Exists()) { suspectBlip.Delete(); }
            base.OnCalloutNotAccepted();
        }

        //This is where it all happens, run all of your callouts logic here
        public override void Process()
        {

            if (Functions.IsPlayerPerformingPullover() & !currentSuspectStopped)
            {
                //Nesting this because GetPulloverSuspect cannot be passed a null value
                if (Functions.GetPulloverSuspect(Functions.GetCurrentPullover()) == suspect)
                {
                    Game.LogTrivialDebug("[FC] Suspect stopped");
                    currentSuspectStopped = true;
                }
                //Game.DisplayHelp("~r~Look in the back of the van.", 15 * 1000);
            }

            if (currentSuspectStopped & !Functions.IsPlayerPerformingPullover() & !currentSuspectGuilty & Functions.GetActivePursuit() == null)
            {
                Game.LogTrivialDebug("[FC] Getting new suspect");
                currentSuspectStopped = false;
                
                GameFiber.StartNew((() =>
                {
                    SpawnPoint = Extensions.FindCloseSpawn(World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(300f)), 300f, 400f);
                    if (suspectBlip.Exists())
                        suspectBlip.Delete();
                    suspect.Dismiss();
                    currentVehicle.Dismiss();
                    suspect = new Ped(SpawnPoint);
                    currentVehicle = new Vehicle("burrito3", SpawnPoint);
                    suspect.WarpIntoVehicle(currentVehicle, -1);
                    suspectBlip = suspect.AttachBlip();
                    suspect.Tasks.CruiseWithVehicle(currentVehicle, 13.0f, VehicleDrivingFlags.RespectIntersections | VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.AvoidHighways);
                    if (MathHelper.GetRandomInteger(0, 101) < Settings.KidnappingMinimumPercent)
                    {
                        Game.LogTrivialDebug("[FC] This suspect is guilty");
                        currentSuspectGuilty = true;
                        victim = new Ped(SpawnPoint);
                        victim.RelationshipGroup = "victims";
                        suspect.RelationshipGroup = "CRIMINALS";
                        Game.SetRelationshipBetweenRelationshipGroups("victims", "CRIMINALS", Relationship.Dislike);
                        Game.SetRelationshipBetweenRelationshipGroups("victims", Game.LocalPlayer.Character.RelationshipGroup, Relationship.Companion);
                        //Put em in the back ;)
                        victim.WarpIntoVehicle(currentVehicle, 1);
                    }
                    Functions.PlayScannerAudioUsingPosition("ATTENTION SUSPECT_LAST_SEEN IN_OR_ON_POSITION", suspect.Position);
                    Game.DisplayNotification("~b~Dispatch~w~: Here is the location of the next suspect.");
                    GameFiber.Wait(750);
                    Game.DisplayNotification("~b~" + Settings.PlayerName + "~w~: 10-4");
                }));
            }
            //SUSPECT_LAST_SEEN
            if (currentSuspectGuilty & (!suspect.IsAlive || Functions.IsPedArrested(suspect)))
            {
                if (!suspect.IsAlive)
                    GameFiber.StartNew((ThreadStart)(() =>
                    {
                        GameFiber.Wait(1000);
                        Game.DisplayNotification("~b~" + Settings.PlayerName + "~w~: Dispatch, the suspect is dead and we have located the victim.");
                        GameFiber.Wait(2000);
                        Functions.PlayScannerAudio("REPORT_RESPONSE_COPY SUSPECT_DOWN");
                        Game.DisplayNotification("~b~Dispatch~w~: 10-4.");
                    }));
                if (Functions.IsPedArrested(suspect))
                    GameFiber.StartNew((ThreadStart)(() =>
                    {
                        Game.DisplayNotification("~b~" + Settings.PlayerName + "~w~: Dispatch, suspect is in custody and we have located the victim.");
                        GameFiber.Wait(2000);
                        Functions.PlayScannerAudio("REPORT_RESPONSE_COPY SUSPECT_ARRESTED");
                        Game.DisplayNotification("~b~Dispatch~w~: 10-4.");
                    }));
                End();
            }
            base.Process();
        }

        /// <summary>
        /// More cleanup, when we call end you clean away anything left over
        /// This is also important as this will be called if a callout gets aborted (for example if you force a new callout)
        /// </summary>
        public override void End()
        {
            if (suspect.Exists()) { suspect.Dismiss(); }
            if (victim.Exists()) { victim.Dismiss(); }
            if (currentVehicle.Exists()) { currentVehicle.Dismiss(); }
            if (suspectBlip.Exists()) { suspectBlip.Delete(); }
            base.End();
        }
    }
    public enum KidnappingState
    {
        Searching,
        OnScene,
        Action
    }
}
