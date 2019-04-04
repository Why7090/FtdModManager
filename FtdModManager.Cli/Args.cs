using System;
using System.Collections.Generic;
using System.Linq;

namespace FtdModManager.Cli
{
    public class Args
    {
        private Dictionary<string, string> options = new Dictionary<string, string>();
        private NameWithAlias command;
        private string[] commandParameters;

        private List<NameWithAlias> allCommands = new List<NameWithAlias>();
        private List<NameWithAlias> allOptions = new List<NameWithAlias>();

        public void AddCommand(string name, int count, params string[] aliases)
        {
            allCommands.Add(new NameWithAlias(name, count, aliases));
        }

        public void AddOption(string name, bool isFlag, params string[] aliases)
        {
            allOptions.Add(new NameWithAlias(name, isFlag ? 0 : 1, aliases));
        }

        public void Parse(string[] arguments)
        {
            if (arguments.Length == 0) return;

            if (arguments.Length >= 1)
                command = allCommands.FirstOrDefault(x => x.CompareWith(arguments[0]));

            if (command == default(NameWithAlias)) return;

            int cursor = 1;
            commandParameters = new string[command.count];

            while (cursor < arguments.Length
                && cursor <= command.count
                && !arguments[cursor].StartsWith("-"))
            {
                commandParameters[cursor - 1] = arguments[cursor];
                cursor++;
            }

            while (cursor < arguments.Length)
            {
                var opt = allOptions.FirstOrDefault(x => x.CompareWith(arguments[cursor]));
                cursor++;
                if (opt == null)
                    continue;

                options[opt.name] = null;
                if (opt.count == 1 && cursor < arguments.Length)
                {
                    options[opt.name] = arguments[cursor];
                    cursor++;
                }
            }
        }

        public bool AnyCommand()
        {
            return command != default(NameWithAlias);
        }

        public bool TryGetCommand(out string action)
        {
            action = this.command?.name;
            return AnyCommand();
        }

        public string[] GetCommandParameters()
        {
            return commandParameters;
        }

        public bool IsTrue(string option)
        {
            return options.ContainsKey(option);
        }

        public bool TryGetOption(string option, out string value)
        {
            return options.TryGetValue(option, out value);
        }

        public string GetOption(string option)
        {
            TryGetOption(option, out string value);
            return value;
        }

        private class NameWithAlias
        {
            public string name;
            public string[] aliases;
            public int count;

            public NameWithAlias(string name, int count, params string[] aliases)
            {
                this.name = name;
                this.aliases = aliases;
                this.count = count;
            }

            public bool CompareWith(string input)
            {
                return name.Equals(input, StringComparison.InvariantCultureIgnoreCase)
                    || aliases.Contains(input, StringComparer.InvariantCultureIgnoreCase);
            }
        }
    }
}
