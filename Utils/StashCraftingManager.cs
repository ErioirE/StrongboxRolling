using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ItemFilterLibrary;
using System.Windows.Forms;

namespace StrongboxRolling.Utils
{
    public class StashCraftingManager
    {
        public static Regex Weird = new(@"[^A-Za-z0-9\ ]");
        public string[] prevMods = Array.Empty<string>();
        public int currencyStashIndex = 2;
        public StrongboxRolling instance;

        public StashCraftingManager(StrongboxRolling ins)
        {
            instance = ins;
        }
        public bool CraftStep(Regex mods, Entity target)
        {
            try
            {
                if (CheckMods(mods, target))
                {

                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.ToString());
            }
            return false;
        }
        public bool CheckMods(Regex goodMods, Entity toCraft)
        {
            try
            {
                string[] allMods = StaticHelpers.FindAllLabels(toCraft);
                Mods modData = StaticHelpers.GetMods(toCraft);

                allMods = allMods.Select(x => Weird.Replace(x, "").ToLower()).ToArray();
                string added = String.Join(" ", allMods);
                //toCraft.Item.TryGetComponent<ObjectMagicProperties>(out ObjectMagicProperties magicPropsC);



                if (goodMods.IsMatch(added))
                {

                    
                    if (modData.ExplicitMods.Count == 1)
                    {
                        if (GetAugsFromInv().Any())
                        {
                            CraftWithItem(GetAugsFromInv().First(), toCraft);
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                instance.LogError(ex.ToString());
            }
            return false;
        }
        public bool CraftWithItem(Entity currency, Entity target)
        {
            if (instance.FullWork)
            {
                return false;
            }


            string[] labels = StaticHelpers.FindAllLabels(target);
            List<string> toLog = new();

            toLog.Add(@$"{DateTime.Now.ToString("yyyy-mm-dd_T")}");
            toLog.Add(@$"{currency.RenderName}");
            toLog.AddRange(labels);
            if (!currency.Metadata.ToLower().Contains("ident") && labels.Where(x => x.ToLower().Contains("unidentified")).Any())
            {
                return false;
            }
            string allMods = string.Join(" ", toLog);

            File.AppendAllLines(@"./craftingLog.txt", toLog);

            if (!instance.GameController.Window.IsForeground()) return false;
            //if (!IngameState.pTheGame.IngameState.IngameUi.InventoryPanel.IsVisibleLocal)
            //{
            //    SendKeys.SendWait("i");

            //    Task.Delay(instance.Settings.BoxCraftingMidStepDelay).Wait();
            //}
            Mouse.MoveCursorToPosition(instance.GetPos(currency));
            
            Task.Delay(instance.Settings.BoxCraftingMidStepDelay).Wait();
            if (!StaticHelpers.WaitForMouseIcon(MouseActionType.Free, instance.GameController.IngameState.IngameUi.Cursor))
            {
                return false;
            }
            Mouse.RightClick(instance.Settings.BoxCraftingMidStepDelay);
            if (!StaticHelpers.WaitForMouseIcon(MouseActionType.UseItem, instance.GameController.IngameState.IngameUi.Cursor))
            {
                return false;
            }

            Task.Delay(instance.Settings.BoxCraftingMidStepDelay).Wait();
            //Mouse.SetCursorPos(GetPos(toCraft));
            Mouse.LinearSmoothMove(instance.GetPos(target));
            bool? isTargeted = target.GetComponent<Targetable>()?.isTargeted;
            int limit = 200;
            int i = 0;
            while (isTargeted is null || !isTargeted.Value)
            {
                Task.Delay(1).Wait();
                if (i >= limit)
                {
                    return false;
                }
                i++;
                isTargeted = target.GetComponent<Targetable>()?.isTargeted;
            }
            if (isTargeted is not null && isTargeted.Value)
            {

                Task.Delay(instance.Settings.BoxCraftingMidStepDelay).Wait();
                if (!StaticHelpers.WaitForMouseIcon(MouseActionType.UseItem, instance.GameController.IngameState.IngameUi.Cursor))
                {
                    return false;
                }
                Mouse.LeftClick(instance.Settings.BoxCraftingMidStepDelay);
                StaticHelpers.WaitForMouseIcon(MouseActionType.Free, instance.GameController.IngameState.IngameUi.Cursor);
            }
            else
            {

            }

            Task.Delay(instance.Settings.BoxCraftingStepDelay).Wait();
            Task.Delay(100).Wait();
            return true;
        }
        public Entity? GetItemInCraftingZone(string currencyTabName)
        {
            try
            {
                StashElement stash = IngameState.pTheGame.IngameState.IngameUi.StashElement;
                if (!stash.IsVisibleLocal)
                {
                    return null;
                }
                IList<ExileCore.PoEMemory.Element> tablist = IngameState.pTheGame.IngameState.IngameUi.StashElement.GetTabListButtons();
                Element currencyTab = tablist[2];
                if (currencyTab is null)
                {
                    return null;
                }

                int? ctIndexQ = currencyTab.IndexInParent;
                if (ctIndexQ is null)
                {
                    return null;
                }

                int ctIndex = ctIndexQ.Value;
                if (stash.IndexVisibleStash != ctIndex)
                {
                    SharpDX.Vector2 buttonpos = currencyTab.GetClientRect().Center;
                    Mouse.LinearSmoothMove(buttonpos);
                    Mouse.LeftClick(50);
                    Task.Delay(200).Wait();
                }
                Inventory visibleStashInv = stash.VisibleStash;
                ServerInventory se = visibleStashInv.ServerInventory;
                Entity toReturn = se.Items.Where(x => !x.Metadata.Contains("Metadata/Items/Currency") && !x.Metadata.Contains("Metadata/Items/DivinationCards")).FirstOrDefault();
                return toReturn;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }
        public IEnumerable<Entity> GetContentsOfStashTab(int index)
        {
            try
            {
                StashElement stash = IngameState.pTheGame.IngameState.IngameUi.StashElement;
                if (!stash.IsVisibleLocal)
                {
                    return new Entity[0];
                }
                IList<ExileCore.PoEMemory.Element> tablist = IngameState.pTheGame.IngameState.IngameUi.StashElement.GetTabListButtons();
                Element currencyTab = tablist[index];
                if (currencyTab is null)
                {
                    return new Entity[0];
                }

                int? ctIndexQ = currencyTab.IndexInParent;
                if (ctIndexQ is null)
                {
                    return new Entity[0];
                }

                int ctIndex = ctIndexQ.Value;
                if (stash.IndexVisibleStash != ctIndex)
                {
                    SharpDX.Vector2 buttonpos = currencyTab.GetClientRect().Center;
                    Mouse.LinearSmoothMove(buttonpos);
                    Mouse.LeftClick(200);
                }
                Inventory visibleStashInv = stash.VisibleStash;
                ServerInventory se = visibleStashInv.ServerInventory;
                IList<Entity> toReturn = se.Items;

                return toReturn;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return new Entity[0];
        }
        public IList<Entity> GetStashItemFromMD(string metadataToFind)
        {
            try
            {
                IEnumerable<Entity> stashContents = GetContentsOfStashTab(currencyStashIndex);
                if (stashContents is not null && stashContents.Any())
                {
                    return stashContents.Where(x => (bool)(x.Metadata?.Contains(metadataToFind))).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return new List<Entity>();
        }
        public IList<Entity> GetScoursFromInv()
        {
            return GetStashItemFromMD(ItemCodes.ScourCode);
        }
        public IList<Entity> GetAlchsFromInv()
        {
            return GetStashItemFromMD(ItemCodes.AlchCode);
        }
        public IList<Entity> GetTransmutesFromInv()
        {
            return GetStashItemFromMD(ItemCodes.TransmuteCode);
        }
        public IList<Entity> GetAugsFromInv()
        {
            return GetStashItemFromMD(ItemCodes.AugCode);
        }
        public IList<Entity> GetAltsFromInv()
        {
            return GetStashItemFromMD(ItemCodes.AltCode);
        }
        public IList<Entity> GetWisFromInv()
        {
            return GetStashItemFromMD(ItemCodes.WisdomCode);
        }
    }
}
