using BepInEx;
using System.IO;
using System;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using static MonoMod.InlineRT.MonoModRule;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace TestMod;

[BepInPlugin("bro.HaltRain", "HaltRain", "0.1.0")]
sealed class Plugin : BaseUnityPlugin
{
    bool init;

    public void OnEnable()
    {
        // Add hooks here
        On.RainCycle.Update += RainCycle_Update;
        On.Region.ctor += Region_ctor;
    }

    private void Region_ctor(On.Region.orig_ctor orig, Region self, string name, int firstRoomIndex, int regionNumber, SlugcatStats.Name storyIndex)
    {
        orig(self, name, firstRoomIndex, regionNumber, storyIndex);
        string text = "";
        if (storyIndex != null)
        {
            text = "-" + storyIndex.value;
        }
        string path = AssetManager.ResolveFilePath(string.Concat(new string[]
    {
        "World",
        Path.DirectorySeparatorChar.ToString(),
        name,
        Path.DirectorySeparatorChar.ToString(),
        "properties",
        text,
        ".txt"
    }));
        if (text != "" && !File.Exists(path))
        {
            path = AssetManager.ResolveFilePath(string.Concat(new string[]
            {
            "World",
            Path.DirectorySeparatorChar.ToString(),
                name,
            Path.DirectorySeparatorChar.ToString(),
            "properties.txt"
            }));
        }

        if (File.Exists(path))
        {
            foreach (string str in File.ReadAllLines(path))
            {
                string[] array2 = Regex.Split(RWCustom.Custom.ValidateSpacedDelimiter(str, ":"), ": ");
                if (array2.Length >= 2 && array2[0] == "PauseTimerAt")
                {
                    PausePoint(self).Value = float.Parse(array2[1]);
                }
            }
        }
    }

    private static readonly ConditionalWeakTable<Region, StrongBox<float>> table = new();

    public static StrongBox<float> PausePoint(Region p) => table.GetValue(p, _ => new StrongBox<float>(-1f));

    private void RainCycle_Update(On.RainCycle.orig_Update orig, RainCycle self)
    {
        orig(self);
        if (!self.world.game.IsStorySession || self.world.region == null) return;
        int pausePoint = (int)(self.cycleLength * PausePoint(self.world.region).Value);
        if (PausePoint(self.world.region).Value == -1f || pausePoint > self.timer) return;

        self.pause = 10;
    }
}
