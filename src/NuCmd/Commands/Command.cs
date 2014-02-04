using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Client;
using NuGet.Services.Operations;
using PowerArgs;

namespace NuCmd.Commands
{
    public interface ICommand {
        Task Execute(OperationsSession session, IConsole console, CommandDefinition definition, CommandDirectory directory);
    }

    public abstract class Command : ICommand
    {
        [ArgShortcut("!")]
        [ArgShortcut("n")]
        [ArgDescription("Report what the command would do but do not actually perform any changes")]
        public bool WhatIf { get; set; }

        protected IConsole Console { get; private set; }
        protected CommandDefinition Definition { get; private set; }
        protected CommandDirectory Directory { get; private set; }
        protected OperationsSession Session { get; set; }

        public virtual async Task Execute(OperationsSession session, IConsole console, CommandDefinition definition, CommandDirectory directory)
        {
            if (await LoadContext(session, console, definition, directory))
            {
                await LoadDefaultsFromContext();
                await OnExecute();
            }
        }

        protected virtual Task LoadDefaultsFromContext()
        {
            return Task.FromResult<object>(null);
        }

        protected virtual Task<bool> LoadContext(OperationsSession session, IConsole console, CommandDefinition definition, CommandDirectory directory)
        {
            Console = console;
            Directory = directory;
            Definition = definition;
            Session = session;

            return Task.FromResult(true);
        }

        protected abstract Task OnExecute();

        protected virtual async Task<bool> ReportHttpStatus<T>(ServiceResponse<T> response)
        {
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            await Console.WriteErrorLine(
                Strings.Commands_HttpError,
                (int)response.StatusCode,
                response.ReasonPhrase,
                await response.HttpResponse.Content.ReadAsStringAsync());
            return false;
        }
    }
}
