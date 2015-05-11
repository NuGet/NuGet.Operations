// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NuCmd
{
    public class SystemConsole : IConsole
    {
        private ConsoleWriter _error = new ConsoleWriter("error", ConsoleColor.Red, Console.Error);
        private ConsoleWriter _warning = new ConsoleWriter("warn", ConsoleColor.Yellow, Console.Error);
        private ConsoleWriter _info = new ConsoleWriter("info", ConsoleColor.Green, Console.Out);
        private ConsoleWriter _help = new ConsoleWriter("help", ConsoleColor.Blue, Console.Error);
        private ConsoleWriter _trace = new ConsoleWriter("trace", ConsoleColor.Gray, Console.Error);
        private ConsoleWriter _data = new ConsoleWriter("data", ConsoleColor.DarkRed, Console.Out);
        private ConsoleWriter _http = new ConsoleWriter("http", ConsoleColor.Cyan, Console.Out);
        private ConsoleWriter _fatal = new ConsoleWriter("fatal", ConsoleColor.Red, Console.Out);

        public TextWriter Error { get { return _error; } }
        public TextWriter Fatal { get { return _fatal; } }
        public TextWriter Trace { get { return _trace; } }
        public TextWriter Warning { get { return _warning; } }
        public TextWriter Info { get { return _info; } }
        public TextWriter Help { get { return _help; } }
        public TextWriter Http { get { return _http; } }
        public TextWriter Data { get { return _data; } }

        public Task WriteObject(object obj, IConsoleFormatter formatter)
        {
            var formatted = formatter.Format(obj);
            return _data.WriteLineAsync(formatted);
        }

        public async Task WriteObjects(IEnumerable<object> objs, IConsoleFormatter formatter)
        {
            foreach (var obj in objs)
            {
                await WriteObject(obj, formatter);
            }
        }

        public async Task WriteTable(ConsoleTable table)
        {
            await _data.WriteLineAsync(table.GetHeader());

            var rows = table.GetRows();
            if (rows.Any())
            {
                foreach (var row in rows)
                {
                    await _data.WriteLineAsync(row);
                }
            }
        }

        public Task WriteTable<T>(IEnumerable<T> objs, Func<T, object> selector)
        {
            var table = ConsoleTable.For(objs, selector);
            return WriteTable(table);
        }

        public async Task<string> Prompt(string message)
        {
            await this.WriteInfo(message + " ");
            string str = Console.ReadLine();
            _info.RestartLine();
            return str;
        }

        private static readonly Dictionary<string, bool> _values = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) {
            {"y", true},
            {"yes", true},
            {"n", false},
            {"no", false}
        };

        public async Task<bool> Confirm(string message, bool defaultValue)
        {
            string prompt = message + " " + (defaultValue ? Strings.SystemConsole_ConfirmSuffix_DefaultYes : Strings.SystemConsole_ConfirmSuffix_DefaultNo);
            bool? result = null;
            while (result == null)
            {
                string str = await Prompt(prompt);
                bool b;
                if (String.IsNullOrWhiteSpace(str))
                {
                    result = defaultValue;
                }
                else if (_values.TryGetValue(str, out b))
                {
                    result = b;
                }
                else
                {
                    await this.WriteInfoLine(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.SystemConsole_ConfirmUnknownAnswer,
                        str));
                }
            }
            return result.Value;
        }

        public async Task<SecureString> PromptForPassword(string prompt)
        {
            await this.WriteInfo(prompt + " ");
            ConsoleKeyInfo key;
            SecureString password = new SecureString();
            do {
                key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    // Remove the last character
                    password.RemoveAt(password.Length - 1);
                    Console.CursorLeft -= 1;
                    Console.Write(" ");
                    Console.CursorLeft -= 1;
                }
                else if(key.KeyChar > '\0' && key.KeyChar != '\r' && key.KeyChar != '\n' && key.KeyChar != '\t') {
                    // Append the character to the password
                    password.AppendChar(key.KeyChar);
                    Console.Write("*");
                }
                // Exit on Enter
            } while(key.Key != ConsoleKey.Enter);
            password.MakeReadOnly();
            await this.WriteInfoLine();
            return password;
        }

        internal class ConsoleWriter : TextWriter
        {
            private static ConcurrentBag<string> _prefixes = new ConcurrentBag<string>();
            private static Lazy<int> _maxPrefix = new Lazy<int>(() => _prefixes.Max(p => p.Length));

            private string _prefix;
            private TextWriter _console;
            private ConsoleColor _prefixColor;
            private Action<ConsoleColor> _setForegroundColor;
            private Func<ConsoleColor> _getForegroundColor;

            private char? _previous = null;

            public override Encoding Encoding
            {
                get { return _console.Encoding; }
            }

            public ConsoleWriter(string prefix, ConsoleColor prefixColor, TextWriter console)
                : this(prefix, prefixColor, console, c => Console.ForegroundColor = c, () => Console.ForegroundColor) { }

            public ConsoleWriter(string prefix, ConsoleColor prefixColor, TextWriter console, Action<ConsoleColor> setForegroundColor, Func<ConsoleColor> getForegroundColor)
            {
                _prefix = prefix;
                _console = console;
                _prefixColor = prefixColor;
                _setForegroundColor = setForegroundColor;
                _getForegroundColor = getForegroundColor;

                _prefixes.Add(prefix);
            }

            public void RestartLine()
            {
                _previous = null;
            }

            public override async Task WriteAsync(char value)
            {
                await WritePrefixIfNecessary(value);
                _previous = value;
                await _console.WriteAsync(value);
            }

            public override void Write(char value)
            {
                WriteAsync(value).Wait();
            }

            public override async Task WriteAsync(string value)
            {
                if (value != null)
                {
                    await WriteAsync(value.ToCharArray());
                }
            }

            public override async Task WriteAsync(char[] buffer, int index, int count)
            {
                if (buffer != null)
                {
                    for (int i = index; i < count; i++)
                    {
                        await WriteAsync(buffer[i]);
                    }
                }
            }

            public override async Task WriteLineAsync()
            {
                await WriteAsync(Environment.NewLine);
            }

            public override async Task WriteLineAsync(char value)
            {
                await WriteAsync(value);
                await WriteLineAsync();
            }

            public override async Task WriteLineAsync(char[] buffer, int index, int count)
            {
                if (buffer != null)
                {
                    for (int i = index; i < count; i++)
                    {
                        await WriteAsync(buffer[i]);
                    }
                    await WriteLineAsync();
                }
            }

            public override async Task WriteLineAsync(string value)
            {
                await WriteAsync(value);
                await WriteLineAsync();
            }

            private async Task WritePrefixIfNecessary(char value)
            {
                // Don't write prefix for \n in two-char newline
                if(_previous == null || (_previous == '\r' && value != '\n') || _previous == '\n')
                {
                    await WritePrefix();
                }
            }

            private async Task WritePrefix()
            {
                var old = _getForegroundColor();
                _setForegroundColor(_prefixColor);
                await _console.WriteAsync(_prefix.PadRight(_maxPrefix.Value));
                _setForegroundColor(old);

                await _console.WriteAsync(": ");
            }
        }
    }
}
