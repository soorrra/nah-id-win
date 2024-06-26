﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using GMap.NET.MapProviders;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Petsitter.Controllers;
using Petsitter.Data.Services;
using Petsitter.Models;
using Petsitter.Repositories;
using Petsitter.ViewModels;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;
//using static Petsitter.Services.ReCAPTCHA;

namespace Petsitter.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {

        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly PetsitterContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            PetsitterContext context,
            IWebHostEnvironment webHost,
            IEmailService emailService,
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
            webHostEnvironment = webHost;
            _emailService = emailService;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            /// 

            [Display(Name = "Имя")]
            [Required(ErrorMessage = "Пожалуйста, укажите имя.")]
            public string FirstName { get; set; }

            [Display(Name = "Фамилия")]
            [Required(ErrorMessage = "Пожалуйста, укажите фамилию.")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "Пожалуйста, укажите адрес электронной почты.")]
            [EmailAddress(ErrorMessage = "Неверный формат адреса электронной почты.")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Пожалуйста, укажите номер телефона.")]
            [Phone(ErrorMessage = "Неверный формат номера телефона.")]
            [Display(Name = "Номер телефона")]
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage = "Пожалуйста, укажите город.")]
            [Display(Name = "Город")]
            public string City { get; set; }

            [Required(ErrorMessage = "Пожалуйста, укажите почтовый индекс.")]
            [RegularExpression("^[A-Za-z]\\d[A-Za-z][ ]?\\d[A-Za-z]\\d$", ErrorMessage = "Пожалуйста, введите действительный почтовый индекс в формате A1A 1A1.")]
            [Display(Name = "Почтовый индекс")]
            [MaxLength(7)]
            public string PostalCode { get; set; }

            [Required(ErrorMessage = "Пожалуйста, укажите адрес.")]
            [Display(Name = "Адрес")]
            public string StreetAddress { get; set; }

            [Required(ErrorMessage = "Пожалуйста, укажите тип аккаунта.")]
            [Display(Name = "Тип аккаунта")]
            public string UserType { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "Пожалуйста, укажите пароль.")]
            [StringLength(100, ErrorMessage = "{0} должен содержать не менее {2} и не более {1} символов.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Пароль")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Подтвердите пароль")]
            [Compare("Password", ErrorMessage = "Пароль и его подтверждение не совпадают.")]
            public string ConfirmPassword { get; set; }
        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ViewData["SiteKey"] = _configuration["Recaptcha:SiteKey"];

            // Ensure ReCaptcha doesn't disappear after invalid attempt
            Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Add("Pragma", "no-cache");
            Response.Headers.Add("Expires", "0");
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            string captchaResponse = Request.Form["g-Recaptcha-Response"];
            string secret = _configuration["Recaptcha:SecretKey"];
            //ReCaptchaValidationResult resultCaptcha =
            //  ReCaptchaValidator.IsValid(secret, captchaResponse);

            // Invalidate the form if the captcha is invalid.
            //if (!resultCaptcha.Success)
            //{
            //    ModelState.AddModelError(string.Empty,
            //        "The ReCaptcha is invalid.");
            //}

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);
                var defaultImageFilePath = Path.Combine(webHostEnvironment.WebRootPath, "images/default-image.jpg");
                byte[] defaultImageBytes;
                using (var fileStream = new FileStream(defaultImageFilePath, FileMode.Open))
                {
                    using var binaryReader = new BinaryReader(fileStream);
                    defaultImageBytes = binaryReader.ReadBytes((int)fileStream.Length);
                }
                // store in AspNetUserRole table once user sign up as either sitter or customer
                var UserManager = _serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var findUser = await UserManager.FindByEmailAsync(Input.Email);

                if (result.Succeeded)
                {
                    User newUser = new User()
                    {
                        FirstName = Input.FirstName,
                        LastName = Input.LastName,
                        Email = Input.Email,
                        PhoneNumber = Input.PhoneNumber,
                        City = Input.City,
                        PostalCode = Input.PostalCode,
                        StreetAddress = Input.StreetAddress,
                        UserType = Input.UserType,
                        ProfileImage = defaultImageBytes
                    };

                    if (findUser != null)
                    {
                        await UserManager.AddToRoleAsync(findUser, newUser.UserType);
                    }

                    if (newUser.UserType == "Sitter")
                    {
                        SitterRepos sitterRepos = new SitterRepos(_context, webHostEnvironment);
                        CustomerRepo customerRepo = new CustomerRepo(_context, webHostEnvironment);

                        customerRepo.AddUser(newUser);

                        var customerID = customerRepo.GetCustomerId(Input.Email);

                        Sitter newSitter = new Sitter()
                        {
                            UserId = customerID.UserId,
                            RatePerPetPerDay = 200

                        };
                        sitterRepos.AddSiter(newSitter);

                        var sitterID = sitterRepos.GetSitterByEmail(Input.Email);
                        HttpContext.Session.SetString("Email", Input.Email);
                        HttpContext.Session.SetString("UserName", customerID.FirstName);
                        HttpContext.Session.SetString("UserID", customerID.UserId.ToString());
                        HttpContext.Session.SetString("SitterID", sitterID.SitterId.ToString());

                    }
                    else if (newUser.UserType == "Customer")
                    {
                        CustomerRepo customerRepo = new CustomerRepo(_context, webHostEnvironment);

                        customerRepo.AddUser(newUser);
                        var customerID = customerRepo.GetCustomerId(Input.Email);

                        // assign roles

                        HttpContext.Session.SetString("UserName", customerID.FirstName);
                        HttpContext.Session.SetString("UserID", customerID.UserId.ToString());
                    }
                    // usertype == 'admin' can go here this will make more clean code structure in terms of user roles

                    _logger.LogInformation("User created a new account with password.");

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId, code, returnUrl = returnUrl },
                        protocol: Request.Scheme);


                    Mailrequest mailrequest = new();
                    mailrequest.ToEmail = newUser.Email; // Получить email пользователя
                    mailrequest.Subject = "Добро пожаловать!";
                    mailrequest.Body = $"Пожалуйста, подтвердите почту <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>нажав здесь</a>."; // Здесь ваше письмо приветствия

                    await _emailService.SendEmailAsync(mailrequest);

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new
                        {
                            email = Input.Email
                                                  ,
                            returnUrl = returnUrl
                                                  ,
                            DisplayConfirmAccountLink = false
                        });

                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }



                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}
