using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Helpers
{
    public static class TaskHelper
    {
        public static async Task WhenAllAndThrow(params Task[] tasks)
        {
            await Task.WhenAll(tasks);
            if (tasks != null)
            {
                foreach (Task task in tasks)
                {
                    if (task.Exception != null)
                    {
                        throw task.Exception;
                    }
                }
            }
        }
    }
}
