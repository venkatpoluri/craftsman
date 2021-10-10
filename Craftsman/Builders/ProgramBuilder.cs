﻿namespace Craftsman.Builders
{
    using Craftsman.Exceptions;
    using Craftsman.Helpers;
    using System.IO.Abstractions;
    using System.Text;
    using static Helpers.ConstMessages;

    public class ProgramBuilder
    {
        public static void CreateWebApiProgram(string srcDirectory, string projectBaseName, IFileSystem fileSystem)
        {
            var classPath = ClassPathHelper.WebApiProjectRootClassPath(srcDirectory, $"Program.cs", projectBaseName);
            var fileText = GetWebApiProgramText(classPath.ClassNamespace);
            Utilities.CreateFile(classPath, fileText, fileSystem);
        }
        
        public static void CreateAuthServerProgram(string projectDirectory, string authServerProjectName, IFileSystem fileSystem)
        {
            var classPath = ClassPathHelper.WebApiProjectRootClassPath(projectDirectory, $"Program.cs", authServerProjectName);
            var fileText = GetWebApiProgramText(classPath.ClassNamespace);
            Utilities.CreateFile(classPath, fileText, fileSystem);
        }

        public static string GetWebApiProgramText(string classNamespace)
        {
            return @$"namespace {classNamespace}
{{
    using Autofac.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Serilog;
    using System;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;

    public class Program
    {{
        public async static Task Main(string[] args)
        {{
            var host = CreateHostBuilder(args).Build();

            using var scope = host.Services.CreateScope();

            //Read configuration from appSettings
            var services = scope.ServiceProvider;
            var hostEnvironment = services.GetService<IWebHostEnvironment>();
            var config = new ConfigurationBuilder()
                .AddJsonFile(""appsettings.json"")
                .AddJsonFile($""appsettings.{{hostEnvironment.EnvironmentName}}.json"", true)
                .Build();

            //Initialize Logger
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .CreateLogger();

            try
            {{
                Log.Information(""Starting application"");
                await host.RunAsync();
            }}
            catch (Exception e)
            {{
                Log.Error(e, ""The application failed to start correctly"");
                throw;
            }}
            finally
            {{
                Log.Information(""Shutting down application"");
                Log.CloseAndFlush();
            }}
        }}

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(webBuilder =>
                {{
                    webBuilder.UseStartup(typeof(Startup).GetTypeInfo().Assembly.FullName)
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseKestrel();
                }});
    }}
}}";
        }

        
        public static string GetAuthServerProgramText(string classNamespace)
        {
            return @$"{DuendeDisclosure}namespace {classNamespace}
{{
    using Autofac.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Serilog.Sinks.SystemConsole.Themes;
    using Serilog;
    using System;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;

    public class Program
    {{
        public async static Task Main(string[] args)
        {{
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override(""Microsoft"", LogEventLevel.Warning)
                .MinimumLevel.Override(""Microsoft.Hosting.Lifetime"", LogEventLevel.Information)
                .MinimumLevel.Override(""System"", LogEventLevel.Warning)
                .MinimumLevel.Override(""Microsoft.AspNetCore.Authentication"", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: ""[{{Timestamp:HH:mm:ss}} {{Level}}] {{SourceContext}}{{NewLine}}{{Message:lj}}{{NewLine}}{{Exception}}{{NewLine}}"", theme: AnsiConsoleTheme.Code)
                .CreateLogger();

            var host = CreateHostBuilder(args).Build();

            try
            {{
                Log.Information(""Starting application"");
                await host.RunAsync();
            }}
            catch (Exception e)
            {{
                Log.Error(e, ""The application failed to start correctly"");
                throw;
            }}
            finally
            {{
                Log.Information(""Shutting down application"");
                Log.CloseAndFlush();
            }}
        }}

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {{
                    webBuilder.UseStartup(typeof(Startup).GetTypeInfo().Assembly.FullName)
                }});
    }}
}}";
        }
    }
}