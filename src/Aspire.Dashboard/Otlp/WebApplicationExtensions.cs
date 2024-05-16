// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Authentication;
using Aspire.Dashboard.Otlp.Grpc;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Aspire.Dashboard.Otlp;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigureHttpOtlp(this WebApplication app, Uri httpEndpoint)
    {
        app.MapPost("/v1/metrics",
                async ([FromServices] OtlpMetricsService service, HttpContext httpContext) =>
                {
                    var body = await ReadFullyAsync(httpContext).ConfigureAwait(false);
                    var metrics = ExportMetricsServiceRequest.Parser.ParseFrom(body);
                    await service.Export(metrics, null!).ConfigureAwait(false);
                }
            ).RequireAuthorization(OtlpAuthorization.PolicyName)
            .RequireHost($"*:{httpEndpoint.Port}");

        app.MapPost("/v1/traces",
                async ([FromServices] OtlpTraceService service, HttpContext httpContext) =>
                {
                    var body = await ReadFullyAsync(httpContext).ConfigureAwait(false);
                    var traces = ExportTraceServiceRequest.Parser.ParseFrom(body);
                    await service.Export(traces, null!).ConfigureAwait(false);
                }
            ).RequireAuthorization(OtlpAuthorization.PolicyName)
            .RequireHost($"*:{httpEndpoint.Port}");

        app.MapPost("/v1/logs",
                async ([FromServices] OtlpLogsService service, HttpContext httpContext) =>
                {
                    var body = await ReadFullyAsync(httpContext).ConfigureAwait(false);
                    var logs = ExportLogsServiceRequest.Parser.ParseFrom(body);
                    await service.Export(logs, null!).ConfigureAwait(false);
                }
            ).RequireAuthorization(OtlpAuthorization.PolicyName)
            .RequireHost($"*:{httpEndpoint.Port}");

        return app;
    }

    private static async Task<byte[]> ReadFullyAsync(HttpContext context)
    {
        using var ms = new MemoryStream();
        await context.Request.Body.CopyToAsync(ms).ConfigureAwait(false);
        return ms.ToArray();
    }
}
