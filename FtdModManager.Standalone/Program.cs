using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FtdModManager.Standalone
{
    internal static class Program
    {
        const string modRelativePath = "From The Depths/Mods";
        const string ownManifestUri = "https://raw.githubusercontent.com/Why7090/FtdModManager/master/modmanifest.json";

        static string modParentPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), modRelativePath);
        static Arguments args;

        static bool auto = false;

        static void Main(string[] arguments)
        {
            args = new Arguments(arguments);

            Run();
        }

        static void Run()
        {
            if (args.Count == 0) // self-update
            {
                string installPath = Path.Combine(modParentPath, "FtdModManager").NormalizedDirPath();

                InstallMod(ownManifestUri, installPath);

                Console.WriteLine("FtdModManager successfully installed to " + installPath);
                Console.Write("Press any key to exit: ");
                Console.ReadKey();
                return;
            }

            if (args.IsTrue("help") || args.IsTrue("h"))
            {
                PrintHelp();
                return;
            }

            if (args.IsTrue("y"))
                auto = true;

            if (args.Exists("parent"))
                modParentPath = args.Single("parent");
            Directory.SetCurrentDirectory(modParentPath);

            if (args.Exists("install"))
            {
                InstallMod(args.Single("install"), args.Single("name"));
            }

            if (args.Exists("update-all"))
            {
                foreach(string file in Directory.GetFiles(modParentPath, ModPreferences.manifestFileName, SearchOption.AllDirectories))
                {
                    UpdateMod(Path.GetDirectoryName(file));
                }
            }
        }

        static void InstallMod(string manifestUri, string installFolder)
        {
            InstallModAsync(manifestUri, installFolder).Wait();
        }

        static async Task InstallModAsync(string manifestUri, string installFolder)
        {
            if (!File.Exists(Path.Combine(installFolder, ModPreferences.manifestFileName)))
            {
                Helper.Log("Downloading manifest...");
                await Helper.DownloadToFileAsync(manifestUri, Path.Combine(installFolder, ModPreferences.manifestFileName));
                Helper.Log("Downloaded manifest file");
            }
            await UpdateModAsync(installFolder);
        }

        static void UpdateMod(string basePath)
        {
            UpdateModAsync(basePath).Wait();
        }

        static async Task UpdateModAsync(string basePath)
        {
            var updateInfo = new StandaloneModUpdateInfo(basePath);
            await updateInfo.CheckAndPrepareUpdate();

            Helper.Log("\n");
            Helper.LogSeparator();
            Helper.Log(updateInfo.GetConfirmationMessage());
            Helper.LogSeparator();
            Helper.Log(updateInfo.GetConfirmationTitle());
            Helper.LogSeparator();
            Helper.Log("\n");

            if (!updateInfo.isUpdateAvailable)
                return;

            if (!(auto || Confirm("Do you want to install this mod? [Y/n] : ", true)))
                return;

            Console.WriteLine();

            await updateInfo.ApplyUpdate();

            Helper.Log("\n");
            Helper.LogSeparator();
            Helper.Log(updateInfo.GetFinishMessage());
            Helper.LogSeparator();
            Helper.Log(updateInfo.GetFinishTitle());
            Helper.LogSeparator();
            Helper.Log("\n");
        }

        static void PrintHelp()
        {
            Helper.Log("Help");
        }

        static bool Confirm(string question, bool? defaultBehaviour = null)
        {
            Console.Write(question);
            while (true)
            {
                switch (Console.ReadKey(false).Key)
                {
                    case ConsoleKey.Y:
                        return true;
                    case ConsoleKey.N:
                        return false;
                    case ConsoleKey.Enter:
                        if (defaultBehaviour == null)
                            Console.Write(question);
                        else
                            return defaultBehaviour == true;
                        break;
                }
            }
        }
    }
}
