﻿using System;
using System.IO;
using System.Net;
using System.Security.Authentication;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace PresenceLight.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
        private static void ConfigureConfiguration(IConfigurationBuilder config)
        {
            config.AddEnvironmentVariables()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("PresenceLightSettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            IConfigurationBuilder configBuilderForMain = new ConfigurationBuilder();
            ConfigureConfiguration(configBuilderForMain);
            IConfiguration configForMain = configBuilderForMain.Build();

            return Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                    .ConfigureKestrel(options =>
                     {
                         if (Convert.ToBoolean(configForMain["DeployedToServer"]))
                         {
                             if (string.IsNullOrEmpty(configForMain["ServerIP"]) || configForMain["ServerIP"] != "192.168.86.27")
                             {
                                 throw new ArgumentException("Supplied Server Ip Address is not configured or it is not in list of redirect Uris for Azure Active Directory");
                             }

                             options.Listen(System.Net.IPAddress.Parse(configForMain["ServerIP"]), 5001, listenOptions =>
                             {
                                 var envCertPath = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Path");
                                 if (string.IsNullOrEmpty(envCertPath))
                                 {
                                     // Cert Env Not provided, use appsettings
                                     //assumes cert is at same level as exe
                                     listenOptions.UseHttps(configForMain["Certificate:Name"], configForMain["Certificate:Password"]);
                                 }
                                 else
                                 { }
                             });

                             options.ListenLocalhost(5000, listenOptions =>
                             {
                             });
                         }
                         else
                         {
                             options.ListenLocalhost(5001, listenOptions =>
                             {
                                 var envCertPath = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Path");
                                 if (string.IsNullOrEmpty(envCertPath))
                                 {
                                     // Cert Env Not provided, use appsettings
                                     //assumes cert is at same level as exe
                                     listenOptions.UseHttps(configForMain["Certificate:Name"], configForMain["Certificate:Password"]);
                                 }
                                 else
                                 { }
                             });

                             options.ListenLocalhost(5000, listenOptions =>
                             {
                             });
                         }
                         if (Convert.ToBoolean(configForMain["DeployedToContainer"]))
                         {
                             options.ConfigureHttpsDefaults(listenOptions =>
                             {
                                 listenOptions.SslProtocols = SslProtocols.Tls12;
                             });
                         }
                     })
                     .UseContentRoot(Directory.GetCurrentDirectory());
                    webBuilder.UseStartup<Startup>();
                });
        }
    }
}
