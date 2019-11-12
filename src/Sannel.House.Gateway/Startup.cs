/* Copyright 2018 Sannel Software, L.L.C.
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at
      http://www.apache.org/licenses/LICENSE-2.0
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.*/

using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Sannel.House.Web;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Sannel.House.Gateway
{
	public class Startup
	{
		public Startup(IConfiguration configuration, ILogger<Startup> logger)
		{
			this.Configuration = configuration;
			this.logger = logger;
		}

		public IConfiguration Configuration { get; }
		private ILogger logger { get; }


		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddCors(i =>
			{
				i.AddDefaultPolicy(p =>
				{
					var origins = Configuration.GetSection("Cors:Origins").Get<string[]>();
					if (string.Compare(origins?.FirstOrDefault(), "any", true, CultureInfo.InvariantCulture) == 0)
					{
						logger.LogInformation("Allowing any Origin");
						p.AllowAnyOrigin();
					}
					else if(origins != null)
					{
						logger.LogInformation($"Allowing Origins:{Environment.NewLine}{string.Join(Environment.NewLine, origins)}");
						p.WithOrigins(origins);
					}

					var headers = Configuration.GetSection("Cors:Headers").Get<string[]>();
					if(string.Compare(headers?.FirstOrDefault(), "any", true, CultureInfo.InvariantCulture) == 0)
					{
						logger.LogInformation("Allow any header");
						p.AllowAnyHeader();
					}
					else if(headers != null)
					{
						logger.LogInformation($"Allow Headers:{Environment.NewLine}{string.Join(Environment.NewLine, headers)}");
						p.WithHeaders(headers);
					}

					var methods = Configuration.GetSection("Cors:Methods").Get<string[]>();
					if(string.Compare(methods?.FirstOrDefault(), "any", true, CultureInfo.InvariantCulture) == 0)
					{
						logger.LogInformation("Allow any Method");
						p.AllowAnyMethod();
					}
					else if(methods != null)
					{
						logger.LogInformation($"Allow Methods:{Environment.NewLine}{string.Join(Environment.NewLine, methods)}");
						p.WithMethods(methods);
					}

					var wildcard = Configuration.GetSection("Cors:AllowWildCardDomains").Get<bool?>();
					if(wildcard == true)
					{
						logger.LogInformation("Allowing wild card domains");
						p.SetIsOriginAllowedToAllowWildcardSubdomains();
					}

					var allowCredentials = Configuration.GetSection("Cors:AllowCredentials").Get<bool?>();
					if(allowCredentials == true)
					{
						logger.LogInformation("Allowing Credentials");
						p.AllowCredentials();
					}
					else
					{
						logger.LogInformation("Disallowing Credentials");
						p.DisallowCredentials();
					}
				});
			});

			services.AddAuthentication()
				.AddIdentityServerAuthentication(this.Configuration["Authentication:Schema"], o =>
				{
					o.Authority = this.Configuration["Authentication:AuthorityUrl"];
					o.ApiName = this.Configuration["Authentication:ApiName"];
					o.SupportedTokens = SupportedTokens.Both;
					if(!string.IsNullOrWhiteSpace(this.Configuration["Authentication:ApiSecret"]))
					{
						o.ApiSecret = this.Configuration["Authentication:ApiSecret"];
					}

					if (this.Configuration.GetValue<bool?>("Authentication:DisableRequireHttpsMetadata") == true)
					{
						o.RequireHttpsMetadata = false;
					}
				});

			services.AddHealthChecks();

			services.AddOcelot();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public async void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider provider)
		{
			provider.CheckAndInstallTrustedCertificate();

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
			}

			//app.UseHttpsRedirection();
			app.UseHealthChecks("/health");
			app.UseCors();

			await app.UseOcelot();
		}
	}
}
