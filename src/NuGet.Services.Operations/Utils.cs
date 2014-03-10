using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nustache.Core;

namespace NuGet.Services.Operations
{
    public static class Utils
    {
        static Utils()
        {
            // Clone the existing getters
            var factories = ValueGetterFactories.Factories.ToArray();

            ValueGetterFactories.Factories.Clear();
            ValueGetterFactories.Factories.Add(new DelegateWrappingValueGetterFactory(factories));
        }

        public static string GeneratePassword(bool timestamped)
        {
            string randomness =
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(
                        Guid.NewGuid().ToString("N"))).Replace("=", "");

            if (timestamped)
            {
                return DateTime.Now.ToString("MMMddyy") + "!" + randomness;
            }
            else
            {
                return randomness;
            }
        }

        public static string RenderNustacheTemplate(string template, object model)
        {
            return Render.StringToString(template, model);
        }
    }
}
