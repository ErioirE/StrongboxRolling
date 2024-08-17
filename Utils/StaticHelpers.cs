using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;

namespace StrongboxRolling.Utils
{
    internal static class StaticHelpers
    {
        internal static SBType GetStrongboxType(LabelOnGround l)
        {
            if (l.ItemOnGround.Rarity == ExileCore.Shared.Enums.MonsterRarity.Unique)
            {
                return SBType.Unique;
            }
            else if (l.ItemOnGround.RenderName.ToLower().Contains("divin"))
            {
                return SBType.Diviner;
            }
            else if (l.ItemOnGround.RenderName.ToLower().Contains("arcanis"))
            {
                return SBType.Arcanist;
            }
            else if (l.ItemOnGround.RenderName.ToLower().Contains("carto"))
            {
                return SBType.Cartographer;
            }
            else
            {
                return SBType.Regular;
            }
        }
        public static bool LabelsChanged(string[] before, string[] after)
        {
            if (before.Length != after.Length)
            {
                return true;
            }
            for (int i = 0; i < before.Length; i++)
            {
                if (before[i] != after[i])
                {
                    return true;
                }
            }
            return false;
        }

        public static string[] FindAllLabels(LabelOnGround L)
        {
            Element label = L.Label;
            return FindAllLabels(label);
        }
        public static string[] FindAllLabels(InventSlotItem I)
        {
            Entity entity = I.Item;
            return FindAllLabels(entity);
        }

        public static string[] FindAllLabels(Element L)
        {

            List<string> toReturn = new();
            try
            {
                IList<Element> children = L.Children;
                foreach (Element child in children)
                {
                    try
                    {
                        toReturn.AddRange(FindAllLabels(child));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
                if (L.Text is not null && L.Text.Length > 0)
                {
                    toReturn.Add(L.Text);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return toReturn.ToArray();
        }
        public static string[] FindAllLabels(Entity E)
        {

            List<string> toReturn = new();
            try
            {
                //Dictionary<string, long> cc = E.CacheComp;
                //Base baseComp = E.GetComponent<Base>();
                //LocalStats localStats = E.GetComponent<LocalStats>();
                Mods mods = E.GetComponent<Mods>();

                toReturn.Add("Name: " + mods.UniqueName + "");
                toReturn.Add("Rarity: " + mods.ItemRarity.ToString());
                toReturn.Add("Identified: " + mods.Identified.ToString());

                PropertyInfo[] properties = typeof(Mods).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (PropertyInfo p in properties)
                {
                    if (p.PropertyType == typeof(List<ItemMod>))
                    {
                        List<ItemMod> modList = (List<ItemMod>)p.GetValue(mods);

                        Console.WriteLine($"Property: {p.Name}, Count: {modList?.Count ?? 0}");

                        if (modList != null)
                        {
                            foreach (ItemMod mod in modList)
                            {
                                string text = mod.Translation;
                                if (!toReturn.Contains(text))
                                {
                                    toReturn.Add(text);
                                }
                            }
                        }
                    }
                }
                string md = E.Metadata;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return toReturn.ToArray();
        }
        public static Mods GetMods(Entity E)
        {

            Mods mods = E.GetComponent<Mods>();
            return mods;

        }
        public static bool WaitForMouseIcon(MouseActionType mat, Cursor cursor)
        {


            bool usingItem = false;
            int maxWait = 200;
            int totalWait = 0;

            while (!usingItem && totalWait < maxWait)
            {
                int delay = 1;

                if (cursor.Action == mat)
                {
                    usingItem = true;
                }

                Task.Delay(delay).Wait();
                totalWait += delay;
            }

            return usingItem;
        }

    }
}
