﻿using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;

namespace Stormpath.AspNetCore.DocExamples.Controllers
{
    #region code/csharp/authentication/protected_route.cs
    [Authorize]
    public class ProfileController : Controller
    {
        // GET: /profile
        public IActionResult Index()
        {
            // [Authorize] will require a logged-in user for this action
            return View();
        }
    }
    #endregion
}
