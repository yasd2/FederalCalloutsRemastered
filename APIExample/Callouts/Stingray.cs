
using FederalCallouts.Tools;
using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System.Drawing;

namespace FederalCallouts.Callouts
{
#if DEBUG
    [CalloutInfo("Stingray", CalloutProbability.Always)]
#else
    [CalloutInfo("Stingray", CalloutProbability.High)]
#endif

    public class Stingray : Callout
    {

        private Ped suspect;
        private Vehicle helo;
        private Blip suspectBlip;
        private Vector3 SpawnPoint;
        private CharacterManager charManager;
        private bool switchedCharacter = true;
        private StingrayCalloutState state = StingrayCalloutState.Start;
        TimerBarPool pool;
        BarTimerBar stingBar;
        private string[] weapons = {
                                       "WEAPON_KNIFE",
                                       "WEAPON_HAMMER",
                                       "WEAPON_BAT",
                                       "WEAPON_CROWBAR",
                                       "WEAPON_PISTOL",
                                       "WEAPON_COMBATPISTOL",
                                       "WEAPON_APPISTOL",
                                       "WEAPON_PISTOL50",
                                       "WEAPON_MICROSMG",
                                       "WEAPON_SAWNOFFSHOTGUN",
                                       "WEAPON_STUNGUN"
                                   };
        public override bool OnBeforeCalloutDisplayed()
        {
            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(600f));
            CalloutMessage = "Operate Stingray Device";
            CalloutPosition = SpawnPoint;
            charManager = new CharacterManager();
            return base.OnBeforeCalloutDisplayed();
        }


        public override bool OnCalloutAccepted()
        {
            pool = new TimerBarPool();
            if (!(Game.LocalPlayer.Character.IsInAirVehicle))
            {
                Vector3 spawn = new Vector3(SpawnPoint.X, SpawnPoint.Y, SpawnPoint.Z + 600);
                
                Ped pilot = new Ped(new Model("S_M_M_PILOT_02"), spawn, 0);
                helo = new Vehicle("maverick", spawn);
                helo.IsEngineOn = true;
                helo.IsEngineStarting = true;
                helo.DriveForce = 1000000f;
                helo.LockStatus = VehicleLockStatus.Locked;
                helo.MakePersistent();
                helo.IsGravityDisabled = true;
                
                GameFiber.StartNew((() =>
                {
                    helo.IsGravityDisabled = true;
                    charManager.SwitchToPed(pilot);
                    GameFiber.Sleep(3000);
                    helo.Rotation = new Rotator(0, 0, 0);
                    Game.LocalPlayer.Character.WarpIntoVehicle(helo, helo.GetFreeSeatIndex() ?? -1);
                    helo.DriveForce = 1000000f;
                    GameFiber.Sleep(1000);
                    helo.Rotation = new Rotator(0, 0, 0);
                    helo.IsGravityDisabled = false;
                    GameFiber.Sleep(1000);
                    Game.DisplayHelp("Press ~g~" + Settings.StartKey.ToString() + "~w~ at any time to start the Stingray.");
                }));
            }
            else
            {
                switchedCharacter = false;
                Game.DisplayHelp("Press ~g~" + Settings.StartKey.ToString() + "~w~ at any time to start the Stingray.");
            }
            return base.OnCalloutAccepted();
        }


        public override void OnCalloutNotAccepted()
        {

        }

        public override void Process()
        {
            if (state == StingrayCalloutState.Start & Game.IsKeyDown(Settings.StartKey))
            {
                state = StingrayCalloutState.Scanning;
                stingBar = new BarTimerBar("SCANNING");
                stingBar.Percentage = 0f;
                pool.Add(stingBar);
            }
            if (state == StingrayCalloutState.Scanning)
            {
                pool.Draw();
                stingBar.Percentage += 0.03f * Game.FrameTime;
                if (stingBar.Percentage >= 1f)
                {
                    state = StingrayCalloutState.Scanned;
                }
            }
            if (state == StingrayCalloutState.Scanned)
            {
                SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(200f));
                suspect = new Ped(SpawnPoint);
                bool armed;
                Realism.GetArrestReason(out armed);
                suspectBlip = suspect.AttachBlip();
                suspectBlip.EnableRoute(Color.Yellow);
                suspectBlip.Scale = 0.75f;
                Dispatch.Notify("~g~Ground teams~w~: You have a green light to move in.");
                if (switchedCharacter)
                    charManager.SwitchToBackup();
                else
                {
                    throw new System.Exception("Not implemented!");
                }
                //TODO: Make suspect do something base on being armed or not.
                state = StingrayCalloutState.Action;
            }
            if (state == StingrayCalloutState.Action)
            {
                End();
            }
            base.Process();
        }
        public override void End()
        {
            if (suspectBlip.Exists())
                suspectBlip.Delete();
            if (suspect.Exists())
                suspect.IsPersistent = false;
            pool.Remove(stingBar);
            charManager.SwitchToBackup();
            base.End();

        }
    }
    public enum StingrayCalloutState
    {
        Start,
        Scanning,
        Scanned,
        Action
    }
}
