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

        static string rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), modRelativePath).NormalizedDirPath();
        static Args args;

        static bool auto = false;

        static void Main(string[] arguments)
        {
            args = new Args();

            args.AddCommand("help",    0, "-help", "--help", "/help", "-h", "/h", "h", "-?", "/?", "?");
            args.AddCommand("install", 2, "i");
            args.AddCommand("update",  1, "u", "upgrade");
            args.AddCommand("remove",  1, "r", "d", "delete", "uninstall");
            args.AddCommand("list",    0, "l");
            args.AddCommand("setup",   0, "s", "self-update");

            args.AddOption("--root",       false, "-r");
            args.AddOption("--accept-all", true,  "-y", "--yes");

            args.Parse(arguments);

            Run();
        }

        static void Run()
        {
            if (!args.TryGetCommand(out string cmd) || cmd == "setup") // self-update
            {
                string installPath = Path.Combine(rootPath, "FtdModManager").NormalizedDirPath();
                Console.WriteLine($"Installing FtdModManager to {installPath}");

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

            if (args.TryGetOption("--root", out string value))
                rootPath = value.NormalizedDirPath();
            Directory.SetCurrentDirectory(rootPath);

            if (cmd == "install")
            {
                InstallMod(param[0], param[1]);
            }

            if (cmd == "update")
            {
                if (param[0] == "all")
                {
                    foreach (string file in Directory.GetFiles(rootPath, ModPreferences.manifestFileName, SearchOption.AllDirectories))
                    {
                        UpdateMod(Path.GetDirectoryName(file));
                    }
                }
                else
                {
                    UpdateMod(Path.GetDirectoryName(Directory.GetFiles(
                        Path.GetFullPath(param[0]).NormalizedDirPath(),
                        ModPreferences.manifestFileName,
                        SearchOption.AllDirectories).First()));
                }
            }
        }

        static void InstallMod(string manifestUri, string installFolder = null)
        {
            InstallModAsync(manifestUri, installFolder).Wait();
        }

        static async Task InstallModAsync(string manifestUri, string installFolder = null)
        {
            var info = new StandaloneModUpdateInfo(rootPath);
            bool success = await info.PrepareNewInstallation(manifestUri, installFolder);
            if (!success)
            {
                Helper.Log("\n");
                Helper.Log("The following target directory is not empty.");
                Helper.Log(info.basePath);
                if (!Confirm("Do you want to delete this folder permanently in order to continue installation? [y/N] : ", false))
                    return;
                await info.PrepareNewInstallation(manifestUri, installFolder);
            }
            await UpdateModAsync(info);
        }

        static void UpdateMod(string basePath)
        {
            var updateInfo = new StandaloneModUpdateInfo(basePath);
            UpdateModAsync(updateInfo).Wait();
        }

        static async Task UpdateModAsync(StandaloneModUpdateInfo updateInfo)
        {
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

            if (!Confirm("Do you want to install this mod? [Y/n] : ", true))
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
            Console.WriteLine(Properties.Resources.README);
        }

        static bool Confirm(string question, bool? defaultBehaviour = null)
        {
            Console.Write(question);

            if (auto)
            {
                Console.Write("y (--accept-all)");
                return true;
            }

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
