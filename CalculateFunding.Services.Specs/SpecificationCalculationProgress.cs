using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services
{
	public class SpecificationCalculationProgress
	{
		public SpecificationCalculationProgress(string specificationId, int percentageCompleted,
			CalculationProgressStatus calculationProgress)
		{
			SpecificationId = specificationId;
			PercentageCompleted = percentageCompleted;
			CalculationProgress = calculationProgress;
		}

		public string SpecificationId { get; set; }
		public int PercentageCompleted { get; set; }
		public CalculationProgressStatus CalculationProgress { get; set; }
		public string ErrorMessage { get; set; }
        
		public enum CalculationProgressStatus
		{
			NotStarted,
			Started,
			Error,
			Finished
		}
        //public void test(List<CalculationProgressStatus> status)
        //{

        //   // status.Count()
        //    foreach (var row in status)
        //    {
                
        //    }
        //}
        //public void ProcessRequest(HttpContext context)
        //{
        //    context.Response.ContentType = "text/plain";

        //    if (context.Cache["_cache"] != null)
        //    {
        //        if (context.Cache["_cache"].ToString() == "stop")
        //        {
        //            context.Response.WriteAsync("stop");
        //        }
        //        else
        //        {
        //            context.Response.WriteAsync(context.Cache["_cache"].ToString());
        //        }
        //    }
        //    else
        //        context.Response.WriteAsync((-2).ToString());
        //}
	}
}