using System.Reflection;

namespace Sounds
{
    public static class Debug
    {
        public static string Sound1 => "event:/SFX/Text/general";
        public static string Sound2 => "event:/SFX/Interactions/inventory_pickup";
        public static string MetroHi => "event:/SFX/Debug/MetronomeHigh";
        public static string MetroLo => "event:/SFX/Debug/MetronomeLow";

    }

    public static class Player
    {
        public static string CollectItem => "event:/SFX/Interactions/inventory_pickup";
        public static string CollectSprite => "event:/SFX/Interactions/lantern_collect";
        public static string GiveItem => "event:/SFX/Interactions/inventory_leave";
    }

    public static class Walking
    {
        public static int Grass_B => 0; 
        public static int Soil_Rocks => 1; 
        public static int Soil_RoughStone => 1;
        public static int Sand => 2;
        public static int Sand2 => 2;
        public static int Muddy => 3;
        public static int Rock => 4; 
        public static int Water => 8;
        public static int SoulSand => 6;
        public static int SoulSand2 => 6;
        public static int ThroneWater => 7;
        public static int PierWood => 9;
    }

    public static class Terrain
    {
        public static int Grass => 0; 
        public static int ShortGrass => 0; 
        public static int Wheat => 1; 
        public static int bluebells => 0;
        public static int bluebells_2 => 0;
        public static int bluebells_3 => 0;
    }

    namespace UI
    {
        public static class MemoryView
        {
            public static string Open => "event:/SFX/UI/memory_menu_open";
            public static string Close => "event:/SFX/UI/memory_menu_close";
        }

        public static class Dialogue
        {
            public static string ChallengeAppear => Debug.Sound2; // TODO
            public static string ChallengeRemind => Debug.Sound1; // TODO
        }

        public static class Text
        {
            public static string Typewriter => "event:/SFX/Text/general";
        }
    }
}
