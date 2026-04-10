using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Threading;
using System.Diagnostics;
using System.Drawing;
using static FederalCallouts.Extensions;

namespace FederalCallouts.Callouts
{
#if DEBUG
    [CalloutInfo("BombSting", CalloutProbability.Medium)]
#else
    [CalloutInfo("BombSting", CalloutProbability.Medium)]
#endif
    public class BombSting : Callout
    {
        private Vector3 spawnPoint;
        private Vector3 hvtLocation;
        private string hvtName;
        private Ped bomber;
        private Vehicle bombVan;
        private Blip b, targetArea;
        StingState state = StingState.EnRoute;
        /// <summary>
        /// OnBeforeCalloutDisplayed is where we create a blip for the user to see where the pursuit is happening, we initiliaize any variables above and set
        /// the callout message and position for the API to display
        /// </summary>
        /// <returns></returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            spawnPoint = FindCloseSpawn(Game.LocalPlayer.Character.Position, 300f, 350f);
            hvtLocation = GetHVTInfo(spawnPoint, 1000f, out hvtName);

            bomber = new Ped(spawnPoint);
            bombVan = new Vehicle("burrito3", spawnPoint);

            //Now we have spawned them, check they actually exist and if not return false (preventing the callout from being accepted and aborting it)
            if (!bomber.Exists()) return false;
            if (!bombVan.Exists()) return false;

            //If we made it this far both exist so let's warp the ped into the driver seat
            bomber.WarpIntoVehicle(bombVan, -1);

            // Show the user where the pursuit is about to happen and block very close peds.
            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 15f);
            AddMinimumDistanceCheck(5f, spawnPoint);
            //todo: make this not say drug deal
            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_DRUG_DEAL IN_OR_ON_POSITION", bomber.Position);
            CalloutMessage = "Execute sting operation";
            CalloutPosition = spawnPoint;
            return base.OnBeforeCalloutDisplayed();
        }


        /// <summary>
        /// OnCalloutAccepted is where we begin our callout's logic. In this instance we create our pursuit and add our ped from eariler to the pursuit as well
        /// </summary>
        /// <returns></returns>
        public override bool OnCalloutAccepted()
        {
            GameFiber.StartNew(() =>
            {
                Game.DisplayNotification("~b~Dispatch~w~: Follow the ~r~suspect~w~ until he parks his vehicle.");
                GameFiber.Wait(1000);
                Game.DisplayNotification("~b~Dispatch~w~: Arrest him after he uses his ~r~phone~w~ in an attempt to activate the bomb.");
                GameFiber.Wait(1000);
                Game.DisplayNotification("~b~Dispatch~w~: The target location is the ~y~" + hvtName);
            });
            Game.DisplayNotification("~b~Dispatch~w~: Follow the ~r~suspect~w~ until he parks his vehicle.");
            Game.DisplayNotification("~b~Dispatch~w~: Arrest him after he uses his ~r~phone~w~ in an attempt to activate the bomb.");
            //bomber.Tasks.CruiseWithVehicle(bombVan, 30.0f, DriveToPositionFlags.DriveAroundVehicles | DriveToPositionFlags.DriveAroundObjects);
            bomber.Tasks.DriveToPosition(hvtLocation, 45f, VehicleDrivingFlags.DriveAroundPeds
                | VehicleDrivingFlags.DriveAroundVehicles
                | VehicleDrivingFlags.Normal);
            b = bomber.AttachBlip();
            /*
            GameFiber.StartNew(() =>
            {
                GameFiber.Wait(15 * 1000);
                Game.DisplayHelp("Press ~g~Y~w~ at any time to move in on suspect.");
            });
            */
            targetArea = new Blip(hvtLocation, 60f);
            targetArea.Color = Color.FromArgb(100, 255, 255, 0);
            return base.OnCalloutAccepted();
        }

        /// <summary>
        /// If you don't accept the callout this will be called, we clear anything we spawned here to prevent it staying in the game
        /// </summary>
        public override void OnCalloutNotAccepted()
        {
            if (bombVan.Exists()) { bombVan.Dismiss(); }
            if (bomber.Exists()) { bomber.Dismiss(); }
            if (b.Exists()) { b.Delete(); }
            if (targetArea.Exists()) { targetArea.Delete(); }
            base.OnCalloutNotAccepted();
        }

        //This is where it all happens, run all of your callouts logic here
        public override void Process()
        {
            if (state == StingState.EnRoute & Vector3.Distance(bomber.Position, hvtLocation) < 7f)
            {
                state = StingState.Surveillance;
                StartCrime();
            }
            base.Process();
            if (!bomber.IsAlive | Functions.IsPedArrested(bomber))
            {
                if (!bomber.IsAlive)
                    GameFiber.StartNew((() =>
                    {
                        GameFiber.Wait(1000);

                        GameFiber.Wait(2000);
                        Functions.PlayScannerAudio("REPORT_RESPONSE_COPY SUSPECT_DOWN");
                        Game.DisplayNotification("~b~Dispatch~w~: Copy, suspect down.");
                    }));
                if (Functions.IsPedArrested(bomber))
                    GameFiber.StartNew((ThreadStart)(() =>
                    {
                        Game.DisplayNotification("~b~" + Settings.PlayerName + "~w~: Dispatch, we have the bomber in custody.");
                        GameFiber.Wait(2000);
                        Functions.PlayScannerAudio("REPORT_RESPONSE_COPY SUSPECT_ARRESTED");
                        Game.DisplayNotification("~b~Dispatch~w~: 10-4, suspect is in custody.");
                    }));
                End();
            }
        }
        void StartCrime()
        {
            GameFiber.StartNew((() =>
            {
                bomber.Tasks.LeaveVehicle(LeaveVehicleFlags.None);
                while (bomber.IsInVehicle(bombVan, false))
                {
                    GameFiber.Wait(100);
                }
                bomber.Tasks.Wander();
                GameFiber.Wait(20 * 1000);
                if (bomber.IsAlive)
                {
                    Game.LogTrivial("[FC] Suspect is using phone");
                    //TASK_USE_MOBILE_PHONE_TIMED(Ped ped, int duration)
                    Rage.Native.NativeFunction.CallByName<uint>("TASK_USE_MOBILE_PHONE_TIMED", bomber, 15 * 1000);
                    GameFiber.Wait(3000);
                    if (MathHelper.GetRandomInteger(0, 100) < 40)
                    {
                        bombVan.Explode(true);
                        Game.DisplaySubtitle("The ~r~suspect~w~ must have gotten a real bomb!", 15 * 1000);
                    }
                    else
                    {
                        switch (MathHelper.GetRandomInteger(2))
                        {
                            case 0: 
                                Game.DisplaySubtitle("It appears that the bomb is fake.", 8000);
                                break;

                            case 1:
                                Game.DisplaySubtitle("The bomb didn't explode.", 8000);
                                break;

                            case 2:
                                Game.DisplaySubtitle("Detain the suspect and check the van for explosives.", 8000);
                                break;
                        }
                    }
                }
            }));
        }
        /// <summary>
        /// More cleanup, when we call end you clean away anything left over
        /// This is also important as this will be called if a callout gets aborted (for example if you force a new callout)
        /// </summary>
        public override void End()
        {
            base.End();
            if (bombVan.Exists()) { bombVan.Dismiss(); }
            if (bomber.Exists()) { bomber.Dismiss(); }
            if (targetArea.Exists()) { targetArea.Delete(); }
            if (b.Exists()) { b.Delete(); }

        }
    }
}
