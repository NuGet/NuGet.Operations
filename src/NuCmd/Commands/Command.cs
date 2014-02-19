using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Client;
using NuGet.Services.Operations;
using NuGet.Services.Operations.Model;
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

                if (WhatIf)
                {
                    await Console.WriteInfoLine(Strings.Command_WhatIfMode);
                }

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

        protected virtual void EnsureSession()
        {
            if (Session == null)
            {
                throw new InvalidOperationException(Strings.Command_NoSession);
            }
        }

        protected virtual DeploymentEnvironment GetEnvironment(string provided)
        {
            EnsureSession();
            DeploymentEnvironment env;
            if (String.IsNullOrEmpty(provided))
            {
                env = Session.CurrentEnvironment;
            }
            else
            {
                env = Session.Model.Environments.FirstOrDefault(e => String.Equals(e.Name, provided, StringComparison.OrdinalIgnoreCase));
                if (env == null)
                {
                    throw new InvalidOperationException(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Command_UnknownEnv,
                        provided));
                }
            }
            if (env == null)
            {
                throw new InvalidOperationException(Strings.Command_NoEnv);
            }
            return env;
        }

        protected virtual Datacenter GetDatacenter(DeploymentEnvironment environment, int datacenter)
        {
            var dc = environment.Datacenters.FirstOrDefault(d => d.Id == datacenter);
            if (dc == null)
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Command_UnknownDc,
                    environment.Name,
                    datacenter));
            }
            return dc;
        }
    }
}
