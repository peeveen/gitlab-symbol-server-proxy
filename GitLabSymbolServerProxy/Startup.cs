using Opw.HttpExceptions.AspNetCore;

namespace GitLabSymbolServerProxy;

public class Startup {
	public IConfiguration Configuration { get; }
	public bool IsDevelopment { get; }

	public Startup(IConfiguration configuration, IWebHostEnvironment env) {
		Configuration = configuration;
		IsDevelopment = env.IsDevelopment();
	}

	// This method gets called by the runtime. Use this method to add services to the container.
	public void ConfigureServices(IServiceCollection services) {
		var proxyConfig = new ProxyConfig(Configuration);
		var pdbStore = new FileSystemSymbolStore(proxyConfig);
		var pdbCache = new SymbolCache(pdbStore);

		services.AddSingleton<IProxyConfig>(proxyConfig);
		services.AddSingleton<ISymbolStore>(pdbStore);
		services.AddSingleton<ISymbolCache>(pdbCache);
		services.AddScoped<ISnupkgSource, GitLabSnupkgSource>();

		services.AddHttpClient<IGitLabClient, GitLabClient>(client => {
			client.BaseAddress = new Uri(proxyConfig.GitLabHostOrigin);
		});
		services.AddControllersWithViews();
		services.AddMvc().AddHttpExceptions(options => {
			// Always include exception details in dev mode.
			// Otherwise simple message will do.
			options.IncludeExceptionDetails = context => IsDevelopment;
		});
		services.AddRouting();
	}

	// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
	public void Configure(IApplicationBuilder app) {
		app.UseHttpExceptions();
		app.UseRouting();
		if (IsDevelopment)
			// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
			app.UseHsts();
		app.UseEndpoints(endpoints => {
			endpoints.MapDefaultControllerRoute();
		});
	}
}
