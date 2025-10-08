using CLDVPOE.Models;
using CLDVPOE.Services;
using Microsoft.AspNetCore.Mvc;

namespace CLDVPOE.Controllers
{
    public class UploadController : Controller
    {
        private readonly IFunctionsApi _api;

        public UploadController(IFunctionsApi api) => _api = api;

        public IActionResult Index() => View(new FileUploadModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                if (model.ProofOfPayment is null || model.ProofOfPayment.Length == 0)
                {
                    ModelState.AddModelError("ProofOfPayment", "Please select a file to upload.");
                    return View(model);
                }

                var fileName = await _api.UploadProofOfPaymentAsync(
                    model.ProofOfPayment,
                    model.OrderId,
                    model.CustomerName
                );

                TempData["Success"] = $"File uploaded successfully! File name: {fileName}";
                return View(new FileUploadModel());
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"File upload failed: {ex.Message}";
                return View(model);
            }
        }
    }
}
