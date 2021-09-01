using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Services
{
    public static class PipeWriterExtensions
    {
        public static async Task WriteAsync(this PipeWriter pipeWriter, string output)
        {
            await pipeWriter.WriteAsync(Encoding.UTF8.GetBytes(output));
        }
    }
}
