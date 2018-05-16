using LSPD_First_Response;
using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Threading;
using System.Diagnostics;
using System.Drawing;

//Our namespace (aka folder) where we keep our callout classes.
namespace FederalCallouts.Callouts
{

    /*
     * TODO:
     * 
     */
#if DEBUG
    [CalloutInfo("TailCriminal", CalloutProbability.Always)]
#else
    [CalloutInfo("TailCriminal", CalloutProbability.Never)]
#endif
    public class TailCriminal : Callout
    {
        private Ped guard1, guard2, attacker1, attacker2, attacker3, attacker4;
        private Blip tBlip;
        private Vehicle securicar, robberyVan;
        private Vector3 SpawnPoint;
        private bool robbersEnRoute = true;
        private uint lastLocationUpdate = 0;
        private TailState state = TailState.EnRoute;


        /// <summary>
        /// OnBeforeCalloutDisplayed is where we create a blip for the user to see where the pursuit is happening, we initiliaize any variables above and set
        /// the callout message and position for the API to display
        /// </summary>
        /// <returns></returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            //RelationshipGroup r = new RelationshipGroup("ROBBERS");

            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(500f));
            GameFiber.StartNew((ThreadStart)(() =>
            {
            }));
            this.ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 15f);
            this.AddMinimumDistanceCheck(5f, SpawnPoint);
            this.CalloutMessage = "Perform mobile surveillance";
            //Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_ARMORED_CAR_ROBBERY IN_OR_ON_POSITION", securicar.Position);
            this.CalloutPosition = SpawnPoint;

            return base.OnBeforeCalloutDisplayed();
        }


        /// <summary>
        /// OnCalloutAccepted is where we begin our callout's logic. In this instance we create our pursuit and add our ped from eariler to the pursuit as well
        /// </summary>
        /// <returns></returns>
        public override bool OnCalloutAccepted()
        {
            GameFiber.StartNew((ThreadStart)(() =>
            {
                GameFiber.Wait(1000);
                Game.DisplayNotification("~b~" + Settings.PlayerName + "~w~: Copy dispatch.");
                GameFiber.Wait(1000);
                Game.DisplayNotification("~b~Dispatch~w~: Copy.");
                Functions.PlayScannerAudio("REPORT_RESPONSE_COPY");
            }));
            return base.OnCalloutAccepted();
        }

        /// <summary>
        /// If you don't accept the callout this will be called, we clear anything we spawned here to prevent it staying in the game
        /// </summary>
        public override void OnCalloutNotAccepted()
        {

            base.OnCalloutNotAccepted();
        }

        public override void Process()
        {
            base.Process();
        }

        /// <summary>
        /// More cleanup, when we call end you clean away anything left over
        /// This is also important as this will be called if a callout gets aborted (for example if you force a new callout)
        /// </summary>
        public override void End()
        {

            base.End();
        }
    }
}
public enum TailState
{
    EnRoute,
    Following,
    Action,
    Clear
}