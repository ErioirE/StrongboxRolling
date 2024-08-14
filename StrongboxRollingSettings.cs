using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using System.Windows.Forms;

namespace StrongboxRolling
{
    public class StrongboxRollingSettings : ISettings
    {
        public static readonly string defaultRegex = @"(cart|ambush|harbin|harvest|divination|domination|horned|misc|essence).*scarab|stream";
        public StrongboxRollingSettings()
        {
            Enable = new ToggleNode(false);
            CraftBoxKey = Keys.F1;
            ExtraDelay = new RangeNode<int>(0, 0, 200);

            CancelKey = Keys.Escape;
            BoxCraftingUseAltsAugs = new ToggleNode(true);
            BoxCraftingMidStepDelay = new RangeNode<int>(40, 0, 200);
            BoxCraftingStepDelay = new RangeNode<int>(0, 0, 400);
            ModsRegex = defaultRegex;

        }

        public ToggleNode Enable { get; set; }
        public HotkeyNode CraftBoxKey { get; set; }
        public RangeNode<int> ExtraDelay { get; set; }

        public HotkeyNode CancelKey { get; set; }
        public ToggleNode BoxCraftingUseAltsAugs { get; set; } = new ToggleNode(false);
        public RangeNode<int> BoxCraftingMidStepDelay { get; set; } = new RangeNode<int>(0, 0, 200);
        public RangeNode<int> BoxCraftingStepDelay { get; set; } = new RangeNode<int>(0, 0, 200);
        public string ModsRegex { get; set; }

        public HotkeyNode LazyLootingPauseKey { get; set; } = new HotkeyNode(Keys.Space);



    }
}
