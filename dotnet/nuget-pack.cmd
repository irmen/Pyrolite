@echo Running tests...
@call test-dotnet.cmd
if %errorlevel% neq 0 exit /b %errorlevel%

@echo.
@echo Building NuGet package...
nuget pack Pyrolite\Pyrolite.nuspec
