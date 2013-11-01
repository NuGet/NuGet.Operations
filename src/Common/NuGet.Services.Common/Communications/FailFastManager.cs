using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Services.Communications
{
    /// <summary>
    /// Component that manages communication with an underlying service. This is not used
    /// directly by NuGet Services, but is used to build wrappers around APIs like Azure
    /// Storage in order to provide Communication management functionality.
    /// </summary>
    /// </remarks>
    public class FailFastManager
    {
        public static readonly TimeSpan DefaultFailFastPeriod = TimeSpan.FromMinutes(5);

        private ServiceState _state;

        /// <summary>
        /// Gets the current state of the service
        /// </summary>
        public FailFastState State { get { return _state.State; } }

        /// <summary>
        /// Gets the failure detector used to determine if a service response is a failure
        /// </summary>
        public ServiceFailureDetector Detector { get; private set; }

        /// <summary>
        /// Gets the name of the service this manager is controlling
        /// </summary>
        public string ServiceName { get { return _state.ServiceName; } }

        internal Clock Clock
        {
            get { return _state.Clock; }
        }

        /// <summary>
        /// Constructs an ExternalCommunicationManager that does not track consecutive failures using the default service detector and remains in fail fast mode for the default period of 5 minutes.
        /// </summary>
        /// <param name="serviceName">The name of the service being communicated with</param>
        public FailFastManager(string serviceName)
            : this(serviceName, ServiceFailureDetector.Default)
        {
        }

        /// <summary>
        /// Constructs an ExternalCommunicationManager that uses a custom failure detector and does not track consecutive failures and remains in fail fast mode for the default period of 5 minutes.
        /// </summary>
        /// <param name="serviceName">The name of the service being communicated with</param>
        /// <param name="detector">The failure detector used to determine if a service response is a failure</param>
        public FailFastManager(string serviceName, ServiceFailureDetector detector)
            : this(serviceName, detector, (int?)null, DefaultFailFastPeriod)
        {
        }

        /// <summary>
        /// Constructs an ExternalCommunicationManager that uses the default failure detector and tracks consecutive failures and remains in fail fast mode for the default period of 5 minutes.
        /// </summary>
        /// <param name="serviceName">The name of the service being communicated with</param>
        /// <param name="consecutiveFailureThreshold">The number of consecutive failures that can be tolerated before entering FailFast mode (once the number of consecutive failures goes OVER this number, the manager enters fail fast mode)</param>
        public FailFastManager(string serviceName, int consecutiveFailureThreshold)
            : this(serviceName, ServiceFailureDetector.Default, (int?)consecutiveFailureThreshold, DefaultFailFastPeriod)
        {
        }

        /// <summary>
        /// Constructs an ExternalCommunicationManager that uses a custom failure detector and tracks consecutive failures and remains in fail fast mode for the default period of 5 minutes.
        /// </summary>
        /// <param name="serviceName">The name of the service being communicated with</param>
        /// <param name="detector">The failure detector used to determine if a service response is a failure</param>
        /// <param name="consecutiveFailureThreshold">The number of consecutive failures that can be tolerated before entering FailFast mode (once the number of consecutive failures goes OVER this number, the manager enters fail fast mode)</param>
        public FailFastManager(string serviceName, ServiceFailureDetector detector, int consecutiveFailureThreshold)
            : this(serviceName, detector, (int?)consecutiveFailureThreshold, DefaultFailFastPeriod)
        {
        }

        /// <summary>
        /// Constructs an ExternalCommunicationManager that uses the default failure detector and remains in fail fast mode for the specified period.
        /// </summary>
        /// <param name="serviceName">The name of the service being communicated with</param>
        /// <param name="failFastPeriod">The length of time to stay in fail fast mode before attempting to restore the connection</param>
        public FailFastManager(string serviceName, TimeSpan failFastPeriod)
            : this(serviceName, ServiceFailureDetector.Default, (int?)null, failFastPeriod)
        {
        }

        /// <summary>
        /// Constructs an ExternalCommunicationManager that uses a custom failure detector and remains in fail fast mode for the specified period.
        /// </summary>
        /// <param name="serviceName">The name of the service being communicated with</param>
        /// <param name="detector">The failure detector used to determine if a service response is a failure</param>
        /// <param name="failFastPeriod">The length of time to stay in fail fast mode before attempting to restore the connection</param>
        public FailFastManager(string serviceName, ServiceFailureDetector detector, TimeSpan failFastPeriod)
            : this(serviceName, detector, (int?)null, failFastPeriod)
        {
        }
        

        /// <summary>
        /// Constructs an ExternalCommunicationManager that uses the default failure detector, tracks consecutive failures, and remains in fail fast mode for the specified period.
        /// </summary>
        /// <param name="serviceName">The name of the service being communicated with</param>
        /// <param name="consecutiveFailureThreshold">The number of consecutive failures that can be tolerated before entering FailFast mode (once the number of consecutive failures goes OVER this number, the manager enters fail fast mode)</param>
        /// <param name="failFastPeriod">The length of time to stay in fail fast mode before attempting to restore the connection</param>
        public FailFastManager(string serviceName, int consecutiveFailureThreshold, TimeSpan failFastPeriod)
            : this(serviceName, ServiceFailureDetector.Default, (int?)consecutiveFailureThreshold, failFastPeriod)
        {
        }

        /// <summary>
        /// Constructs an ExternalCommunicationManager that uses a custom failure detector, tracks consecutive failures, and remains in fail fast mode for the specified period.
        /// </summary>
        /// <param name="serviceName">The name of the service being communicated with</param>
        /// <param name="detector">The failure detector used to determine if a service response is a failure</param>
        /// <param name="consecutiveFailureThreshold">The number of consecutive failures that can be tolerated before entering FailFast mode (once the number of consecutive failures goes OVER this number, the manager enters fail fast mode)</param>
        /// <param name="failFastPeriod">The length of time to stay in fail fast mode before attempting to restore the connection</param>
        public FailFastManager(string serviceName, ServiceFailureDetector detector, int consecutiveFailureThreshold, TimeSpan failFastPeriod)
            : this(serviceName, detector, (int?)consecutiveFailureThreshold, failFastPeriod)
        {
        }

        private FailFastManager(string serviceName, ServiceFailureDetector detector, int? consecutiveFailureThreshold, TimeSpan failFastPeriod)
        {
            Detector = detector;
            _state = new ServiceState(serviceName, consecutiveFailureThreshold, failFastPeriod);

            ExternalCommunicationsEventSource.Log.Initialize(serviceName);
        }

        public virtual async Task<T> Invoke<T>(Func<Task<T>> act)
        {
            if (_state.ShouldFailFast())
            {
                FailFast();
            }

            T result;
            try
            {
                result = await act();
            }
            catch (TimeoutException tex)
            {
                _state.Timeout(tex);
                throw;
            }
            catch (Exception ex)
            {
                if (Detector.IsFailureException(ex))
                {
                    _state.Fault(ex);
                }
                else
                {
                    // Consider this a success, but still throw the exception out
                    _state.Success();
                }
                throw;
            }

            if (Detector.IsFailureResult(result))
            {
                _state.Fault();
            }
            else
            {
                _state.Success();
            }
            return result;
        }

        public virtual async Task Invoke(Func<Task> act)
        {
            await Invoke<object>(async () => { 
                await act();
                return null;
            });
        }

        /// <summary>
        /// Forces the service to enter the state specified below. It will continue to automatically
        /// transition from that state to other states.
        /// </summary>
        /// <param name="state"></param>
        public void SetState(FailFastState state)
        {
            _state.SetState(state);
        }

        internal void SetClock(Clock clock)
        {
            _state.SetClock(clock);
        }

        private void FailFast()
        {
            throw new FailFastException();
        }

        private class ServiceState
        {
            private object _lock = new object();
            private int? _consecutiveFailureThreshold = null;
            private DateTimeOffset? _enteredFailFastAt;
            
            public string ServiceName { get; private set; }
            public int ConsecutiveFailureCount { get; private set; }
            public FailFastState State { get; private set; }
            public Clock Clock { get; private set; }
            public TimeSpan FailFastPeriod { get; private set; }

            public ServiceState(string serviceName, int? consecutiveFailureThreshold, TimeSpan failFastPeriod)
            {
                ServiceName = serviceName;
                _consecutiveFailureThreshold = consecutiveFailureThreshold;
                State = FailFastState.Unknown;
                FailFastPeriod = failFastPeriod;
                Clock = Clock.Instance;
            }

            public void SetClock(Clock clock)
            {
                lock (_lock)
                {
                    Clock = clock;
                }
            }

            public void SetState(FailFastState state)
            {
                if (state != State)
                {
                    lock (_lock)
                    {
                        if (state != State)
                        {
                            ExternalCommunicationsEventSource.Log.StateChanged(ServiceName, state);
                            if (state == FailFastState.FailFast)
                            {
                                UnlockedEnterFailFastMode();
                            }
                            else
                            {
                                State = state;
                            }
                        }
                    }
                }
            }

            public void Success()
            {
                lock (_lock)
                {
                    if (State == FailFastState.TryRestore || State == FailFastState.FailFast)
                    {
                        ExternalCommunicationsEventSource.Log.LeftFailFast(ServiceName);
                    }
                    State = FailFastState.Active;

                    var oldCount = ConsecutiveFailureCount;
                    ConsecutiveFailureCount = 0;
                    if (oldCount > 0)
                    {
                        ExternalCommunicationsEventSource.Log.SucceededAfterFailures(
                            ServiceName,
                            oldCount);
                    }
                    else
                    {
                        ExternalCommunicationsEventSource.Log.Succeeded(ServiceName);
                    }
                }
            }

            public void Timeout(TimeoutException ex)
            {
                lock (_lock)
                {
                    ExternalCommunicationsEventSource.Log.TimedOut(
                        ServiceName,
                        ex.ToString(),
                        ex.StackTrace,
                        ConsecutiveFailureCount);
                    UnlockedAnyFailure(FailFastState.TimingOut);
                }
            }

            public void Fault()
            {
                lock (_lock)
                {
                    ExternalCommunicationsEventSource.Log.FaultedResult(
                        ServiceName,
                        ConsecutiveFailureCount);
                    UnlockedAnyFailure(FailFastState.Faulting);
                }
            }

            public void Fault(Exception ex)
            {
                lock (_lock)
                {
                    ExternalCommunicationsEventSource.Log.Faulted(
                        ServiceName,
                        ex.ToString(),
                        ex.StackTrace,
                        ConsecutiveFailureCount);
                    UnlockedAnyFailure(FailFastState.Faulting);
                }
            }

            private void UnlockedAnyFailure(FailFastState nonFailFastState)
            {
                if (State == FailFastState.TryRestore)
                {
                    ExternalCommunicationsEventSource.Log.ContinuingFailFast(ServiceName);
                    _enteredFailFastAt = Clock.UtcNow;
                    State = FailFastState.FailFast;
                }
                else
                {
                    State = nonFailFastState;
                }
                UnlockedIncrementFailureCount();
            }

            public bool ShouldFailFast()
            {
                if (State == FailFastState.FailFast)
                {
                    lock (_lock)
                    {
                        if (State == FailFastState.FailFast)
                        {
                            Debug.Assert(_enteredFailFastAt.HasValue);
                            var timeSinceEntry = Clock.UtcNow - _enteredFailFastAt.Value;
                            if (timeSinceEntry > FailFastPeriod)
                            {
                                ExternalCommunicationsEventSource.Log.LeaveFailFastAttempt(ServiceName);
                                State = FailFastState.TryRestore;
                            }
                            else
                            {
                                // Fail fast!
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            private void UnlockedEnterFailFastMode()
            {
                State = FailFastState.FailFast;
                _enteredFailFastAt = Clock.UtcNow;
                ExternalCommunicationsEventSource.Log.EnteredFailFastFromConsecutiveFailures(ServiceName, ConsecutiveFailureCount);
            }

            private void UnlockedIncrementFailureCount()
            {
                ConsecutiveFailureCount++;
                if (_consecutiveFailureThreshold.HasValue && ConsecutiveFailureCount > _consecutiveFailureThreshold.Value)
                {
                    UnlockedEnterFailFastMode();
                }
            }
        }
    }

    public class ExternalCommunicationManager<TServiceType> : FailFastManager
    {
        public ExternalCommunicationManager(ServiceFailureDetector detector)
            : base(typeof(TServiceType).FullName, detector) { }

        public ExternalCommunicationManager(string serviceName, ServiceFailureDetector detector)
            : base(serviceName, detector) { }
    }
}
