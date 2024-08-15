using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Elements;
using System;
using System.Collections.Generic;

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

    }
}
