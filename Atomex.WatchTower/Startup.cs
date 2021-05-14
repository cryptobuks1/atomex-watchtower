using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Atomex.Contexts;
using Atomex.Core;
using Atomex.WatchTower.Services;
using Atomex.WatchTower.Services.Abstract;
using Atomex.Abstract;

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

            var network = Enum.Parse<Network>(Configuration["Network"], true);

            var currenciesSettingsSection = Configuration.GetSection("Currencies");
            services.AddSingleton<ICurrenciesProvider>(services => new CurrenciesProvider(currenciesSettingsSection));
            services.AddSingleton(services => services.GetService<ICurrenciesProvider>().GetCurrencies(network));

            var blockchainSettingsSection = Configuration.GetSection("Blockchain");
            services.Configure<BlockchainSettings>(blockchainSettingsSection);

            services.AddSingleton<IBlockchainService, BlockchainService>();

            var trackerSettingsSection = Configuration.GetSection("Tracker");
            services.Configure<TrackerSettings>(trackerSettingsSection);
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
