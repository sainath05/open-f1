using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi.Models;
using UndercutF1.Console.Audio;
using UndercutF1.Console.Graphics;
using UndercutF1.Data;

namespace UndercutF1.Console;

public static partial class CommandHandler
{
    public static async Task Root(
        bool isApiEnabled,
        DirectoryInfo? dataDirectory,
        DirectoryInfo? logDirectory,
        bool isVerbose,
        bool? notifyEnabled,
        bool? preferFfmpeg,
        GraphicsProtocol? forceGraphicsProtocol
    )
    {
        var builder = GetBuilder(
            isApiEnabled: isApiEnabled,
            dataDirectory: dataDirectory,
            logDirectory: logDirectory,
            isVerbose: isVerbose,
            notifyEnabled: notifyEnabled,
            preferFfmpeg: preferFfmpeg,
            forceGraphicsProtocol: forceGraphicsProtocol
        );

        builder
            .Services.AddSingleton<ConsoleLoop>()
            .AddSingleton<State>()
            .AddInputHandlers()
            .AddDisplays()
            .AddSingleton<INotifyHandler, NotifyHandler>()
            .AddSingleton<TerminalInfoProvider>()
            .AddSingleton<AudioPlayer>()
            .AddHostedService(sp => sp.GetRequiredService<ConsoleLoop>());

        var options = builder.Configuration.Get<Options>() ?? new();

        if (options.ApiEnabled)
        {
            builder.WebHost.UseKestrel(opt => opt.ListenAnyIP(0xF1F1)); // listens on 61937

            builder
                .Services.AddRouting()
                .AddEndpointsApiExplorer()
                .AddSwaggerGen(c =>
                {
                    c.CustomSchemaIds(type =>
                        type.FullName!.Replace("UndercutF1.Data.", string.Empty)
                            .Replace("+", string.Empty)
                    );
                    c.SwaggerDoc(
                        "v1",
                        new OpenApiInfo { Title = "Undercut F1 API", Version = "v1" }
                    );
                });
        }

        builder.Services.Configure<JsonOptions>(x =>
        {
            x.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            x.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        var app = builder.Build();

        if (options.ApiEnabled)
        {
            app.UseSwagger().UseSwaggerUI();

            app.MapSwagger();

            app.MapTimingEndpoints();
        }

        app.Logger.LogDebug("Options: {Options}", options);

        Whisper.net.Logger.LogProvider.AddLogger(
            (level, msg) =>
            {
                switch (level)
                {
                    case Whisper.net.Logger.WhisperLogLevel.Error:
                        app.Logger.LogError("Whisper: {Message}", msg?.Trim('\n'));
                        break;
                    case Whisper.net.Logger.WhisperLogLevel.Warning:
                        app.Logger.LogWarning("Whisper: {Message}", msg?.Trim('\n'));
                        break;
                    case Whisper.net.Logger.WhisperLogLevel.Debug:
                        app.Logger.LogDebug("Whisper: {Message}", msg?.Trim('\n'));
                        break;
                    default:
                        app.Logger.LogDebug("Whisper {Level}: {Message}", level, msg?.Trim('\n'));
                        break;
                }
            }
        );

        await app.RunAsync();
    }
}
