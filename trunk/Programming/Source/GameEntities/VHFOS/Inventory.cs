using System;
using System.Collections.Generic;
using System.Text;
using GameEntities;
using Engine.UISystem;
using System.Collections;
using Engine.MathEx;
using Engine.Renderer;

namespace GameEntities
{
    public static class InventoryHelpers
    {
        public static Vec2 BattleShip2Vector(string battleVector)
        {
            float realX = 0.0f;
            float realY;

            string x = battleVector.Substring(0, 1).ToUpper();
            string y = battleVector.Substring(1);

            if (x == "A")
                realX = 0.0f;
            else if (x == "B")
                realX = 1.0f;
            else if (x == "C")
                realX = 2.0f;
            else if (x == "D")
                realX = 3.0f;

            float.TryParse(y, out realY);

            return new Vec2(realX, (realY - 1));
        }

        public static string Vector2BattleShip(Vec2 vector)
        {
            string realX = "";
            string realY;
            int otherY;

            if (vector.X == 0.0f)
                realX = "A";
            else if (vector.X == 1.0f)
                realX = "B";
            else if (vector.X == 2.0f)
                realX = "C";
            else if (vector.X == 3.0f)
                realX = "D";

            otherY = (int)vector.Y;

            realY = (otherY + 1).ToString();

            return realX + realY;
        }
    }


    public class Inventory
    {
        Hashtable inventory;

        public Inventory()
        {
            this.inventory = new Hashtable();
        }

        public Hashtable getHashtable()
        {
            return inventory;
        }

        public bool AddItem(Vec2 location, Item theItem)
        {
            if (this.inventory.ContainsKey(location))
                return false;
            else
            {
                this.inventory.Add(location, theItem);
                return true;
            }
        }
        public bool AddItem(string battleShipPos, Item theItem)
        {
            Vec2 location = InventoryHelpers.BattleShip2Vector(battleShipPos);

            if (this.inventory.ContainsKey(location))
                return false;
            else
            {
                this.inventory.Add(location, theItem);
                return true;
            }
        }
        /// <summary>
        /// This returns true if it can be taken into inventory, false if full.
        /// </summary>
        /// <param name="theItem"></param>
        /// <returns></returns>
        public bool AddItem(Item theItem)
        {
            Vec2 pos = findFreeSpace();

            if (pos.X == -1.0f && pos.Y == -1.0f)
                return false;

            this.AddItem(pos, theItem);

            return true;
        }

        public void RemoveItem(Vec2 location, EControl hud)
        {
            this.inventory.Remove(location);
            hud.Controls["Inventory/" + InventoryHelpers.Vector2BattleShip(location)].BackTexture = TextureManager.Instance.Load(@"Gui\Inventory\kill.png", Texture.Type.Type2D, 0);
        }
        public void RemoveItem(string battleshipPos, EControl hud)
        {
            Vec2 thePos = InventoryHelpers.BattleShip2Vector(battleshipPos);

            this.inventory.Remove(thePos);
            hud.Controls["Inventory/" + battleshipPos].BackTexture = TextureManager.Instance.Load(@"Gui\HUD\BLOCK.tga", Texture.Type.Type2D, 0);
        }
        public Item GetItem(Vec2 pos)
        {
            if (this.inventory.ContainsKey(pos))
                return (Item)this.inventory[pos];
            else
                return null;
        }

        public void OnRender(EControl hud)
        {
            foreach (DictionaryEntry dItem in this.inventory)
            {
                Item theOrigItem = (Item)dItem.Value;

                if (theOrigItem.Type.InventoryIcon == null)
                    theOrigItem.Type.InventoryIcon = @"Gui\Inventory\inativate.jpg";

                hud.Controls["Inventory/" + InventoryHelpers.Vector2BattleShip((Vec2)dItem.Key)].BackTexture = TextureManager.Instance.Load(theOrigItem.Type.InventoryIcon, Texture.Type.Type2D, 0);
            }

            // hud.Controls["Inventory/A1"].BackTexture = "Gui\HUD\BLOCK.tga";
        }

        private Vec2 findFreeSpace()
        {
            int x = 0;
            int y = 0;

            Vec2 pos;

            for (x = 0; x < 4; x++)
            {
                for (y = 0; y < 2; y++)
                {
                    pos = new Vec2((float)x, (float)y);

                    if (!inventory.ContainsKey(pos))
                        return pos;
                }

                pos = new Vec2((float)x, (float)y);

                if (!inventory.ContainsKey(pos))
                    return pos;
            }

            return new Vec2(-1.0f, -1.0f);
        }

        public int TotalInventoryItems()
        {
            return inventory.Count;
        }

        public void ClearInventory(EControl hudControl)
        {
            this.inventory.Clear();

            foreach (EControl inventoryControl in hudControl.Controls["Inventory"].Controls)
            {
            //    EButton button = inventoryControl as EButton;

            //    if (button != null)
            //        button.BackTexture = TextureManager.Instance.Load(@"Gui\HUD\BLOCK.tga", Texture.Type.Type2D, 0);
            }
        }
    }
}
