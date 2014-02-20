using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuCmd
{
    public class CommandGroup : IReadOnlyDictionary<string, CommandDefinition>
    {
        private IReadOnlyDictionary<string, CommandDefinition> _commands;
        public static readonly CommandGroup Empty = new CommandGroup(String.Empty, String.Empty, Enumerable.Empty<CommandDefinition>());

        public string Name { get; private set; }
        public string Description { get; private set; }

        public CommandGroup(string name, string description, IEnumerable<CommandDefinition> commands)
            : this(name, description, new ReadOnlyDictionary<string, CommandDefinition>(commands.ToDictionary(d => d.Name))) { }
        public CommandGroup(string name, string description, IReadOnlyDictionary<string, CommandDefinition> commands)
        {
            _commands = commands;
            Name = name;
            Description = description;
        }

        public bool ContainsKey(string key)
        {
            return _commands.ContainsKey(key);
        }

        public IEnumerable<string> Keys
        {
            get { return _commands.Keys; }
        }

        public bool TryGetValue(string key, out CommandDefinition value)
        {
            return _commands.TryGetValue(key, out value);
        }

        public IEnumerable<CommandDefinition> Values
        {
            get { return _commands.Values; }
        }

        public CommandDefinition this[string key]
        {
            get { return _commands[key]; }
        }

        public int Count
        {
            get { return _commands.Count; }
        }

        IEnumerator<KeyValuePair<string, CommandDefinition>> IEnumerable<KeyValuePair<string, CommandDefinition>>.GetEnumerator()
        {
            return _commands.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _commands.GetEnumerator();
        }

        internal static CommandGroup Create(IGrouping<string, CommandDefinition> cs)
        {
            // Try to get the description
            var desc = Strings.ResourceManager.GetString("Group_" + cs.Key.ToLowerInvariant());
            return new CommandGroup(
                cs.Key,
                desc,
                cs.ToDictionary(c => c.Name));
        }
    }
}
