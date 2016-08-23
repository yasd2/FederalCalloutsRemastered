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
        private Dictionary<int, Vector2> savedVariation;
        PedInventory savedInventory;
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
                savedInventory = p.Inventory;
                savedVariation = new Dictionary<int, Vector2>();
                int currDraw;
                int currDrawTex;
                for (int i = 0; i < 11; i++)
                {
                    p.GetVariation(i, out currDraw, out currDrawTex);
                    savedVariation.Add(i, new Vector2(currDraw, currDrawTex));
                }
                
            }
            GameFiber.StartNew((() =>
            {
                Game.FadeScreenOut(1000, true);
                foreach (WeaponDescriptor w in Game.LocalPlayer.Character.Inventory.Weapons)
                    ped.Inventory.GiveNewWeapon(w, w.Ammo, false);
                Game.LocalPlayer.Model = ped.Model;
                foreach (WeaponDescriptor w in ped.Inventory.Weapons)
                    Game.LocalPlayer.Character.Inventory.GiveNewWeapon(w, w.Ammo, false);
                //TODO: backup weapons
                CopyVariationToPed(ped, Game.LocalPlayer.Character);
                
                
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
                    Game.LocalPlayer.Character.Position = ped.Position;
                    Game.LocalPlayer.Character.Heading = ped.Heading;
                    ped.Delete();
                }

                GameFiber.Sleep(2 * 1000);
                Game.FadeScreenIn(1000, true);
            }), "Character Manager Switch");

        }
        /// <summary>
        /// Switch back to the player's original character
        /// </summary>
        public void SwitchToBackup()
        {
            if (switchedToBackup)
                return;
            switchedToBackup = true;

            Ped backup = new Ped(savedModel, savedPosition, savedHeading);
            LoadVariationToPed(backup);
            SwitchToPed(backup, false);
        }

        public void LoadVariationToPed(Ped p)
        {
            for (int i = 0; i < 11; i++)
                p.SetVariation(i, (int)savedVariation[i].X, (int)savedVariation[i].Y);
        }

        public void CopyVariationToPed(Ped from, Ped to)
        {
            Dictionary<int, Vector2> copiedVariation = new Dictionary<int, Vector2>();
            int currDraw;
            int currDrawTex;
            for (int i = 0; i < 11; i++)
            {
                from.GetVariation(i, out currDraw, out currDrawTex);
                copiedVariation.Add(i, new Vector2(currDraw, currDrawTex));
            }
            for (int i = 0; i < 11; i++)
                to.SetVariation(i, (int)copiedVariation[i].X, (int)copiedVariation[i].Y);
        }
    }
}
