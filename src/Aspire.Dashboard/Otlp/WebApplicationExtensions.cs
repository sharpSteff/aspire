// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
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
                    await ExportOtlpData(httpContext, sequence => service.Export(ExportMetricsServiceRequest.Parser.ParseFrom(sequence.IsSingleSegment ? sequence.First.Span : sequence.ToArray().AsSpan()), null!)).ConfigureAwait(false);
                }
            ).RequireAuthorization(OtlpAuthorization.PolicyName)
            .RequireHost($"*:{httpEndpoint.Port}");

        app.MapPost("/v1/traces",
                async ([FromServices] OtlpTraceService service, HttpContext httpContext) =>
                {
                    await ExportOtlpData(httpContext, sequence => service.Export(ExportTraceServiceRequest.Parser.ParseFrom(sequence.IsSingleSegment ? sequence.First.Span : sequence.ToArray().AsSpan()), null!)).ConfigureAwait(false);
                }
            ).RequireAuthorization(OtlpAuthorization.PolicyName)
            .RequireHost($"*:{httpEndpoint.Port}");

        app.MapPost("/v1/logs",
                async ([FromServices] OtlpLogsService service, HttpContext httpContext) =>
                {
                    await ExportOtlpData(httpContext, sequence => service.Export(ExportLogsServiceRequest.Parser.ParseFrom(sequence.IsSingleSegment ? sequence.First.Span : sequence.ToArray().AsSpan()), null!)).ConfigureAwait(false);
                }
            ).RequireAuthorization(OtlpAuthorization.PolicyName)
            .RequireHost($"*:{httpEndpoint.Port}");

        return app;
    }

    private static async Task ExportOtlpData(HttpContext httpContext, Func<ReadOnlySequence<byte>, Task> exporter)
    {
        var reader = httpContext.Request.BodyReader;
        var readResult = await reader.ReadAsync().ConfigureAwait(false);
        var readResultBuffer = readResult.Buffer;
        if (readResultBuffer.IsEmpty && readResult.IsCompleted)
        {
            return;
        }

        await exporter(readResultBuffer).ConfigureAwait(false);
        reader.AdvanceTo(readResultBuffer.Start, readResultBuffer.End);
        await reader.CompleteAsync().ConfigureAwait(false);
    }
}
