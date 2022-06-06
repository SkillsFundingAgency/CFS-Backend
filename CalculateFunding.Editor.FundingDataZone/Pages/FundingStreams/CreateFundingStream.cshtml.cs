using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CalculateFunding.Editor.FundingDataZone.Pages.FundingStreams
{
    public class CreateFundingStreamModel : PageModel
    {
        private readonly IPublishingAreaEditorRepository _repo;

        public CreateFundingStreamModel(IPublishingAreaEditorRepository publishingAreaEditorRepository)
        {
            _repo = publishingAreaEditorRepository;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            FundingStream fundingStream = new FundingStream()
            {
                FundingStreamName = Name,
                FundingStreamCode = Code,
            };

            await _repo.CreateFundingStream(fundingStream);

            return RedirectToPage("/FundingStreams/Index");
        }

        [BindProperty]
        [Required, StringLength(128)]
        [Display(Name = "Funding stream name")]
        public string Name { get; set; }

        [BindProperty]
        [Required, StringLength(10)]
        [Display(Name = "Funding stream code")]
        public string Code { get; set; }
    }
}
