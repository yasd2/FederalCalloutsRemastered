using Rage;
using System;
using RAGENativeUI.Elements;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FederalCallouts.UI
{
    public class PedAwarenessIndicator
    {
        //TODO: Fix exception where it bugs out when player exits vehicle
        Ped target;
        Ped player = Game.LocalPlayer.Character;
        int awareness = 0;
        int threshold = 1000;
        bool shouldDraw = true;
        TimerBarPool pool;
        BarTimerBar indicator;
        public event NoticedHandler Noticed;
        public delegate void NoticedHandler(EventArgs e);
        public PedAwarenessIndicator(Ped ped)
        {
            target = ped;
            pool = new TimerBarPool();
            indicator = new BarTimerBar("Target Awareness");
            indicator.Percentage = 0F;
            pool.Add(indicator);
            //Establishing base awareness values
            if (player.DistanceTo(target) < 20F)
                awareness = 300;
            else if (player.DistanceTo(target) < 50F)
                awareness = 100;
            else if (player.DistanceTo(target) < 75F)
                awareness = 50;
            else if (player.DistanceTo(target) < 100F)
                awareness = 0;
        }
        public void Think()
        {
            int scale = 0;
            bool dirty = false;
            if (player.DistanceTo(target) <= 20F)
                scale = 3;
            else if (player.DistanceTo(target) <= 60F | (player.DistanceTo(target) < 75F && player.IsUniformed() && !player.IsInCover))
                scale = 2;
            else if (player.DistanceTo(target) >= 60F && player.DistanceTo(target) < 110F)
                scale = 1;
            else if (player.DistanceTo(target) < 120F | player.IsInCover)
                scale = 0;
            //if (player.DistanceTo(target) < 75F & !player.IsInCover)
            //    Game.LogTrivialDebug(awareness.ToString() + "|" + indicator.Percentage);
            //TODO:
            //Make scale lower if player is behind the target
            try
            {
                if (player.IsInAnyVehicle(false))
                {
                    //These must be nested to prevent NullReferenceException
                    if (player.CurrentVehicle.IsMarked())
                    {
                        awareness += (int)((50 * scale) * Game.FrameTime);
                        dirty = true;
                    }
                    else if (player.IsUniformed())
                    {
                        awareness += (int)((15 * scale) * Game.FrameTime);
                        dirty = true;
                    }

                    //This is set to intentionally stack onto the awareness of the previous two statements
                    if (player.CurrentVehicle.IsSirenOn)
                    {
                        awareness += (int)((30 * scale) * Game.FrameTime);
                        dirty = true;
                    }
                }
                else if (player.IsUniformed() & !player.IsInCover & !player.IsInAnyVehicle(true))
                {
                    awareness += (int)((23 * scale) * Game.FrameTime);
                    dirty = true;
                }
                if (player.DistanceTo(target) < 20F & !player.IsInCover)
                {
                    awareness += (int)((35 * scale) * Game.FrameTime);
                    dirty = true;
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("Federal Callouts caught an exception: " + ex.ToString());
            }
            //Only reduce awareness if it is not increasing
            if (!dirty | scale == 0)
                awareness -= (int)(60 * Game.FrameTime);
            if (awareness < 0)
                awareness = 0;
            indicator.Percentage = (float)awareness / threshold;
            if (awareness >= threshold)
            {
                indicator.Percentage = 1F;
                Noticed(null);
                shouldDraw = false;
                pool.Remove(indicator);
            }
            else if(shouldDraw)
            {
                pool.Draw();
            }
        }
    }
}