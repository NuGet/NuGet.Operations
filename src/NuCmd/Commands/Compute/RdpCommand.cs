using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Compute.Models;
using PowerArgs;

namespace NuCmd.Commands.Compute
{
    public class RdpCommand : EnvironmentCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("s")]
        [ArgDescription("The name of the cloud service/VM to connect to")]
        public string Service { get; set; }

        [ArgPosition(1)]
        [ArgShortcut("i")]
        [ArgDescription("The name of the instance inside the service to connect to. Required if there are multiple instances")]
        public string InstanceName { get; set; }

        protected override async Task OnExecute()
        {
            // Get cloud service info for the machine
            using(var compute = CloudContext.Clients.CreateComputeManagementClient(await GetAzureCredentials()))
            {
                var slot = await compute.Deployments.GetBySlotAsync(Service, DeploymentSlot.Production);
                if (slot.RoleInstances.Count == 0)
                {
                    await Console.WriteErrorLine(Strings.Compute_RdpCommand_NoInstances);
                    return;
                }

                RoleInstance instance;
                if(String.IsNullOrEmpty(InstanceName))
                {
                    if(slot.RoleInstances.Count > 1)
                    {
                        await Console.WriteErrorLine(Strings.Compute_RdpCommand_MultipleInstances);
                        return;
                    }
                    instance = slot.RoleInstances.Single();
                } else
                {
                    instance = slot.RoleInstances.Single(r => String.Equals(r.InstanceName, InstanceName, StringComparison.OrdinalIgnoreCase));
                }

                await Console.WriteInfoLine(Strings.Compute_RdpCommand_GettingRdpFile, Service, instance.InstanceName);

                // Get the RDP file
                var rdp = await compute.VirtualMachines.GetRemoteDesktopFileAsync(
                    Service, slot.Name, instance.InstanceName);

                // Get the password
                var password = await GetSecretOrDefault("rdp." + Service);
                var sysroot = System.Environment.ExpandEnvironmentVariables("%systemroot%");
                if (String.IsNullOrEmpty(password))
                {
                    await Console.WriteWarningLine(Strings.Compute_RdpCommand_NoPasswordFound, Service);
                } else
                {
                    // Store the password using cmdkey
                    await Console.WriteInfoLine(Strings.Compute_RdpCommand_TemporarilyStoringPassword);
                    Process.Start(sysroot + @"\system32\cmdkey.exe", "/generic:TERMSRV/" + slot.Uri.Host + " /user:nuget /pass:" + password);
                }

                // Rewrite the RDP file to allow the use of stored creds if there are any
                var rdpFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".nucmd.rdp");
                var rdpStr = Encoding.Default.GetString(rdp.RemoteDesktopFile);
                if (!String.IsNullOrEmpty(password))
                {
                    rdpStr = rdpStr.Replace("prompt for credentials:i:1", "prompt for credentials:i:0");
                }
                rdpStr += System.Environment.NewLine + "authentication level:i:0";
                File.WriteAllText(rdpFile, rdpStr);

                await Console.WriteInfoLine(Strings.Compute_RdpCommand_LaunchingMstsc);
                Process.Start(sysroot + @"\system32\mstsc.exe", rdpFile);

                await Console.WriteInfoLine(Strings.Compute_RdpCommand_WaitingForCleanup);
                Thread.Sleep(5000);

                File.Delete(rdpFile);
                if (!String.IsNullOrEmpty(password))
                {
                    // Remove the password from cmdkey
                    await Console.WriteInfoLine(Strings.Compute_RdpCommand_RemovingCredentials);
                    Process.Start(sysroot + @"\system32\cmdkey.exe", "/delete:TERMSRV/" + slot.Uri.Host);
                }
            }
        }
    }
}