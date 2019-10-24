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
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Sannel.House.Web;
using System;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Sannel.House.Gateway
{
	public class Startup
	{
		public Startup(IConfiguration configuration) => this.Configuration = configuration;

		public IConfiguration Configuration { get; }


		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
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



			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
			services.AddHealthChecks();

			services.AddOcelot();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public async void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider provider)
		{
			provider.CheckAndInstallTrustedCertificate();

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseDatabaseErrorPage();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
				app.UseHsts();
			}

			//app.UseHttpsRedirection();
			app.UseHealthChecks("/health");
			app.UseStaticFiles();
			app.UseCookiePolicy();

			app.UseMvc();

			await app.UseOcelot();
		}
	}
}
