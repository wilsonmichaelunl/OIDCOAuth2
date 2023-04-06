﻿using Duende.IdentityServer;
using Duende.IdentityServer.Services;
using IdentityModel;
using Marvin.IDP.Entities;
using Marvin.IDP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Marvin.IDP.Pages.User.Registration;

[AllowAnonymous]
[SecurityHeaders]
public class Index : PageModel
{
    private readonly ILocalUserService _localUserService;
    private readonly IIdentityServerInteractionService _interactionService;

    public Index(ILocalUserService localUserService, IIdentityServerInteractionService interactionService)
    {
        _localUserService = localUserService;
        _interactionService = interactionService;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public IActionResult OnGet(string returnUrl)
    {
        BuildModel(returnUrl);

        return Page();
    }

    private void BuildModel(string returnUrl)
    {
        Input = new InputModel
        {
            ReturnUrl = returnUrl
        };
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            BuildModel(Input.ReturnUrl);
            return Page();
        }

        var userToCreate = new Entities.User
        {
            UserName = Input.UserName,
            Subject = Guid.NewGuid().ToString(),
            Active = true
        };

        userToCreate.Claims.Add(new UserClaim()
        {
            Type = "country",
            Value = Input.Country
        });

        userToCreate.Claims.Add(new UserClaim()
        {
            Type = JwtClaimTypes.GivenName,
            Value = Input.GivenName
        });

        userToCreate.Claims.Add(new UserClaim()
        {
            Type = JwtClaimTypes.FamilyName,
            Value = Input.FamilyName
        });

        _localUserService.AddUser(userToCreate, Input.Password);
        await _localUserService.SaveChangesAsync();

        // Issue authentication cookie (log the user in)
        var isUser = new IdentityServerUser(userToCreate.Subject)
        {
            DisplayName = userToCreate.UserName
        };

        await HttpContext.SignInAsync(isUser);

        // continue with the flow
        if (_interactionService.IsValidReturnUrl(Input.ReturnUrl) || Url.IsLocalUrl(Input.ReturnUrl))
        {
            return Redirect(Input.ReturnUrl);
        }

        return Redirect("~/");
    }
}