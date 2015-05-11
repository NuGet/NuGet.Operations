// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using NuGet.Services.Client;

namespace NuCmd.Models
{
    public abstract class AuditRecord
    {
        private string _resourceType;

        public abstract string GetPath();

        public virtual string GetResourceType()
        {
            return _resourceType ?? (_resourceType = InferResourceType());
        }

        public abstract string GetAction();

        private string InferResourceType()
        {
            string type = GetType().Name;
            if (type.EndsWith("AuditRecord", StringComparison.OrdinalIgnoreCase))
            {
                return type.Substring(0, type.Length - 11);
            }
            return type;
        }

        public async Task WriteAuditRecord(string resourceType, CloudStorageAccount storageAccount)
        {
            var entry = new AuditEntry(
                this,
                await AuditActor.GetCurrentMachineActor());

            // Write the blob to the storage account
            var client = storageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference("auditing");
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference(
                resourceType + "/" + this.GetPath() + "/" + DateTime.UtcNow.ToString("s") + "-" + this.GetAction().ToLowerInvariant() + ".audit.v1.json");

            if (await blob.ExistsAsync())
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Package_DeleteCommand_AuditBlobExists,
                    blob.Uri.AbsoluteUri));
            }

            byte[] data = Encoding.UTF8.GetBytes(
                JsonFormat.Serialize(entry));
            await blob.UploadFromByteArrayAsync(data, 0, data.Length);
        }
    }

    public abstract class AuditRecord<T> : AuditRecord
        where T : struct
    {
        public T Action { get; set; }

        protected AuditRecord(T action)
        {
            Action = action;
        }

        public override string GetAction()
        {
            return Action.ToString().ToLowerInvariant();
        }
    }
}
