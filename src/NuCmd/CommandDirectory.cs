// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NuCmd.Commands;

namespace NuCmd
{
    public class CommandDirectory
    {
        private List<CommandDefinition> _commands = new List<CommandDefinition>();
        private IReadOnlyDictionary<string, CommandGroup> _groups = new ReadOnlyDictionary<string, CommandGroup>(new Dictionary<string, CommandGroup>());
        private CommandGroup _rootCommands = 
            new CommandGroup(String.Empty, String.Empty, new ReadOnlyDictionary<string, CommandDefinition>(new Dictionary<string, CommandDefinition>()));
            

        public CommandGroup RootCommands { get { return _rootCommands; } }
        public IReadOnlyList<CommandDefinition> Commands { get { return _commands.AsReadOnly(); } }
        public IReadOnlyDictionary<string, CommandGroup> Groups { get { return _groups; } }

        public CommandDirectory()
        {
        }

        public void LoadCommands(params Assembly[] assemblies)
        {
            _commands = assemblies
                .SelectMany(a => 
                    a.GetExportedTypes()
                     .Where(t => !t.IsAbstract && t.Namespace.StartsWith("NuCmd.Commands") && typeof(ICommand).IsAssignableFrom(t))
                     .Select(CommandDefinition.FromType))
                .ToList();
            _groups = new ReadOnlyDictionary<string, CommandGroup>(
                _commands
                    .GroupBy(c => c.Group ?? String.Empty)
                    .Where(c => !String.IsNullOrEmpty(c.Key))
                    .Select(cs => CommandGroup.Create(cs))
                    .ToDictionary(g => g.Name));
            _rootCommands = new CommandGroup(
                String.Empty,
                String.Empty,
                _commands
                    .Where(c => String.IsNullOrEmpty(c.Group)));
        }

        public CommandGroup GetGroup(string group)
        {
            if (String.IsNullOrEmpty(group))
            {
                return RootCommands;
            }

            CommandGroup commands;
            if (!Groups.TryGetValue(group, out commands))
            {
                return CommandGroup.Empty;
            }
            return commands;
        }

        public CommandDefinition GetCommand(string group, string name)
        {
            CommandDefinition command;
            if (!GetGroup(group).TryGetValue(name, out command))
            {
                return null;
            }
            return command;
        }
    }
}
