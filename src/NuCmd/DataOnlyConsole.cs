using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading.Tasks;

namespace NuCmd
{
    internal class DataOnlyConsole : IConsole
    {
        private IConsole _console;

        public DataOnlyConsole(IConsole _console)
        {
            this._console = _console;
        }

        public TextWriter Data
        {
            get
            {
                return _console.Data;
            }
        }

        public TextWriter Error
        {
            get
            {
                return _console.Error;
            }
        }

        public TextWriter Fatal
        {
            get
            {
                return _console.Fatal;
            }
        }

        public TextWriter Help
        {
            get
            {
                return TextWriter.Null;
            }
        }

        public TextWriter Http
        {
            get
            {
                return TextWriter.Null;
            }
        }

        public TextWriter Info
        {
            get
            {
                return TextWriter.Null;
            }
        }

        public TextWriter Trace
        {
            get
            {
                return TextWriter.Null;
            }
        }

        public TextWriter Warning
        {
            get
            {
                return TextWriter.Null;
            }
        }

        public Task<bool> Confirm(string message, bool defaultValue)
        {
            return _console.Confirm(message, defaultValue);
        }

        public Task<string> Prompt(string message)
        {
            return _console.Prompt(message);
        }

        public Task<SecureString> PromptForPassword(string message)
        {
            return _console.PromptForPassword(message);
        }

        public Task WriteObject(object obj, IConsoleFormatter formatter)
        {
            return _console.WriteObject(obj, formatter);
        }

        public Task WriteObjects(IEnumerable<object> objs, IConsoleFormatter formatter)
        {
            return _console.WriteObject(objs, formatter);
        }

        public Task WriteTable(ConsoleTable table)
        {
            return _console.WriteTable(table);
        }

        public Task WriteTable<T>(IEnumerable<T> objs, Func<T, object> selector)
        {
            return _console.WriteTable(objs, selector);
        }
    }
}