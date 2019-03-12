#if DEBUG

using System;
using System.Threading.Tasks;

namespace FtdModManager
{
    public static class Test
    {
        public static async Task MainAsync(string[] args)
        {
            var pref = new ModPreferences
            {
                modData = ModData.FromGithubRepo("why7090", "buildingtools", "master", ModPreferences.UpdateType.LatestCommit),
                updateType = ModPreferences.UpdateType.LatestCommit,
                currentTreeUrl = "",
                basePath = @"C:\Users\user\Documents\From The Depths\Mods\BT"
            };
            Console.WriteLine(await pref.CheckUpdate());
            await pref.Update();
        }

        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
            Console.ReadLine();
        }
    }
}

#endif