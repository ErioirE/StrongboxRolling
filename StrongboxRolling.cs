using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.FilesInMemory;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ImGuiNET;
using Newtonsoft.Json;
using Random_Features.Libs;
using SharpDX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;
using Color = SharpDX.Color;
using Input = ExileCore.Input;
using RectangleF = SharpDX.RectangleF;
using Vector2 = SharpDX.Vector2;

namespace StrongboxRolling
{
    public class StrongboxRolling : BaseSettingsPlugin<StrongboxRollingSettings>
    {
        private const string StrongboxRollingRuleDirectory = "StrongboxRolling Rules";
        private readonly List<Entity> _entities = new List<Entity>();
        private readonly Stopwatch _pickUpTimer = Stopwatch.StartNew();
        private readonly Stopwatch DebugTimer = Stopwatch.StartNew();
        private readonly WaitTime toPick = new WaitTime(1);
        private readonly WaitTime wait1ms = new WaitTime(1);
        private readonly WaitTime wait2ms = new WaitTime(2);
        private readonly WaitTime wait3ms = new WaitTime(3);
        private readonly WaitTime wait100ms = new WaitTime(100);
        private readonly WaitTime waitForNextTry = new WaitTime(1);
        private Vector2 _clickWindowOffset;
        private HashSet<string> _magicRules;
        private HashSet<string> _normalRules;
        private HashSet<string> _rareRules;
        private HashSet<string> _uniqueRules;
        private HashSet<string> _ignoreRules;
        private Dictionary<string, int> _weightsRules = new Dictionary<string, int>();
        private WaitTime _workCoroutine;
        public DateTime buildDate;
        private uint coroutineCounter;
        private Vector2 cursorBeforePickIt;
        private bool FullWork = true;
        private Element LastLabelClick;
        public string MagicRuleFile;
        private WaitTime mainWorkCoroutine = new WaitTime(5);
        public string NormalRuleFile;
        private Coroutine pickItCoroutine;
        public string RareRuleFile;
        private WaitTime tryToPick = new WaitTime(7);
        public string UniqueRuleFile;
        private WaitTime waitPlayerMove = new WaitTime(10);
        private List<string> _customItems = new List<string>();
        public int[,] inventorySlots { get; set; } = new int[0, 0];
        public ServerInventory InventoryItems { get; set; }
        public static StrongboxRolling Controller { get; set; }


        public FRSetManagerPublishInformation FullRareSetManagerData = new FRSetManagerPublishInformation();

        public StrongboxRolling()
        {
            Name = "StrongboxRolling";
        }

        public string PluginVersion { get; set; }
        private List<string> PickitFiles { get; set; }

        public override bool Initialise()
        {
            Controller = this;
            pickItCoroutine = new Coroutine(MainWorkCoroutine(), this, "StrongboxRolling");
            Core.ParallelRunner.Run(pickItCoroutine);
            pickItCoroutine.Pause();
            DebugTimer.Reset();

            _workCoroutine = new WaitTime(Settings.ExtraDelay);

            //LoadCustomItems();
            return true;
        }



        private IEnumerator MainWorkCoroutine()
        {
            while (true)
            {
                yield return FindSBToFix();

                coroutineCounter++;
                pickItCoroutine.UpdateTicks(coroutineCounter);
                yield return _workCoroutine;
            }
        }

        public override void DrawSettings()
        {
            Settings.CraftBoxKey = ImGuiExtension.HotkeySelector("Craft box Key: " + Settings.CraftBoxKey.Value.ToString(), Settings.CraftBoxKey);
            Settings.CancelKey = ImGuiExtension.HotkeySelector("Key to cancel rolling: " + Settings.CancelKey.Value.ToString(), Settings.CancelKey);
            Settings.BoxCraftingUseAltsAugs.Value = ImGuiExtension.Checkbox("Roll strongboxes using Transmuts, Alts & Augs instead of Scour/Alch", Settings.BoxCraftingUseAltsAugs);
            Settings.BoxCraftingMidStepDelay.Value = ImGuiExtension.IntSlider("Box Crafting Mid-Step delay (wait for hover registration)", Settings.BoxCraftingMidStepDelay);
            Settings.BoxCraftingStepDelay.Value = ImGuiExtension.IntSlider("Box crafting step delay (between crafts)", Settings.BoxCraftingStepDelay);
            Settings.ModsRegex = ImGuiExtension.InputText("RegEx for mod text, I.E. 'Guarded by \\d rare monsters'. Not case sensitive", Settings.ModsRegex, 1024, ImGuiInputTextFlags.None);
            //Settings.OverrideItemPickup.Value = ImGuiExtension.Checkbox("Item Pickup Override", Settings.OverrideItemPickup); ImGui.SameLine();
            //ImGuiExtension.ToolTip("Override item.CanPickup\n\rDO NOT enable this unless you know what you're doing!");
            
        }


        private DateTime DisableLazyLootingTill { get; set; }

        public override Job Tick()
        {
            List<string> toLog = new();
            if (Settings.BoxCraftingUseAltsAugs && !GetTransmutesFromInv().Any())
            {
                toLog.Add("Trying to craft but no Orbs of Transmutation found in inventory.");

            }
            if (Settings.BoxCraftingUseAltsAugs && !GetAltsFromInv().Any())
            {
                toLog.Add("Trying to craft but no Orbs of Alteration found in inventory.");
            }
            if (Settings.BoxCraftingUseAltsAugs && !GetAugsFromInv().Any())
            {
                toLog.Add("Trying to craft but no Orbs of Augmentation found in inventory.");
            }
            if (!GetScoursFromInv().Any())
            {
                toLog.Add("Trying to craft but no Orbs of Scouring found in inventory.");                
            }
            if (!Settings.BoxCraftingUseAltsAugs && !GetAlchsFromInv().Any())
            {
                toLog.Add("Trying to craft but no Orbs of Alchemy found in inventory.");
            }
            for(int i = 0; i<toLog.Count;i++)
            {
                DrawText(toLog[i],i*20);
            }
            InventoryItems = GameController.Game.IngameState.ServerData.PlayerInventories[0].Inventory;
            inventorySlots = Misc.GetContainer2DArray(InventoryItems);

            if (Input.GetKeyState(Settings.LazyLootingPauseKey)) DisableLazyLootingTill = DateTime.Now.AddSeconds(2);
            if (Input.GetKeyState(Keys.Escape)) 
            {
                FullWork = true;
                pickItCoroutine.Pause(); 
            }

            if (Input.GetKeyState(Settings.CraftBoxKey.Value))
            {
                DebugTimer.Restart();

                if (pickItCoroutine.IsDone)
                {
                    var firstOrDefault = Core.ParallelRunner.Coroutines.FirstOrDefault(x => x.OwnerName == nameof(StrongboxRolling));

                    if (firstOrDefault != null)
                        pickItCoroutine = firstOrDefault;
                }

                pickItCoroutine.Resume();
                FullWork = false;
            }
            else
            {
                if (FullWork)
                {
                    pickItCoroutine.Pause();
                    DebugTimer.Reset();
                }
            }

            if (DebugTimer.ElapsedMilliseconds > 300)
            {
                //FullWork = true;
                //LogMessage("Error pick it stop after time limit 300 ms", 1);
                DebugTimer.Reset();
            }
            //Graphics.DrawText($@"PICKIT :: Debug Tick Timer ({DebugTimer.ElapsedMilliseconds}ms)", new Vector2(100, 100), FontAlign.Left);
            //DebugTimer.Reset();

            return null;
        }


        public void DrawText(string text, int offset)
        {
            Graphics.DrawTextWithBackground(text,new(50,100+offset),Color.Crimson,Color.Black);
        }
        //TODO: Make function pretty





        public override void ReceiveEvent(string eventId, object args)
        {
            if (!Settings.Enable.Value) return;

            if (eventId == "frsm_display_data")
            {

                var argSerialised = JsonConvert.SerializeObject(args);
                FullRareSetManagerData = JsonConvert.DeserializeObject<FRSetManagerPublishInformation>(argSerialised);
            }
        }
        private IEnumerator FindSBToFix()
        {
            if (!GameController.Window.IsForeground()) yield break;
            var window = GameController.Window.GetWindowRectangleTimeCache;
            var rect = new RectangleF(window.X, window.X, window.X + window.Width, window.Y + window.Height);
            var playerPos = GameController.Player.GridPos;
            var items = InventoryItems;

            List<CustomItem> currentLabels;
            var morphPath = "Metadata/MiscellaneousObjects/Metamorphosis/MetamorphosisMonsterMarker";
            List<string> labelsToLog = new();

           
            
           



            if (!FullWork)
            {
                
                yield return TryToCraft(GetClosestChest());
                //FullWork = true;
            }
        }
        public LabelOnGround GetClosestChest()
        {
            IList<ExileCore.PoEMemory.Elements.LabelOnGround> otherLabels = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabelsVisible;
            return otherLabels.Where(x => x.ItemOnGround.Metadata.Contains("Metadata/Chests/StrongBoxes") && !x.ItemOnGround.IsOpened && x.ItemOnGround.DistancePlayer < 70).OrderBy(x => x.ItemOnGround.DistancePlayer).MinBy(x => x.ItemOnGround.DistancePlayer);
        }
        private ServerInventory.InventSlotItem[] GetInvWithMD(string metadataToFind)
        {
            try
            {
                return InventoryItems.InventorySlotItems.Where(x => x.Item.Metadata.Contains(metadataToFind)).OrderBy(x=> x.PosX).ThenBy(x=> x.PosY).ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return Array.Empty<InventSlotItem>();
        }
        private InventSlotItem[] GetScoursFromInv()
        {
            return GetInvWithMD(ScourCode);
        }
        private InventSlotItem[] GetAlchsFromInv()
        {
            return GetInvWithMD(AlchCode);
        }
        private InventSlotItem[] GetTransmutesFromInv()
        {
            return GetInvWithMD(TransmuteCode);
        }
        private InventSlotItem[] GetAugsFromInv()
        {
            return GetInvWithMD(AugCode);
        }
        private InventSlotItem[] GetAltsFromInv()
        {
            return GetInvWithMD(AltCode);
        }
        private InventSlotItem[] GetWisFromInv()
        {
            return GetInvWithMD(WisdomCode);
        }


        private static readonly string ScourCode = @"Metadata/Items/Currency/CurrencyConvertToNormal";
        private static readonly string AlchCode = @"Metadata/Items/Currency/CurrencyUpgradeToRare";
        private static readonly string EngineerCode = @"Metadata/Items/Currency/CurrencyStrongboxQuality";
        private static readonly string ChaosCode = @"Metadata/Items/Currency/CurrencyRerollRare";
        private static readonly string AltCode = @"Metadata/Items/Currency/CurrencyRerollMagic";
        private static readonly string TransmuteCode = @"Metadata/Items/Currency/CurrencyUpgradeToMagic";
        private static readonly string AugCode = @"Metadata/Items/Currency/CurrencyAddModToMagic";
        private static readonly string WisdomCode = @"Metadata/Items/Currency/CurrencyIdentification";
        /// <summary>
        /// LazyLoot item independent checks
        /// </summary>
        /// <returns></returns>


        /// <summary>
        /// LazyLoot item dependent checks
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool ShouldLazyLoot(CustomItem item)
        {
            var itemPos = item.LabelOnGround.ItemOnGround.Pos;
            var playerPos = GameController.Player.Pos;
            if (Math.Abs(itemPos.Z - playerPos.Z) > 50) return false;
            var dx = itemPos.X - playerPos.X;
            var dy = itemPos.Y - playerPos.Y;
            if (dx * dx + dy * dy > 275 * 275) return false;

            if (item.IsElder || item.IsFractured || item.IsShaper ||
                item.IsHunter || item.IsCrusader || item.IsRedeemer || item.IsWarlord || item.IsHeist)
                return true;

            if (item.Rarity == ItemRarity.Rare && item.Width * item.Height > 1) return false;

            return true;
        }

        private IEnumerator TryToPickV2(CustomItem pickItItem)
        {
            if (!pickItItem.IsValid)
            {
                FullWork = true;
                //LogMessage("PickItem is not valid.", 5, Color.Red);
                yield break;
            }

            var centerOfItemLabel = pickItItem.LabelOnGround.Label.GetClientRectCache.Center;
            var rectangleOfGameWindow = GameController.Window.GetWindowRectangleTimeCache;

            var oldMousePosition = Mouse.GetCursorPositionVector();
            _clickWindowOffset = rectangleOfGameWindow.TopLeft;
            rectangleOfGameWindow.Inflate(-36, -36);
            centerOfItemLabel.X += rectangleOfGameWindow.Left;
            centerOfItemLabel.Y += rectangleOfGameWindow.Top;
            if (!rectangleOfGameWindow.Intersects(new RectangleF(centerOfItemLabel.X, centerOfItemLabel.Y, 3, 3)))
            {
                FullWork = true;
                //LogMessage($"Label outside game window. Label: {centerOfItemLabel} Window: {rectangleOfGameWindow}", 5, Color.Red);
                yield break;
            }

            var tryCount = 0;

            while (tryCount < 3)
            {
                var completeItemLabel = pickItItem.LabelOnGround?.Label;

                if (completeItemLabel == null)
                {
                    if (tryCount > 0)
                    {
                        //LogMessage("Probably item already picked.", 3);
                        yield break;
                    }

                    //LogError("Label for item not found.", 5);
                    yield break;
                }

                //while (GameController.Player.GetComponent<Actor>().isMoving)
                //{
                //    yield return waitPlayerMove;
                //}
                var clientRect = completeItemLabel.GetClientRect();

                var clientRectCenter = clientRect.Center;

                var vector2 = clientRectCenter + _clickWindowOffset;

                if (!rectangleOfGameWindow.Intersects(new RectangleF(vector2.X, vector2.Y, 3, 3)))
                {
                    FullWork = true;
                    //LogMessage($"x,y outside game window. Label: {centerOfItemLabel} Window: {rectangleOfGameWindow}", 5, Color.Red);
                    yield break;
                }

                Mouse.MoveCursorToPosition(vector2);
                yield return wait2ms;

                if (pickItItem.IsTargeted())
                    yield return Mouse.LeftClick();

                yield return toPick;
                tryCount++;
            }

            tryCount = 0;

            while (GameController.Game.IngameState.IngameUi.ItemsOnGroundLabelsVisible.FirstOrDefault(
                       x => x.Address == pickItItem.LabelOnGround.Address) != null && tryCount < 6)
            {
                tryCount++;
                //yield return waitForNextTry;
            }

            //yield return waitForNextTry;

            //   Mouse.MoveCursorToPosition(oldMousePosition);
        }
        private string[] prevMods = Array.Empty<string>();
        public Regex goodMods;
        private IEnumerator TryToCraft(LabelOnGround sbLabel)
        {

            if (sbLabel is null)
            {
                FullWork = true;
                yield break;
            }

            
            //Element label = pickItItem.Label;
            //Entity labelEntity = label.Entity;
            //pickItItem.ItemOnGround.TryGetComponent<Render>(out Render renderC);
            //pickItItem.ItemOnGround.TryGetComponent<StateMachine>(out StateMachine stateC);
            //pickItItem.ItemOnGround.TryGetComponent<Stats>(out Stats statsC);
            //pickItItem.ItemOnGround.TryGetComponent<Chest>(out Chest chestC);

            //string objectDump = chestC.DumpObject();


            //if (Settings.BoxCraftingStream)
            //{
            //    goodMods = new(@"uousMo|boxSca|iscSca|gerSca|nceSca|nationSca|stSca|tumSca|phySca|(ummonrares.*ummonmagic)|(ummonmagic.*ummonrares)", RegexOptions.IgnoreCase);   
            //}
            //else
            //{
            //    goodMods = new(@"uousMo|ummonrares|boxSca|iscSca|gerSca|nceSca|nationSca|stSca|tumSca|phySca", RegexOptions.IgnoreCase);
            //}
            

            
            

            string[] labelsBefore = FindAllLabels(sbLabel);
            sbLabel.ItemOnGround.TryGetComponent<ObjectMagicProperties>(out ObjectMagicProperties magicPropsC);
            if (magicPropsC is null)
            {
                yield return wait2ms;
            }
            if(CheckMods())
            {
                yield break;
            }
           
            if (magicPropsC.Mods.Count() > 0 && !Settings.BoxCraftingUseAltsAugs)
            {
                if (GetScoursFromInv().Any() && GetAlchsFromInv().Any())
                {
                    CraftWithItem(GetScoursFromInv().First(), sbLabel);
                    
                    //CraftWithItem(GetAlchsFromInv().First(), pickItItem);
                }
            }
            else if (magicPropsC.Mods.Count() > 0 && Settings.BoxCraftingUseAltsAugs)
            {
                if (magicPropsC.Mods.Count() > 2 && GetScoursFromInv().Any())
                {
                    CraftWithItem(GetScoursFromInv().First(), sbLabel);
                }
                else if (magicPropsC.Mods.Count() == 1 && Settings.BoxCraftingUseAltsAugs && FindAllLabels(sbLabel).Where(x => x.ToLower().Contains("suffix")).Any())
                {
                    if (GetAugsFromInv().Any())
                    {
                        prevMods = FindAllLabels(sbLabel);
                        CraftWithItem(GetAugsFromInv().First(), sbLabel);
                        if (!WaitForChange(prevMods))
                        {
                            yield break;
                        }
                        
                    }
                }
                else if (GetAltsFromInv().Any())
                {
                    prevMods = FindAllLabels(sbLabel);
                    CraftWithItem(GetAltsFromInv().First(), sbLabel);
                    if (!WaitForChange(prevMods))
                    {
                        yield break;
                    }
                    
                }
            }
            else if (magicPropsC.Mods.Count() < 1)
            {
                if (GetWisFromInv().Any() && labelsBefore.Where(x => x.ToLower().Contains("unidentified")).Any())
                {
                    prevMods = FindAllLabels(sbLabel);
                    CraftWithItem(GetWisFromInv().First(), sbLabel);
                    
                    //continue;
                    //CraftWithItem(GetWisFromInv().First(), pickItItem);
                }
                else if (Settings.BoxCraftingUseAltsAugs && GetTransmutesFromInv().Any())
                {
                    prevMods = FindAllLabels(sbLabel);
                    CraftWithItem(GetTransmutesFromInv().First(), sbLabel);
                    if (!WaitForChange(prevMods))
                    {
                        yield break;
                    }
                    sbLabel = GetClosestChest();
                }
                else if (!Settings.BoxCraftingUseAltsAugs && GetAlchsFromInv().Any())
                {
                    prevMods = FindAllLabels(sbLabel);
                    CraftWithItem(GetAlchsFromInv().First(), sbLabel);
                    if (!WaitForChange(prevMods))
                    {
                        yield break;
                    }
                    
                }
                else
                {
                    yield return wait2ms;
                }
            }







            //else if (chestC.Rarity == MonsterRarity.White)
            //{

            //}
            //else if (chestC.Rarity == MonsterRarity.Magic)
            //{

            //}

            //if (!pickItItem.Label.Entity.IsValid)
            //{
            //    if (GetWisFromInv().Any())
            //    {
            //        CraftWithItem(GetWisFromInv().First(),pickItItem);
            //    }
            //    else
            //    {
            //        yield break;
            //    }
            //}



            var centerOfItemLabel = sbLabel.Label.GetClientRectCache.Center;
            var rectangleOfGameWindow = GameController.Window.GetWindowRectangleTimeCache;

            var oldMousePosition = Mouse.GetCursorPositionVector();
            _clickWindowOffset = rectangleOfGameWindow.TopLeft;
            rectangleOfGameWindow.Inflate(-36, -36);
            centerOfItemLabel.X += rectangleOfGameWindow.Left;
            centerOfItemLabel.Y += rectangleOfGameWindow.Top;
            if (!rectangleOfGameWindow.Intersects(new RectangleF(centerOfItemLabel.X, centerOfItemLabel.Y, 3, 3)))
            {
                //FullWork = true;
                //LogMessage($"Label outside game window. Label: {centerOfItemLabel} Window: {rectangleOfGameWindow}", 5, Color.Red);
                yield break;
            }

            var tryCount = 0;

                var completeItemLabel = sbLabel?.Label;

                if (completeItemLabel == null)
                {
                    if (tryCount > 0)
                    {
                        //LogMessage("Probably item already picked.", 3);
                        yield break;
                    }

                    //LogError("Label for item not found.", 5);
                    yield break;
                }

                //while (GameController.Player.GetComponent<Actor>().isMoving)
                //{
                //    yield return waitPlayerMove;
                //}
                var clientRect = completeItemLabel.GetClientRect();

                var clientRectCenter = clientRect.Center;

                var vector2 = clientRectCenter + _clickWindowOffset;

                if (!rectangleOfGameWindow.Intersects(new RectangleF(vector2.X, vector2.Y, 3, 3)))
                {
                    FullWork = true;
                    //LogMessage($"x,y outside game window. Label: {centerOfItemLabel} Window: {rectangleOfGameWindow}", 5, Color.Red);
                    yield break;
                }




            

            //yield return waitForNextTry;

            //   Mouse.MoveCursorToPosition(oldMousePosition);
        }
        public bool LabelsChanged(string[] before, string[] after)
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
        
        public string[] FindAllLabels(LabelOnGround L)
        {
            Element label = L.Label;
            return FindAllLabels(label);
        }
       
        public string[] FindAllLabels(Element L)
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
        private bool WaitForChange(string[] labelsBefore)
        {
            int maxWait = 200;
            int totalWait = 0;
            string[] labels = FindAllLabels(GetClosestChest());
            while (!LabelsChanged(labelsBefore, labels) && totalWait < maxWait)
            {
                int delay = 10;
                Task.Delay(delay).Wait();
                totalWait += delay;
                labels = FindAllLabels(GetClosestChest());
            }
            if (totalWait >= maxWait)
            {
                return false;
            }
            string allMods = string.Join("\r\n", FindAllLabels(GetClosestChest()));
            Regex goodMods = new Regex(@$"{Settings.ModsRegex}", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
            if (goodMods.IsMatch(allMods))
            {
                File.AppendAllText("./LabelLog.txt", allMods);
                pickItCoroutine.Pause();
                FullWork = true;
            }
            return true;
        }
        private void CraftWithItem(InventSlotItem e, LabelOnGround toCraft)
        {
            if (FullWork)
            {
                return;
            }
            toCraft = GetClosestChest();
            string[] labels = FindAllLabels(toCraft);
            List<string> toLog = new();

            toLog.Add(@$"{DateTime.Now.ToString("yyyy-mm-dd_T")}");
            toLog.Add(@$"{e.Item.RenderName}");
            toLog.AddRange(labels);
            string allMods = string.Join(" ", toLog);
            if (allMods.ToLower().Contains("stream") || allMods.ToLower().Contains("3 rare"))
            {

            }
            File.AppendAllLines(@"./craftingLog.txt",toLog);
            if (CheckMods(toLog))
            {
                return;
            }
            
            if (!IngameState.pTheGame.IngameState.IngameUi.InventoryPanel.IsVisibleLocal)
            {
                SendKeys.SendWait("i");

                Task.Delay(Settings.BoxCraftingMidStepDelay).Wait();
            }

            Mouse.MoveCursorToPosition(GetPos(e));
            Task.Delay(Settings.BoxCraftingMidStepDelay).Wait();
            if (!WaitForMouseIcon(MouseActionType.Free))
            {
                return;
            }
            Mouse.RightClick(Settings.BoxCraftingMidStepDelay);
            if (!WaitForMouseIcon(MouseActionType.UseItem))
            {
                return;
            }

            Task.Delay(Settings.BoxCraftingMidStepDelay).Wait();
            //Mouse.SetCursorPos(GetPos(toCraft));
            Mouse.LinearSmoothMove(GetPos(toCraft));
            bool? isTargeted = toCraft.ItemOnGround.GetComponent<Targetable>()?.isTargeted;
            int limit = 200;
            int i = 0;
            while (isTargeted is null || !isTargeted.Value)
            {
                Task.Delay(1).Wait();
                if (i >= limit)
                {
                    return;
                }
                i++;
                isTargeted = toCraft.ItemOnGround.GetComponent<Targetable>()?.isTargeted;
            }
            if (isTargeted is not null && isTargeted.Value)
            {
                Task.Delay(Settings.BoxCraftingMidStepDelay).Wait();
                Mouse.LeftClick(Settings.BoxCraftingMidStepDelay);
                WaitForMouseIcon(MouseActionType.Free);
            }
            else
            {

            }

            Task.Delay(Settings.BoxCraftingStepDelay).Wait();
            Task.Delay(100).Wait();

        }
        private bool WaitForMouseIcon(MouseActionType mat)
        {
            ExileCore.PoEMemory.MemoryObjects.Cursor cursor = GameController.IngameState.IngameUi.Cursor;

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
        private Vector2 GetPos(InventSlotItem l)
        {
            Vector2 centerOfItemLabel = l.GetClientRect().TopLeft;
            RectangleF rectangleOfGameWindow = GameController.Window.GetWindowRectangleTimeCache;

            var oldMousePosition = Mouse.GetCursorPositionVector();
            _clickWindowOffset = rectangleOfGameWindow.TopLeft;
            rectangleOfGameWindow.Inflate(-36, -36);
            centerOfItemLabel.X += rectangleOfGameWindow.Left;
            centerOfItemLabel.Y += rectangleOfGameWindow.Top;
            return centerOfItemLabel;
        }
        private Vector2 GetPos(LabelOnGround l)
        {
            Vector2 botLeftOfLabel = l.Label.GetClientRect().BottomLeft;
            Vector2 centerOfLabel = l.Label.GetClientRect().Center;
            RectangleF rectangleOfGameWindow = GameController.Window.GetWindowRectangleTimeCache;

            var oldMousePosition = Mouse.GetCursorPositionVector();
            _clickWindowOffset = rectangleOfGameWindow.TopLeft;
            rectangleOfGameWindow.Inflate(-36, -36);
            botLeftOfLabel.X += rectangleOfGameWindow.Left;
            botLeftOfLabel.Y += rectangleOfGameWindow.Top;
            float prevX = botLeftOfLabel.X;
            float prevY = botLeftOfLabel.Y;

            return botLeftOfLabel with { X = centerOfLabel.X + 10, Y = prevY - 60 };
        }
        public static Regex Weird = new(@"[^A-Za-z0-9\ ]");
        private bool CheckMods()
        {
            goodMods = new Regex(@$"{Settings.ModsRegex}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            string allMods = string.Join(" ", FindAllLabels(GetClosestChest()));
            allMods = Weird.Replace(allMods,"").ToLower();
            if (allMods.Contains("stream") || allMods.Contains("3 rare"))
            {

            }
            if (goodMods.IsMatch(allMods.ToLower()))
            {
                File.AppendAllText("./LabelLog.txt", allMods);
                pickItCoroutine.Pause();
                FullWork = true;
                return true;
            }
            return false;
        }
        private bool CheckMods(IEnumerable<string> labels)
        {

            goodMods = new Regex(@$"{Settings.ModsRegex}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            string allMods = string.Join(" ", labels);
            allMods = Weird.Replace(allMods, "").ToLower();
            if (allMods.Contains("stream")|| allMods.Contains("3 rare"))
            {

            }
            if (goodMods.IsMatch(allMods.ToLower()))
            {
                File.AppendAllText("./LabelLog.txt", allMods);
                pickItCoroutine.Pause();
                FullWork = true;
                return true;
            }
            return false;
        }


        public HashSet<string> LoadPickit(string fileName)
        {
            var hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (fileName == string.Empty)
            {
                return hashSet;
            }

            var pickitFile = $@"{DirectoryFullName}\{StrongboxRollingRuleDirectory}\{fileName}.txt";

            if (!File.Exists(pickitFile))
            {
                return hashSet;
            }

            var lines = File.ReadAllLines(pickitFile);

            foreach (var x in lines.Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#")))
            {
                hashSet.Add(x.Trim());
            }

            LogMessage($"PICKIT :: (Re)Loaded {fileName}", 5, Color.GreenYellow);
            return hashSet;
        }



        public override void OnPluginDestroyForHotReload()
        {
            pickItCoroutine.Done(true);
        }



        #region Adding / Removing Entities

        public override void EntityAdded(Entity Entity)
        {
        }

        public override void EntityRemoved(Entity Entity)
        {
        }

        #endregion
    }
}
