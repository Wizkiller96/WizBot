using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NadekoBot.VotesApi.Services;

namespace NadekoBot.VotesApi
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
            services.AddControllers();
            services.AddSingleton<IVotesCache, FileVotesCache>();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "NadekoBot.VotesApi", Version = "v1" });
            });

            services
                .AddAuthentication(opts =>
                {
                    opts.DefaultScheme = AuthHandler.SchemeName;
                    opts.AddScheme<AuthHandler>(AuthHandler.SchemeName, AuthHandler.SchemeName);
                });
            
            services
                .AddAuthorization(opts =>
                {
                    opts.DefaultPolicy = new AuthorizationPolicyBuilder(AuthHandler.SchemeName)
                        .RequireAssertion(x => false)
                        .Build();
                    opts.AddPolicy(Policies.DiscordsAuth, policy => policy.RequireClaim(AuthHandler.DiscordsClaim));
                    opts.AddPolicy(Policies.TopggAuth, policy => policy.RequireClaim(AuthHandler.TopggClaim));
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NadekoBot.VotesApi v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}