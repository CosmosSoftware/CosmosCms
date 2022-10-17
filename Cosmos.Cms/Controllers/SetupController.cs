using AspNetCore.Identity.Services.SendGrid;
using Cosmos.BlobService.Config;
using Cosmos.Cms.Common.Data;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Cms.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Cosmos.Cms.Controllers
{
    /// <summary>
    /// Controller used for Cosmos setup.
    /// </summary>
    public class SetupController : Controller
    {
        private readonly ILogger<SetupController> _logger;
        private readonly IOptions<CosmosConfig> _options;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SendGridEmailSender _emailSender;
        private readonly ApplicationDbContext _dbContext;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="dbContext"></param>
        /// <param name="options"></param>
        /// <param name="roleManager"></param>
        /// <param name="userManager"></param>
        /// <param name="emailSender"></param>
        public SetupController(ILogger<SetupController> logger,
            ApplicationDbContext dbContext,
            IOptions<CosmosConfig> options,
            RoleManager<IdentityRole> roleManager,
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender
        )
        {
            _dbContext = dbContext;

            if (options.Value.SiteSettings.AllowSetup ?? false)
            {
                _logger = logger;
                _options = options;
                _roleManager = roleManager;
                _userManager = userManager;
                _emailSender = (SendGridEmailSender)emailSender;
            }
            else
            {
                throw new UnauthorizedAccessException("Setup mode disabled.");
            }
        }

        /// <summary>
        /// Settings index page
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            return View(settings);
        }

        /// <summary>
        /// Setup index page
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Installation()
        {
            if (_options.Value.SiteSettings.AllowSetup ?? false)
            {
                // Setup roles if they don't already exist. //
                if (!await _roleManager.RoleExistsAsync("Administrators"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Administrators"));
                }
                // Setup roles if they don't already exist. //
                if (!await _roleManager.RoleExistsAsync("User Administrators"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("User Administrators"));
                }
                if (!await _roleManager.RoleExistsAsync("Editors"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Editors"));
                }
                if (!await _roleManager.RoleExistsAsync("Authors"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Authors"));
                }
                if (!await _roleManager.RoleExistsAsync("Reviewers"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Reviewers"));
                }
                return View();
            }
            return Unauthorized();
        }

        /// <summary>
        /// Register administrative user
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Installation(InstallationViewModel model)
        {
            if (_options.Value.SiteSettings.AllowSetup ?? false && (await _userManager.Users.CountAsync()) == 0)
            {
                if (ModelState.IsValid)
                {
                    var user = new IdentityUser { UserName = model.AdminEmail, Email = model.AdminEmail };
                    var result = await _userManager.CreateAsync(user, model.Password);

                    foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);

                    if (result.Succeeded)
                    {

                        var roleResult = await _userManager.AddToRolesAsync(user, new[] { "Administrators", "User Administrators" });

                        foreach (var error in roleResult.Errors) ModelState.AddModelError(string.Empty, error.Description);

                        if (roleResult.Succeeded)
                        {
                            _logger.LogInformation("Administrator account created with password.");
                        }


                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        //code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                        var confirmResult = await _userManager.ConfirmEmailAsync(user, code);

                        foreach (var error in confirmResult.Errors) ModelState.AddModelError(string.Empty, error.Description);

                        await _emailSender.SendEmailAsync(model.AdminEmail, "Admin Account Created",
                            $"An administrator account was just created.");

                        if (_emailSender.Response.IsSuccessStatusCode && ModelState.IsValid)
                        {
                            return RedirectToAction("AccountCreated");
                        }

                        ModelState.AddModelError(string.Empty, $"SendGrid error code: { _emailSender.Response.StatusCode }.");
                    }

                }
                return View();
            }

            return Unauthorized();
        }

        /// <summary>
        /// Administrator account created
        /// </summary>
        /// <returns></returns>
        public IActionResult AccountCreated()
        {
            return View();
        }

    }
}