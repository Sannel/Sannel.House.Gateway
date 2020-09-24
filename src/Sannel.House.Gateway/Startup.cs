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
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Newtonsoft.Json.Linq;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Sannel.House.Base.Web;
using Sannel.House.Gateway.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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

			var health = services.AddHealthChecks();

			var uris = Configuration.GetSection("HealthChecks").Get<Uri[]>();
			if(uris != null)
			{
				health.AddWebRequestsHealthChecks(uris);
			}

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
			app.UseCors();
			app.UseRouting();

			var endpoints = Configuration.GetSection("OpenApi:EndPoints").Get<EndpointInfo[]>();

			if (Configuration.GetValue<bool>("OpenApi:EnableSwaggerUI"))
			{
				app.UseSwaggerUi3(i =>
				{
					i.DocumentPath = $"/swagger/{Configuration["OpenApi:VersionString"]}/swagger.json";
				});
			}

			app.UseEndpoints(i =>
			{
				i.MapHouseHealthChecks("/health");
				i.MapHouseRobotsTxt();

				if (Configuration.GetValue<bool>("OpenApi:EnableSwaggerUI"))
				{
					var client = new HttpClient();
					i.MapGet($"/swagger/{Configuration["OpenApi:VersionString"]}/swagger.json", async context =>
					{
						var root = new JObject
						{
							new JProperty("openapi", "3.0.0"),
							new JProperty("info", new JObject(
								new JProperty("title", "House Api"),
								new JProperty("version", "1.0.0")
							)),

							new JProperty("servers", new JArray(
								new JObject(
									new JProperty("url", $"{context.Request.Scheme}://{context.Request.Host.Host}:{context.Request.Host.Port}")
								)
							))
						};

						var paths = new JObject();
						root.Add(new JProperty("paths", paths));

						var components = new JObject();
						root.Add(new JProperty("components", components));

						var schemas = new JObject();
						components.Add(new JProperty("schemas", schemas));

						foreach(var endpoint in endpoints)
						{
							var response = await client.GetAsync(endpoint.Path);
							var content = await response.Content.ReadAsStringAsync();

							var top = JObject.Parse(content);
							var childPaths = top.SelectToken("paths");
							
							foreach(JProperty child in childPaths.Children())
							{
								var path = child.Name;

								foreach(var replacement in endpoint.Rewrite)
								{
									path = path.Replace(replacement.OldPath, replacement.NewPath);
								}
								paths.Add(new JProperty(path, child.Value));
							}

							var childSchemas = top.SelectToken("components.schemas");

							foreach(JProperty schema in childSchemas)
							{
								if (schemas.SelectToken(schema.Name) is null)
								{
									schemas.Add(schema);
								}
							}
						}

						await context.Response.WriteJsonAsync(root.ToString());
					});
				}
			});

			IdentityModelEventSource.ShowPII = true;
			await app.UseOcelot();
		}

	}
}
