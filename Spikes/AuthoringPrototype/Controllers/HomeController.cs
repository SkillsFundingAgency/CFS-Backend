using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Allocations.Models;
using Allocations.Models.Results;
using Allocations.Models.Specs;
using Allocations.Repository;
using Allocations.Services.Calculator;
using Allocations.Services.TestRunner;

namespace AuthoringPrototype.Controllers
{
    public class BuildProductViewModel
    {
        public BuildProductViewModel()
        {
            TestResults = new List<ProviderTestResult>();
        }
        public Product Product { get; set; }
        public BudgetCompilerOutput CompilerOutput { get; set; }
        public List<ProviderTestResult> TestResults { get; set; }
    }

    public class HomeController : Controller
    {
        private readonly Repository<Budget> _repository = new Repository<Budget>("specs");

        public  ActionResult Index()
        {
            return View(_repository.Query().ToList().First());
        }

        public async Task<ActionResult> Product(string id, string calculation = null)
        {
            var budget = _repository.Query().ToList().First();
            Product product = GetProduct(id, budget);
            if (product == null) return HttpNotFound();

            if (!string.IsNullOrWhiteSpace(calculation))
            {
                product.Calculation = new ProductCalculation { SourceCode = calculation };
            }

            var compilerOutput = BudgetCompiler.GenerateAssembly(budget);


            var viewModel = new BuildProductViewModel
            {
                Product = product,
                CompilerOutput = BudgetCompiler.GenerateAssembly(budget)
            };

            if (compilerOutput.Success)
            {

                var calc = new CalculationEngine(compilerOutput);

                foreach (var testProvider in product.TestProviders ?? new Reference[0])
                {
                    var typedDatasets = await calc.GetProviderDatasets(testProvider);


                    var providerResult = calc.CalculateProviderProducts(testProvider, typedDatasets);
                    var testResult = calc.RunProviderTests(testProvider, typedDatasets, providerResult);
                    viewModel.TestResults.Add(testResult);
                }


            }



            return View(viewModel);
        }

        public async Task<ActionResult> Scenarios(string id)
        {
            var budget = _repository.Query().ToList().First();
            Product product = GetProduct(id, budget);
            if (product == null) return HttpNotFound();

            var compilerOutput = BudgetCompiler.GenerateAssembly(budget);


            var viewModel = new BuildProductViewModel
            {
                Product = product,
                CompilerOutput = BudgetCompiler.GenerateAssembly(budget)
            };
            if (compilerOutput.Success)
            {

                var calc = new CalculationEngine(compilerOutput);

                foreach (var testProvider in product.TestProviders ?? new Reference[0])
                {
                    var typedDatasets = await calc.GetProviderDatasets(testProvider);


                    var providerResult = calc.CalculateProviderProducts(testProvider, typedDatasets);
                    var testResult = calc.RunProviderTests(testProvider, typedDatasets, providerResult);
                    viewModel.TestResults.Add(testResult);
                }


            }



            return View(viewModel);
        }

        private static Product GetProduct(string id, Budget budget)
        {
            Product product = null;
            foreach (var fundingPolicy in budget.FundingPolicies)
            {
                foreach (var allocationLine in fundingPolicy.AllocationLines)
                {
                    foreach (var productFolder in allocationLine.ProductFolders)
                    {
                        product = productFolder.Products.FirstOrDefault(x => x.Id == id);
                    }
                }
            }
            return product;
        }





        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}