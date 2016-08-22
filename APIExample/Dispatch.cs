using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
namespace FederalCallouts
{
    public static class Dispatch
    {
        private static string lastMessage = "There was no last message.";
        private static string[] copyMessages = { "Copy that", "10-4", "Copy" };
        public static void Notify(string message)
        {
            lastMessage = message;
            Game.DisplayNotification("~b~Dispatch~w~: " + message);
        }
        public static void Copy()
        {
            string c = copyMessages[new Random().Next(0, copyMessages.Length)];
            Game.DisplayNotification("~b~Dispatch~w~: " + c + ".");
        }
        /// <summary>
        /// Same as copy, however will repeat specified phrase
        /// </summary>
        /// <param name="phrase">phrase to repeat</param>
        public static void Copy(string phrase)
        {
            string c = copyMessages[new Random().Next(0, copyMessages.Length)];
            Game.DisplayNotification("~b~Dispatch~w~: " + c + ", " + phrase + ".");
        }
        public static void PlayerSay(string message)
        {
            Game.DisplayNotification("~b~" + Settings.PlayerName + "~w~: " + message);
        }
        public static void RepeatLast()
        {
            Game.DisplayNotification("~b~Dispatch~w~: " + lastMessage);
        }
    }
}
