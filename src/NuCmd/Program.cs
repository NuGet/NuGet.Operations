// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuCmd.Commands;
using NuGet.Services.Operations;
using PowerArgs;

namespace NuCmd
{
    class Program
    {
        public IConsole _console;
        private IEnumerable<string> _args;
        private CommandDirectory _directory;

        public Program(IEnumerable<string> args)
        {
            _args = args;
            _console = new SystemConsole();
            _directory = new CommandDirectory();
            _directory.LoadCommands(typeof(Program).Assembly);
        }

        static void Main(string[] args)
        {
            // Configure embedded assembly-based resolution
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

#if DEBUG
            if (args.Length > 0 && args[0] == "dbg")
            {
                Debugger.Launch();
                args = args.Skip(1).ToArray();
            }
            if (args.Length > 0 && args[0] == "dbgw")
            {
                Console.Error.WriteLine("Waiting for debugger to attach...");
                args = args.Skip(1).ToArray();
                SpinWait.SpinUntil(() => Debugger.IsAttached);
            }
#endif
            new Program(args).Run().Wait();
        }

        private async Task Run()
        {
            if (String.Equals(_args.FirstOrDefault(), "-data", StringComparison.OrdinalIgnoreCase))
            {
                _args = _args.Skip(1);
                _console = new DataOnlyConsole(_console);
            }

            await _console.WriteTraceLine("NuCmd v{0} (built from {1})", 
                typeof(Program).Assembly.GetName().Version,
                typeof(Program)
                    .Assembly
                    .GetCustomAttributes<AssemblyMetadataAttribute>()
                    .Where(m => String.Equals("CommitId", m.Key, StringComparison.OrdinalIgnoreCase))
                    .Select(m => m.Value)
                    .FirstOrDefault() ?? "<unknown>");

            // Try to load an ops session if the environment variable is provided
            OperationsSession session;
            Exception thrown = null;
            try
            {
                session = OperationsSession.LoadFromEnvironment();
            }
            catch(Exception ex)
            {
                thrown = ex;
                session = null;
            }

            if (thrown != null && !String.IsNullOrEmpty(Environment.GetEnvironmentVariable(OperationsSession.AppModelEnvironmentVariableName)))
            {
                await _console.WriteWarningLine(Strings.Program_ErrorLoadingSession, thrown.ToString());
            }   

            // Get the command group
            var groupOrCommand = _args.FirstOrDefault();
            
            _args = _args.Skip(1);
            await DispatchGroup(session, groupOrCommand ?? "help");
        }

        private async Task DispatchGroup(OperationsSession session, string groupOrRootCommand)
        {
            // Commands are classes with the following naming convention
            //  NuCmd.Commands.<CommandName>Command
            // OR:
            //  NuCmd.Commands.<Group>.<CommandName>Command

            // Find the group being referenced
            CommandGroup group;
            string commandName;
            if (_directory.Groups.TryGetValue(groupOrRootCommand, out group))
            {
                commandName = _args.FirstOrDefault();
                _args = _args.Skip(1);
            }
            else
            {
                commandName = groupOrRootCommand;
                groupOrRootCommand = null;
                group = _directory.RootCommands;
            }

            commandName = commandName ?? String.Empty;

            if (String.IsNullOrEmpty(commandName))
            {
                // nucmd work => nucmd help work
                group = _directory.RootCommands;
                commandName = "help";
                _args = new[] { groupOrRootCommand };
            }

            CommandDefinition command;
            if (!group.TryGetValue(commandName, out command))
            {
                if (String.IsNullOrEmpty(groupOrRootCommand))
                {
                    await _console.WriteErrorLine(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Program_NoSuchCommand,
                        commandName));
                }
                else
                {
                    await _console.WriteErrorLine(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Program_NoSuchCommandInGroup,
                        commandName,
                        groupOrRootCommand));
                }
            }
            else
            {
                await Dispatch(session, command);
            }
        }

        private async Task Dispatch(OperationsSession session, CommandDefinition definition)
        {
            ICommand cmd = null;
            Exception thrown = null;
            try
            {
                cmd = Args.Parse(definition.Type, _args.ToArray()) as ICommand;
            }
            catch (AggregateException aex)
            {
                thrown = aex.InnerException;
            }
            catch (Exception ex)
            {
                thrown = ex;
            }
            if (thrown != null)
            {
                await _console.WriteErrorLine(thrown.Message);
                await new HelpCommand().HelpFor(_console, definition);
            }
            else if (cmd == null)
            {
                await _console.WriteErrorLine(
                    Strings.Program_CommandNotConvertible,
                    definition.Type.FullName,
                    typeof(ICommand).FullName);
            }
            else
            {
                thrown = null;
                try
                {
                    await cmd.Execute(session, _console, definition, _directory);
                }
                catch (AggregateException aex)
                {
                    thrown = aex.InnerException;
                }
                catch (OperationCanceledException)
                {
                    // Do nothing when this is thrown, it's just used to jump out of the job.
                }
                catch (Exception ex)
                {
                    thrown = ex;
                }
                if (thrown != null)
                {
                    await _console.WriteErrorLine(thrown.ToString());
                }
            }
        }

        private async Task WriteUsage(string error)
        {
            await _console.WriteErrorLine(error);
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Look for a resource
            var asmName = new AssemblyName(args.Name);
            var strm = typeof(Program).Assembly.GetManifestResourceStream(
                "EmbeddedAssemblies." + asmName.Name + ".dll");
            if (strm == null)
            {
                return null;
            }

            // Load it and return it
            using (strm)
            using (var ms = new MemoryStream())
            {
                strm.CopyTo(ms);
                return Assembly.Load(ms.ToArray());
            }
        }
    }
}
