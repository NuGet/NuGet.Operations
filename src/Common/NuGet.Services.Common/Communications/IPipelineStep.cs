using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Communications
{
    public interface IPipelineStep
    {
        Task<object> Invoke(Func<Task<object>> action);
    }

    public static class PipelineStepExtensions
    {
        public static async Task Invoke(this IPipelineStep self, Func<Task> action)
        {
            await self.Invoke(async () => { await action(); return Unit.Instance; });
        }
    }
}
