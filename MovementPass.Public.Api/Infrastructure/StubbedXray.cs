namespace MovementPass.Public.Api.Infrastructure;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Context;
using Amazon.XRay.Recorder.Core.Internal.Emitters;
using Amazon.XRay.Recorder.Core.Sampling;
using Amazon.XRay.Recorder.Core.Strategies;

public sealed class StubbedXray : IAWSXRayRecorder
{
    public string Origin { get; set; }

    public ISamplingStrategy SamplingStrategy { get; set; }

    public IStreamingStrategy StreamingStrategy { get; set; }

    public ContextMissingStrategy ContextMissingStrategy { get; set; }

    public IDictionary<string, object> RuntimeContext { get; } =
        new Dictionary<string, object>();

    public ExceptionSerializationStrategy ExceptionSerializationStrategy
    {
        get;
        set;
    }

    public ITraceContext TraceContext { get; set; }

    public ISegmentEmitter Emitter { get; set; }

    public void Dispose()
    {
    }

    public void BeginSegment(
        string name,
        string traceId = null,
        string parentId = null,
        SamplingResponse samplingResponse = null,
        DateTime? timestamp = null)
    {
    }

    public void EndSegment(DateTime? timestamp = null)
    {
    }

    public void BeginSubsegment(string name, DateTime? timestamp = null)
    {
    }

    public void EndSubsegment(DateTime? timestamp = null)
    {
    }

    public void SetNamespace(string value)
    {
    }

    public void AddAnnotation(string key, object value)
    {
    }

    public void MarkFault()
    {
    }

    public void MarkError()
    {
    }

    public void AddException(Exception ex)
    {
    }

    public TResult
        TraceMethod<TResult>(string name, Func<TResult> method) => default;

    public void TraceMethod(string name, Action method)
    {
    }

    public Task<TResult> TraceMethodAsync<TResult>(string name,
        Func<Task<TResult>> method) =>
        Task.FromResult(default(TResult));

    public Task TraceMethodAsync(string name, Func<Task> method) =>
        Task.CompletedTask;

    public void AddHttpInformation(string key, object value)
    {
    }

    public void MarkThrottle()
    {
    }

    public void AddPrecursorId(string precursorId)
    {
    }

    public void AddSqlInformation(string key, string value)
    {
    }

    public void AddMetadata(string key, object value)
    {
    }

    public void AddMetadata(string nameSpace, string key, object value)
    {
    }

    public void SetDaemonAddress(string daemonAddress)
    {
    }
}