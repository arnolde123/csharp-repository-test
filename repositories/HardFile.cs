using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DocumentationBenchmark.Pipeline
{
    /// <summary>
    /// Defines a contract for transforming events of type <typeparamref name="TInput"/> to type <typeparamref name="TOutput"/>.
    /// </summary>
    /// <typeparam name="TInput">The type of the input event.</typeparam>
    /// <typeparam name="TOutput">The type of the output event.</typeparam>
    public interface IEventTransformer<TInput, TOutput> where TOutput : class, new()
    {
        /// <summary>
        /// Asynchronously transforms the input event to an output event.
        /// </summary>
        /// <param name="input">The input event to transform.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, with a result of the transformed output event.</returns>
        Task<TOutput> TransformAsync(TInput input, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Defines a contract for collecting metrics during event processing.
    /// </summary>
    public interface IMetricsCollector
    {
        /// <summary>
        /// Records the processing time for a specific stage.
        /// </summary>
        /// <param name="stageName">The name of the processing stage.</param>
        /// <param name="duration">The duration of the processing.</param>
        void RecordProcessingTime(string stageName, TimeSpan duration);

        /// <summary>
        /// Records a failure that occurred during processing.
        /// </summary>
        /// <param name="stageName">The name of the processing stage where the failure occurred.</param>
        /// <param name="error">The exception that represents the failure.</param>
        void RecordFailure(string stageName, Exception error);
    }

    /// <summary>
    /// Represents a pipeline for processing events of type <typeparamref name="TEvent"/>.
    /// </summary>
    /// <typeparam name="TEvent">The type of events to process.</typeparam>
    public class EventPipeline<TEvent> where TEvent : class, ICloneable, new()
    {
        private readonly IMetricsCollector _metrics;
        private readonly ConcurrentDictionary<string, Action<TEvent>> _handlers;
        private readonly SemaphoreSlim _concurrencyLimiter;
        private int _processedCount;
        private int _failedCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventPipeline{TEvent}"/> class.
        /// </summary>
        /// <param name="metrics">The metrics collector to use for recording processing metrics.</param>
        /// <param name="maxConcurrency">The maximum number of concurrent processing tasks.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="metrics"/> is null.</exception>
        public EventPipeline(IMetricsCollector metrics, int maxConcurrency = 4)
        {
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _handlers = new ConcurrentDictionary<string, Action<TEvent>>();
            _concurrencyLimiter = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            _processedCount = 0;
            _failedCount = 0;
        }

        /// <summary>
        /// Occurs when processing of an event fails.
        /// </summary>
        public event EventHandler<PipelineExceptionEventArgs> OnProcessingFailed;

        /// <summary>
        /// Registers a handler for processing events.
        /// </summary>
        /// <param name="name">The name of the handler.</param>
        /// <param name="handler">The action to execute for the event.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is null.</exception>
        public void RegisterHandler(string name, Action<TEvent> handler)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Handler name cannot be empty", nameof(name));
            
            if (handler is null)
                throw new ArgumentNullException(nameof(handler));

            _handlers.AddOrUpdate(name, handler, (_, existing) => existing + handler);
        }

        /// <summary>
        /// Asynchronously processes a batch of events.
        /// </summary>
        /// <typeparam name="TTransformed">The type of the transformed event.</typeparam>
        /// <param name="events">The array of events to process.</param>
        /// <param name="transformer">The transformer to use for transforming events.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, with a result of the processing results.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="events"/> or <paramref name="transformer"/> is null.</exception>
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

        /// <summary>
        /// Gets the statistics of the event processing.
        /// </summary>
        /// <returns>A <see cref="PipelineStatistics"/> object containing the processing statistics.</returns>
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

    /// <summary>
    /// Represents the result of processing a batch of events.
    /// </summary>
    public class PipelineResult
    {
        /// <summary>
        /// Gets or sets the total number of processed events.
        /// </summary>
        public int TotalProcessed { get; set; }

        /// <summary>
        /// Gets or sets the number of successfully processed events.
        /// </summary>
        public int Successful { get; set; }

        /// <summary>
        /// Gets or sets the number of failed events.
        /// </summary>
        public int Failed { get; set; }

        /// <summary>
        /// Gets or sets the array of exceptions that occurred during processing.
        /// </summary>
        public Exception[] Errors { get; set; }

        /// <summary>
        /// Gets an empty <see cref="PipelineResult"/> instance.
        /// </summary>
        public static PipelineResult Empty => new() { TotalProcessed = 0, Errors = Array.Empty<Exception>() };
    }

    /// <summary>
    /// Represents statistics about the event processing pipeline.
    /// </summary>
    public class PipelineStatistics
    {
        /// <summary>
        /// Gets or sets the count of processed events.
        /// </summary>
        public int ProcessedCount { get; set; }

        /// <summary>
        /// Gets or sets the count of failed events.
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Gets or sets the count of registered handlers.
        /// </summary>
        public int RegisteredHandlers { get; set; }

        /// <summary>
        /// Gets or sets the current depth of the processing queue.
        /// </summary>
        public int CurrentQueueDepth { get; set; }
    }

    /// <summary>
    /// Provides data for the <see cref="EventPipeline{TEvent}.OnProcessingFailed"/> event.
    /// </summary>
    public class PipelineExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the index of the event that caused the failure.
        /// </summary>
        public int EventIndex { get; set; }

        /// <summary>
        /// Gets or sets the exception that occurred during processing.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Gets or sets the data of the event that caused the failure.
        /// </summary>
        public object EventData { get; set; }
    }
}