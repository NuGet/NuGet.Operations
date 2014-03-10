using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Operations;
using NuGet.Services.Operations.Config;
using NuGet.Services.Operations.Model;
using NuGet.Services.Operations.Secrets;
using PowerArgs;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;

namespace NuCmd.Commands.Config
{
    [Description("Generates a configuration file for the specified ")]
    public class GenerateCommand : DatacenterCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("s")]
        [ArgDescription("The service to generate configuration for")]
        public string Service { get; set; }

        [ArgPosition(1)]
        [ArgShortcut("o")]
        [ArgDescription("The output file to generate")]
        public string OutputFile { get; set; }

        protected override async Task OnExecute()
        {
            var dc = GetDatacenter(required: true);
            var service = dc.GetService(Service);
            if (service == null)
            {
                await Console.WriteErrorLine(Strings.Config_GenerateCommand_NoSuchService, Service, dc.FullName);
                return;
            }

            // Get the config template for this service
            if(!String.Equals(dc.Environment.ConfigTemplates.Type, FileSystemConfigTemplateSource.AbsoluteAppModelType, StringComparison.OrdinalIgnoreCase)) {
                await Console.WriteErrorLine(Strings.Config_GenerateCommand_UnknownConfigTemplateSourceType, dc.Environment.ConfigTemplates.Type);
            }
            var configSource = new FileSystemConfigTemplateSource(dc.Environment.ConfigTemplates.Value);
            var configTemplate = configSource.ReadConfigTemplate(service);
            if (String.IsNullOrEmpty(configTemplate))
            {
                await Console.WriteErrorLine(Strings.Config_GenerateCommand_NoTemplate, service.FullName);
            }
            else
            {
                var secrets = await GetEnvironmentSecretStore(Session.CurrentEnvironment);
                
                // Render the template
                var engine = new TemplateService(new TemplateServiceConfiguration()
                {
                    BaseTemplateType = typeof(ConfigTemplateBase),
                    Language = Language.CSharp
                });
                await Console.WriteInfoLine(Strings.Config_GenerateCommand_CompilingConfigTemplate, service.FullName);
                engine.Compile(configTemplate, typeof(object), "configTemplate");

                await Console.WriteInfoLine(Strings.Config_GenerateCommand_ExecutingTemplate, service.FullName);
                string result = engine.Run("configTemplate", new ConfigTemplateModel(secrets, service), null);

                // Write the template
                if (String.IsNullOrEmpty(OutputFile))
                {
                    await Console.WriteDataLine(result);
                }
                else
                {
                    File.WriteAllText(OutputFile, result);
                    await Console.WriteInfoLine(Strings.Config_GenerateCommand_GeneratedConfig, OutputFile);
                }
            }
        }
    }
}
