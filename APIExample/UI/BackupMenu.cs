using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace FederalCallouts.UI
{
    class BackupMenu
    {
        public BackupMenu()
        {
            UIMenu menu = new UIMenu("Federal Menu", "~b~Control how you want your operations to go here");
            menu.AddItem(new UIMenuItem("Takedown target","Calls in a tactical team to assist in an arrest"));
            menu.AddItem(new UIMenuItem("Simple Button"));
            menu.RefreshIndex();
        }
    }
}
