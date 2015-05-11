// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
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
using NuGet.Services.Operations.Secrets;
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

        protected virtual async Task<SecretStore> GetEnvironmentSecretStore(DeploymentEnvironment env)
        {
            if (env.SecretStore == null || String.IsNullOrEmpty(env.SecretStore.Value))
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Command_EnvironmentHasNoSecretStore,
                    env.Name));
            }
            if (!String.Equals(env.SecretStore.Type, DpapiSecretStoreProvider.AppModelTypeName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Command_UnknownSecretStoreType,
                    env.SecretStore.Type));
            }
            var store = await (new DpapiSecretStoreProvider(env.SecretStore.Value).Open(env));
            if (!store.Exists())
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Command_SecretStoreNotCreated,
                    env.Name));
            }
            return store;
        }

        protected virtual DeploymentEnvironment GetEnvironment(string provided)
        {
            return GetEnvironment(provided, required: true);
        }

        protected virtual DeploymentEnvironment GetEnvironment(string provided, bool required)
        {
            if (Session == null)
            {
                if (required)
                {
                    throw new InvalidOperationException(Strings.Command_NoSession);
                }
                return null;
            }
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
            if (env == null && required)
            {
                throw new InvalidOperationException(Strings.Command_NoEnv);
            }
            return env;
        }

        protected virtual Datacenter GetDatacenter(DeploymentEnvironment environment, int datacenter)
        {
            return GetDatacenter(environment, datacenter, required: true);
        }

        protected virtual Datacenter GetDatacenter(DeploymentEnvironment environment, int datacenter, bool required)
        {
            var dc = environment.Datacenters.FirstOrDefault(d => d.Id == datacenter);
            if (dc == null && required)
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Command_UnknownDc,
                    environment.Name,
                    datacenter));
            }
            return dc;
        }

        protected virtual Task<string> GetSecretOrDefault(string secretName)
        {
            return GetSecretOrDefault(Session.CurrentEnvironment, secretName, datacenter: null, clientOperation: Definition.FullName);
        }

        protected virtual Task<string> GetSecretOrDefault(string secretName, int datacenter)
        {
            return GetSecretOrDefault(Session.CurrentEnvironment, secretName, datacenter, clientOperation: Definition.FullName);
        }

        protected virtual async Task<string> GetSecretOrDefault(DeploymentEnvironment env, string secretName, int? datacenter, string clientOperation)
        {
            var secrets = await GetEnvironmentSecretStore(env);
            if (secrets == null)
            {
                return null;
            }

            var secret = await secrets.Read(new SecretName(secretName, datacenter), clientOperation);
            if (secret == null)
            {
                return null;
            }
            return secret.Value;
        }
    }
}
