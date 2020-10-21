using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atomex.Contexts;
using Atomex.Guard.Services;
using Atomex.Guard.Services.Abstract;
using Atomex.Services;
using Atomex.Services.Abstract;
using Atomex.WatchTower.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Atomex.WatchTower
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

            services.AddDbContext<ExchangeContext>(
                optionsAction: options => options
                    .UseNpgsql(Configuration.GetConnectionString("AtomexDb"),
                               options => options.EnableRetryOnFailure()),
                contextLifetime: ServiceLifetime.Transient,
                optionsLifetime: ServiceLifetime.Singleton);

            services.AddSingleton<IDataRepository, DataRepository>();

            var currenciesSettingsSection = Configuration.GetSection("Currencies");
            services.Configure<CurrenciesSettings>(currenciesSettingsSection);

            services.AddSingleton<ICurrenciesProvider, CurrenciesProvider>();

            var blockchainSettingsSection = Configuration.GetSection("Blockchain");
            services.Configure<BlockchainSettings>(blockchainSettingsSection);

            services.AddSingleton<IBlockchainService, BlockchainService>();
            services.AddSingleton<RedeemService>();
            services.AddSingleton<RefundService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
