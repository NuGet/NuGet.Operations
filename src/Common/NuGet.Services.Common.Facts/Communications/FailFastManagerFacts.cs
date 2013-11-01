using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NuGet.Services.Communications;
using Xunit;

namespace NuGet.Services.Common.Facts.Communications
{
    public class FailFastManagerFacts
    {
        // Tests that the constructor sets things up correctly
        public class TheConstructor
        {
            [Fact]
            public void PutsTheServiceInUnknownState()
            {
                var manager = new FailFastManager("test", ServiceFailureDetector.Default);
                Assert.Equal(FailFastState.Unknown, manager.State);
            }
        }

        // Tests that the initial state is set appropriately
        public class WhenInUnknownState
        {
            [Fact]
            public async Task GivenUnknownStateAndTheActionDoesNotThrow_ItSetsStateToActive()
            {
                // Arrange
                var manager = new FailFastManager("test", ServiceFailureDetector.Default);

                // Act
                await Succeed(manager);

                // Assert
                Assert.Equal(FailFastState.Active, manager.State);
            }

            [Fact]
            public async Task GivenUnknownStateAndTheActionThrows_ItSetsStateToFaulting()
            {
                // Arrange
                var manager = new FailFastManager("test", ServiceFailureDetector.Default);

                // Act
                await FailWithException(manager);

                // Assert
                Assert.Equal(FailFastState.Faulting, manager.State);
            }

            [Fact]
            public async Task GivenUnknownStateAndTheActionTimesOut_ItSetsStateToTimingOut()
            {
                // Arrange
                var manager = new FailFastManager("test", ServiceFailureDetector.Default);

                // Act
                await FailWithTimeout(manager);

                // Assert
                Assert.Equal(FailFastState.TimingOut, manager.State);
            }
        }

        // Tests that normal states behave appropriately
        public class WhenInActiveFaultingAndTimingOutStates
        {
            [Fact]
            public async Task GivenActiveStateAndTheActionThrows_ItSetsStateToFaulting()
            {
                // Arrange
                var manager = new FailFastManager("test", ServiceFailureDetector.Default);
                await Succeed(manager);

                // Act
                await FailWithException(manager);

                // Assert
                Assert.Equal(FailFastState.Faulting, manager.State);
            }

            [Fact]
            public async Task GivenActiveStateAndTheActionTimesOut_ItSetsStateToTimingOut()
            {
                // Arrange
                var manager = new FailFastManager("test", ServiceFailureDetector.Default);
                await Succeed(manager);

                // Act
                await FailWithTimeout(manager);

                // Assert
                Assert.Equal(FailFastState.TimingOut, manager.State);
            }

            [Fact]
            public async Task GivenEnoughConsecutiveFailures_ItSetsStateToFailFast()
            {
                // Arrange
                var manager = new FailFastManager(
                    "test", 
                    ServiceFailureDetector.Default,
                    consecutiveFailureThreshold: 2);
                await Succeed(manager);

                // Act (fail three times in a row)
                await FailWithTimeout(manager);
                await FailWithException(manager);
                await FailWithTimeout(manager);

                // Assert
                Assert.Equal(FailFastState.FailFast, manager.State);
            }

            [Fact]
            public async Task GivenSomeFailuresButNotEnoughConsecutive_TheStateIsUpdatedWithEachRequest()
            {
                // Arrange
                var manager = new FailFastManager(
                    "test",
                    ServiceFailureDetector.Default,
                    consecutiveFailureThreshold: 2);
                await Succeed(manager);

                // Act/Assert (fail twice in a row, but then succeed)
                await FailWithTimeout(manager);
                Assert.Equal(FailFastState.TimingOut, manager.State);

                await FailWithException(manager);
                Assert.Equal(FailFastState.Faulting, manager.State);

                await Succeed(manager);
                Assert.Equal(FailFastState.Active, manager.State);
            }
        }

        // Tests that when in FailFast state, the system behaves appropriately
        public class WhenInFailFastState
        {
            [Fact]
            public async Task RequestImmediatelyThrowsFailFastException()
            {
                // Arrange
                var manager = new FailFastManager("test", ServiceFailureDetector.Default);
                manager.SetState(FailFastState.FailFast);

                // Act/Assert
                await AsyncAssert.Throws<FailFastException>(
                    async () => await Succeed(manager));
            }

            [Fact]
            public async Task AfterFailFastPeriodElapses_IfRequestSucceedsStateChangesToActive()
            {
                // Arrange
                var failFastPeriod = TimeSpan.FromMinutes(5);
                var clock = new TestClock();
                var manager = new FailFastManager("test", ServiceFailureDetector.Default, failFastPeriod);
                manager.SetClock(clock);
                manager.SetState(FailFastState.FailFast);

                // Act (advance time by the fail fast period + 1ms)
                clock.Advance(failFastPeriod).Advance(1);
                await Succeed(manager);

                // Assert
                Assert.Equal(FailFastState.Active, manager.State);
            }

            [Fact]
            public async Task AfterFailFastPeriodElapses_IfRequestFailsStateRemainsAtFailFast()
            {
                // Arrange
                var failFastPeriod = TimeSpan.FromMinutes(5);
                var clock = new TestClock();
                var manager = new FailFastManager("test", ServiceFailureDetector.Default, failFastPeriod);
                manager.SetClock(clock);
                manager.SetState(FailFastState.FailFast);

                // Act (advance time by the fail fast period + 1ms)
                clock.Advance(failFastPeriod).Advance(1);
                await FailWithException(manager);

                // Assert
                Assert.Equal(FailFastState.FailFast, manager.State);
            }

            [Fact]
            public async Task AfterFailFastPeriodElapses_IfRequestFailsStateRemainsAtFailFastForAnotherPeriod()
            {
                // Arrange
                var failFastPeriod = TimeSpan.FromMinutes(5);
                var clock = new TestClock();
                var manager = new FailFastManager("test", ServiceFailureDetector.Default, failFastPeriod);
                manager.SetClock(clock);
                manager.SetState(FailFastState.FailFast);

                // Advance time by the fail fast period + 1ms
                clock.Advance(failFastPeriod).Advance(1);
                await FailWithException(manager);

                // Act/Assert (advance time by a millisecond and try again, should fail fast)
                await AsyncAssert.Throws<FailFastException>(
                    async () => await Succeed(manager));
            }
        }

        // Tests that when an admin forces the system to TryRestore state, the system behaves appropriately
        public class WhenForcedIntoTryRestoreState
        {
            [Fact]
            public async Task IfRequestFails_StateTransitionsToFailFast()
            {
                // Arrange
                var manager = new FailFastManager("test", ServiceFailureDetector.Default);
                manager.SetState(FailFastState.TryRestore);

                // Act
                await FailWithException(manager);

                // Assert
                Assert.Equal(FailFastState.FailFast, manager.State);
            }

            [Fact]
            public async Task IfRequestSucceeds_StateTransitionsToActive()
            {
                // Arrange
                var manager = new FailFastManager("test", ServiceFailureDetector.Default);
                manager.SetState(FailFastState.TryRestore);

                // Act
                await Succeed(manager);

                // Assert
                Assert.Equal(FailFastState.Active, manager.State);
            }
        }

        public class WhenUsingACustomFailureDetector
        {
            [Fact]
            public async Task IfDetectorIndicatesExceptionIsNotFailure_ItRemainsInActiveState()
            {
                // Arrange
                var detector = new Mock<ServiceFailureDetector>();
                var manager = new FailFastManager("test", detector.Object);
                var exception = new InvalidOperationException("Not a real failure!");
                detector.Setup(d => d.IsFailureException(exception)).Returns(false);

                // Act
                await FailWithException(manager, exception);

                // Assert
                Assert.Equal(FailFastState.Active, manager.State);
            }

            [Fact]
            public async Task IfDetectorIndicatesExceptionIsFailure_ItGoesToFaultingState()
            {
                // Arrange
                var detector = new Mock<ServiceFailureDetector>();
                var manager = new FailFastManager("test", detector.Object);
                var exception = new InvalidOperationException("Not a real failure!");
                detector.Setup(d => d.IsFailureException(exception)).Returns(true);

                // Act
                await FailWithException(manager, exception);

                // Assert
                Assert.Equal(FailFastState.Faulting, manager.State);
            }

            [Fact]
            public async Task IfDetectorIndicatesResultIsNotFailure_ItRemainsInActiveState()
            {
                // Arrange
                var detector = new Mock<ServiceFailureDetector>();
                var manager = new FailFastManager("test", detector.Object);
                var result = new object();
                detector.Setup(d => d.IsFailureResult(result)).Returns(false);

                // Act
                await Succeed(manager, result);

                // Assert
                Assert.Equal(FailFastState.Active, manager.State);
            }

            [Fact]
            public async Task IfDetectorIndicatesResultIsFailure_ItGoesToFaultingState()
            {
                // Arrange
                var detector = new Mock<ServiceFailureDetector>();
                var manager = new FailFastManager("test", detector.Object);
                var result = new object();
                detector.Setup(d => d.IsFailureResult(result)).Returns(true);

                // Act
                await Succeed(manager, result);

                // Assert
                Assert.Equal(FailFastState.Faulting, manager.State);
            }
        }

        private static async Task Succeed(FailFastManager manager)
        {
            await Succeed(manager, null);
        }

        private static async Task Succeed(FailFastManager manager, object result)
        {
            await manager.Invoke(() => Task.FromResult(result));
        }

#pragma warning disable 1998
        private static async Task FailWithException(FailFastManager manager)
        {
            await FailWithException(manager, new InvalidTimeZoneException("bork bork bork!"));
        }

        private static async Task FailWithException<TException>(FailFastManager manager, TException ex) where TException: Exception
        {
            await AsyncAssert.Throws<TException>(
                async () => await manager.Invoke(async () => { throw ex; }));
        }

        private static async Task FailWithTimeout(FailFastManager manager)
        {
            await AsyncAssert.Throws<TimeoutException>(
                async () => await manager.Invoke(async () => { throw new TimeoutException("bork bork bork!"); }));
        }
#pragma warning restore 1998
    }
}
