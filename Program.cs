using DependencyAnalyzer.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IAnalysisService, JsonAnalysisService>();
builder.Services.AddSingleton<AnalysisJobStore>();
builder.Services.AddScoped<AnalysisEmailSender>();
builder.Services.AddHostedService<AnalysisWorker>();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Analysis/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute("default", "{controller=Analysis}/{action=Index}/{id?}");
app.Run();
