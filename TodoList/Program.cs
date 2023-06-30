using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TodoList;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiUri = builder.Configuration["ApiUri"] ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddScoped(sp => 
     new HttpClient 
     {
          BaseAddress = new Uri(apiUri)
     });

await builder.Build().RunAsync();
