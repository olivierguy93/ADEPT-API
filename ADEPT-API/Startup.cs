using ADEPT_API.Context;
using ADEPT_API.Dto;
using ADEPT_API.Middleware;
using ADEPT_API.Repositories.IRepository;
using ADEPT_API.Repositories.Repository;
using ADEPT_API.Services;
using ADEPT_API.Services.Service;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ADEPT_API
{
    public class Startup
    {

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            AddServicesAndRepositories(services);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
             .AddJwtBearer(x =>
            {
                x.Authority = "https://securetoken.google.com/adept-api";
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "https://securetoken.google.com/adept-api",
                    ValidateAudience = true,
                    ValidAudience = "adept-api",
                    ValidateLifetime = true
                };
            });

            services.AddControllers();

            services.AddDbContext<AdeptContext>(options =>
            {
                options.UseSqlServer(AdeptConfig.Get("AppSettings:connectionString"));
                options.UseLazyLoadingProxies();
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ADEPT_API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public async void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ADEPT_API v1"));
            }

            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile("firebase-auth.json"),
                ProjectId = "adept-api"
            });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseMiddleware<AuthenticationMiddleWare>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            string customToken =  await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync("urOxnFybSTVo4Nai5rWuQuYWJAA3");
            await SignInWithCustomTokenAsync(customToken);
        }

        private static async Task SignInWithCustomTokenAsync(string customToken)
        {
            string apiKey = AdeptConfig.Get("AppSettings:firebaseApiKey");
            using(HttpClient client = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(new
                {
                    token = customToken,
                    returnSecureToken = true
                });
                var data = new StringContent(json, Encoding.UTF8, "application/json");



                var response = await client.PostAsync("https://identitytoolkit.googleapis.com/v1/accounts:signInWithCustomToken?key=" + apiKey, data);

                dynamic kekw = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                AdeptConfig.TestToken = kekw.idToken;

                //Arr�tez sur la ligne suivante pour obtenir l'idToken pour test
            }
        }

        private void AddServicesAndRepositories(IServiceCollection services)
        {
            // Auth
            services.AddTransient<IAuthService, AuthService>();
            services.AddTransient<IAuthRepository, AuthRepository>();
        }
    }
}
