using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FtdModManager.Standalone
{
    internal static class Program
    {
        const string modRelativePath = "From The Depths/Mods";
        const string ownManifestUri = "https://raw.githubusercontent.com/Why7090/FtdModManager/master/modmanifest.json";

        static string modParentPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), modRelativePath);
        static Args args;

        static bool auto = false;

        static void Main(string[] arguments)
        {
            args = new Args();

            args.AddCommand("help",    0, "-help", "--help", "/help", "-h", "/h", "h", "-?", "/?", "?");
            args.AddCommand("install", 2, "i");
            args.AddCommand("update",  1, "u", "upgrade");
            args.AddCommand("remove",  1, "r", "d", "delete", "u", "uninstall");
            args.AddCommand("list",    0, "l");
            args.AddCommand("setup",   0, "s", "self-update");

            args.AddOption("--parent",     false, "-p");
            args.AddOption("--accept-all", true,  "-y", "--yes");

            args.Parse(arguments);

            Run();
        }

        static void Run()
        {
            if (!args.TryGetCommand(out string cmd) || cmd == "setup") // self-update
            {
                string installPath = Path.Combine(modParentPath, "FtdModManager").NormalizedDirPath();

                InstallMod(ownManifestUri, installPath);

                Console.WriteLine("FtdModManager successfully installed to " + installPath);
                Console.Write("Press any key to exit: ");
                Console.ReadKey();
                return;
            }

            if (cmd == "help")
            {
                PrintHelp();
                return;
            }

            string[] param = args.GetCommandParameters();
            if (args.IsTrue("--accept-all"))
                auto = true;

            if (args.TryGetOption("--parent", out string value))
                modParentPath = value;
            Directory.SetCurrentDirectory(modParentPath);

            if (cmd == "install")
            {
                InstallMod(param[0], Path.GetFullPath(param[1]));
            }

            if (cmd == "update")
            {
                if (param[0] == "all")
                {
                    foreach (string file in Directory.GetFiles(modParentPath, ModPreferences.manifestFileName, SearchOption.AllDirectories))
                    {
                        UpdateMod(Path.GetDirectoryName(file));
                    }
                }
                else
                {
                    UpdateMod(Path.GetDirectoryName(Directory.GetFiles(
                        Path.GetFullPath(param[0]),
                        ModPreferences.manifestFileName,
                        SearchOption.AllDirectories).First()));
                }
            }
        }

        static void InstallMod(string manifestUri, string installFolder)
        {
            InstallModAsync(manifestUri, installFolder).Wait();
        }

        static async Task InstallModAsync(string manifestUri, string installFolder)
        {
            Directory.CreateDirectory(installFolder);
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
            if (updateInfo.isUpdateAvailable)
            {
                Helper.LogSeparator();
                Helper.Log(updateInfo.GetConfirmationMessage());
            }
            Helper.LogSeparator();
            Helper.Log(updateInfo.GetConfirmationTitle());
            Helper.LogSeparator();
            Helper.Log("\n");

            if (!updateInfo.isUpdateAvailable)
                return;

            if (Confirm("Do you want to install this mod? [Y/n] : ", true))
                return;

            await updateInfo.ApplyUpdate();

            Helper.Log("\n");
            string finishMessage = updateInfo.GetFinishMessage();
            if (!string.IsNullOrWhiteSpace(finishMessage))
            {
                Helper.LogSeparator();
                Helper.Log(finishMessage);
            }
            Helper.LogSeparator();
            Helper.Log(updateInfo.GetFinishTitle());
            Helper.LogSeparator();
            Helper.Log("\n");
        }

        static void PrintHelp()
        {
            Helper.Log(Properties.Resources.README);
        }

        static bool Confirm(string question, bool? defaultBehaviour = null)
        {
            if (auto)
                return true;

            Console.Write(question);
            while (true)
            {
                switch (Console.ReadKey(false).Key)
                {
                    case ConsoleKey.Y:
                        Console.WriteLine();
                        return true;
                    case ConsoleKey.N:
                        Console.WriteLine();
                        return false;
                    case ConsoleKey.Enter:
                        if (defaultBehaviour == null)
                        {
                            Console.Write(question);
                            break;
                        }
                        Console.WriteLine();
                        return defaultBehaviour == true;
                }
            }
        }
    }
}
