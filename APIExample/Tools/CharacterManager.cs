using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;

namespace FederalCallouts.Tools
{
    public class CharacterManager
    {
        private Model savedModel;
        private Vector3 savedPosition;
        private float savedHeading;
        private Vehicle playerVehicle;
        private bool savedVehicle = false;
        private bool switchedToBackup = false;
        /// <summary>
        /// Switches the player character to another ped. This method will take 6 seconds to complete but is asynchronous.
        /// </summary>
        /// <param name="ped">The ped to switch to</param>
        /// <param name="saveCurrent">If true, mark the current ped (and vehicle it is in) as persistent</param>
        public void SwitchToPed(Ped ped, bool saveCurrent = true)
        {
            if (saveCurrent)
            {
                switchedToBackup = false;
                if (Game.LocalPlayer.Character.CurrentVehicle.Exists())
                {
                    playerVehicle = Game.LocalPlayer.Character.CurrentVehicle;
                    savedVehicle = true;
                }
                if (playerVehicle.Exists())
                    playerVehicle.MakePersistent();
                Ped p = Game.LocalPlayer.Character;
                savedPosition = p.Position;
                savedHeading = p.Heading;
                savedModel = p.Model;
                //playerCharacter.MakePersistent();
            }
            GameFiber.StartNew((() =>
            {
                Game.FadeScreenOut(1000, true);
                //Game.LocalPlayer.Character.Model = ped.Model;
                Game.LocalPlayer.Model = ped.Model;
                //TODO: variation copying
                if (ped.CurrentVehicle.Exists() || (savedVehicle & playerVehicle.Exists()))
                {
                    Vehicle v;
                    if (ped.CurrentVehicle.Exists())
                        v = ped.CurrentVehicle;
                    else
                        v = playerVehicle;
                    ped.Delete();
                    Game.LocalPlayer.Character.WarpIntoVehicle(v, v.GetFreeSeatIndex() ?? -1);
                }
                else
                {
                    ped.Delete();
                }

                GameFiber.Sleep(2 * 1000);
                Game.FadeScreenIn(1000, true);
            }),"Character Manager Switch");

        }
        /// <summary>
        /// Switch back to the player's original character
        /// </summary>
        public void SwitchToBackup()
        {
            if (switchedToBackup)
                return;
            switchedToBackup = true;
            SwitchToPed(new Ped(savedModel, savedPosition, savedHeading), false);
        }
    }
}
