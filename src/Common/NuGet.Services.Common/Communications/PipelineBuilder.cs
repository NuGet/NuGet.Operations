using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Communications
{
    public delegate Task<object> Pipeline(Func<Task<object>> action);

    public class PipelineBuilder
    {
        private IList<IPipelineStep> _steps = new List<IPipelineStep>();

        public PipelineBuilder()
        {
        }

        public void Use(IPipelineStep step)
        {
            _steps.Add(step);   
        }

        public Pipeline Build()
        {
            Pipeline pipe = next => next();
            foreach (var step in _steps.Reverse())
            {
                pipe = next => step.Invoke(next);
            }
            return pipe;
        }
    }
}
