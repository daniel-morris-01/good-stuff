if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error-local-development");
}
else
{
    app.UseExceptionHandler("/error");
}