﻿// <copyright file="RazorViewRenderer.cs" company="Stormpath, Inc.">
// Copyright (c) 2016 Stormpath, Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Stormpath.Owin.Abstractions;
using Stormpath.SDK.Logging;

namespace Stormpath.AspNetCore
{
    public class RazorViewRenderer : IViewRenderer
    {
        private static readonly string MicrosoftHttpContextKey = "Microsoft.AspNetCore.Http.HttpContext";

        private readonly ICompositeViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        //private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public RazorViewRenderer(
            ICompositeViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider,
            ILogger logger)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            //_serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<bool> RenderAsync(string name, object model, IOwinEnvironment context, CancellationToken cancellationToken)
        {
            var httpContext = context.Request[MicrosoftHttpContextKey] as HttpContext;
            if (httpContext == null)
            {
                _logger.Error($"Request dictionary does not contain '{MicrosoftHttpContextKey}'", nameof(RazorViewRenderer));
                return false;
            }

            var actionContext = GetActionContext(httpContext);
            var viewEngineResult = _viewEngine.FindView(actionContext, name, false);

            if (!viewEngineResult.Success)
            {
                _logger.Trace($"Could not find Razor view '{name}'", nameof(RazorViewRenderer));
                return false;
            }

            var view = viewEngineResult.View;

            using (var writer = new StreamWriter(context.Response.Body))
            {
                var viewDataDictionary = new ViewDataDictionary(
                    new EmptyModelMetadataProvider(),
                    new ModelStateDictionary())
                {
                    Model = model
                };

                var viewContext = new ViewContext(
                    actionContext,
                    view,
                    viewDataDictionary,
                    new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                    writer,
                    new HtmlHelperOptions());

                cancellationToken.ThrowIfCancellationRequested();
                await view.RenderAsync(viewContext);

                return true;
            }
        }

        private static ActionContext GetActionContext(HttpContext httpContext)
            => new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
    }
}
