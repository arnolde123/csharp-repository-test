using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace DocumentationBenchmark.Pipeline
{
    public interface IEventTransformer<TInput, TOutput> where TOutput : class, new()
    {
        Task<TOutput> TransformAsync(TInput input, CancellationToken cancellationToken = default);
    }

    public interface IMetricsCollector
    {
        void RecordProcessingTime(string stageName, TimeSpan duration);
        void RecordFailure(string stageName, Exception error);
    }

    public class EventPipeline<TEvent> where TEvent : class, ICloneable, new()
    {
        private readonly IMetricsCollector _metrics;
        private readonly ConcurrentDictionary<string, Action<TEvent>> _handlers;
        private readonly SemaphoreSlim _concurrencyLimiter;
        private int _processedCount;
        private int _failedCount;

        public EventPipeline(IMetricsCollector metrics, int maxConcurrency = 4)
        {
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _handlers = new ConcurrentDictionary<string, Action<TEvent>>();
            _concurrencyLimiter = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            _processedCount = 0;
            _failedCount = 0;
        }

        public event EventHandler<PipelineExceptionEventArgs> OnProcessingFailed;

        public void RegisterHandler(string name, Action<TEvent> handler)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Handler name cannot be empty", nameof(name));
            
            if (handler is null)
                throw new ArgumentNullException(nameof(handler));

            _handlers.AddOrUpdate(name, handler, (_, existing) => existing + handler);
        }

        public async Task<PipelineResult> ProcessBatchAsync<TTransformed>(
            TEvent[] events,
            IEventTransformer<TEvent, TTransformed> transformer,
            CancellationToken cancellationToken = default)
            where TTransformed : class, ICloneable, new()
        {
            if (events is null || events.Length == 0)
                return PipelineResult.Empty;

            var results = new ConcurrentBag<(bool success, Exception error)>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await _concurrencyLimiter.WaitAsync(cancellationToken);

                var processingTasks = new Task[events.Length];
                
                for (int i = 0; i < events.Length; i++)
                {
                    var eventCopy = (TEvent)events[i].Clone();
                    var index = i;

                    processingTasks[i] = Task.Run(async () =>
                    {
                        try
                        {
                            var transformed = await transformer.TransformAsync(eventCopy, cancellationToken);
                            await ExecuteHandlersAsync(eventCopy, cancellationToken);
                            Interlocked.Increment(ref _processedCount);
                            results.Add((true, null));
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref _failedCount);
                            _metrics.RecordFailure("Transform", ex);
                            results.Add((false, ex));
                            
                            OnProcessingFailed?.Invoke(this, new PipelineExceptionEventArgs
                            {
                                EventIndex = index,
                                Exception = ex,
                                EventData = eventCopy
                            });
                        }
                    }, cancellationToken);
                }

                await Task.WhenAll(processingTasks);
            }
            finally
            {
                _concurrencyLimiter.Release();
                stopwatch.Stop();
                _metrics.RecordProcessingTime("BatchProcessing", stopwatch.Elapsed);
            }

            return new PipelineResult
            {
                TotalProcessed = events.Length,
                Successful = _processedCount,
                Failed = _failedCount,
                Errors = results.Where(r => !r.success).Select(r => r.error).ToArray()
            };
        }

        private async Task ExecuteHandlersAsync(TEvent evt, CancellationToken cancellationToken)
        {
            foreach (var kvp in _handlers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await Task.Run(() => kvp.Value(evt), cancellationToken);
                }
                catch (Exception ex)
                {
                    _metrics.RecordFailure($"Handler_{kvp.Key}", ex);
                    
                    if (ex is OutOfMemoryException)
                        throw;
                }
            }
        }

        public PipelineStatistics GetStatistics()
        {
            return new PipelineStatistics
            {
                ProcessedCount = _processedCount,
                FailedCount = _failedCount,
                RegisteredHandlers = _handlers.Count,
                CurrentQueueDepth = _concurrencyLimiter.CurrentCount
            };
        }
    }

    public class PipelineResult
    {
        public int TotalProcessed { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }
        public Exception[] Errors { get; set; }
        public static PipelineResult Empty => new() { TotalProcessed = 0, Errors = Array.Empty<Exception>() };
    }

    public class PipelineStatistics
    {
        public int ProcessedCount { get; set; }
        public int FailedCount { get; set; }
        public int RegisteredHandlers { get; set; }
        public int CurrentQueueDepth { get; set; }
    }

    public class PipelineExceptionEventArgs : EventArgs
    {
        public int EventIndex { get; set; }
        public Exception Exception { get; set; }
        public object EventData { get; set; }
    }
}
