using System;
using System.Net;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using MediatR;
using Serilog;
using Prometheus;
using RabbitMQ.Client;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQCommon;
using RabbitMQCommon.Models;

/* Install Nuget packages:

    dotnet add package RabbitMQ.Client
    dotnet add package prometheus-net
    dotnet add package prometheus-net.AspNetCore
    dotnet add package serilog.aspnetcore
    dotnet add package serilog.expressions
    dotnet add package serilog.Settings.Configuration
    dotnet add package serilog.Settings.AppSettings
    dotnet add package serilog.sinks.Seq
    dotnet add package serilog.sinks.Debug
    dotnet add package serilog.sinks.Elasticsearch
    dotnet add package serilog.Exceptions
    dotnet add package serilog.Enrichers.Environment

    dotnet tool install Nuke.GlobalTool
    nuke :setup
    nuke :addpackage nuget.commandline
*/

namespace RabbitMQPublisher
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Init ConfigruationHelper for static classes
            ConfigurationHelper.Initialize(builder.Configuration);
            string RabbitMQHostName = ConfigurationHelper.config.GetSection("RabbitMQ")["Server"] ?? "localhost";
            string RabbitMQPort = ConfigurationHelper.config.GetSection("RabbitMQ")["Port"] ?? "5672";
            string RabbitMQUserName = ConfigurationHelper.config.GetSection("RabbitMQ")["Username"] ?? "guest";
            string RabbitMQPassword = ConfigurationHelper.config.GetSection("RabbitMQ")["Password"] ?? "guest";

            ConfigureLogging();
            // Add services to the container.
            builder.Host.UseSerilog();
            builder.Services.AddHealthChecks();
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "RabbitMQPublisher",
                    Description = "An ASP.NET Core Web API for Publishing (sending) string messages to RabbitMQ queue.",
                    Contact = new OpenApiContact
                    {
                        Name = "Michael Lucas",
                        Url = new Uri("mailto:mike@lucasnet.org")
                    },
                });
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

            });
            builder.Services.AddSingleton(serviceProvider =>
            {
                var uri = new Uri($"amqp://{RabbitMQUserName}:{RabbitMQPassword}@{RabbitMQHostName}:{RabbitMQPort}/");
                return new ConnectionFactory
                {
                    Uri = uri
                };
            });
            builder.Services.AddSingleton<IRabbitMqProducer<Message>, RabbitMqProducerBase<Message>>();
            //builder.Services.AddMediatR(Assembly.GetExecutingAssembly());

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapHealthChecks("/health");
            app.UseRouting();
            app.UseMetricServer();
            app.MapControllers();
            app.MapMetrics();
            app.Run();
            Log.Information("RabbitMQPublisherApi application started.");

            void ConfigureLogging()
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
                    .Build();

                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration)
                    .CreateLogger();

                Log.Debug("Starting logging....");
            }
        }
    }
}