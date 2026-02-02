#include "Checks.h"
#include "../../Engine/Engine.h"
#include "../../Engine/FrameClock.h"
#include <cmath>
#include <limits>

void VerifyClockStalls(Veris::FrameClock& clock, Veris::Engine& engine, const std::function<void(double)>& simulate);

void VerifyFrameTiming()
{
    Veris::Engine engine;
    engine.Initialize({4, 4});
    Veris::FrameClock clock;
    const auto simulate = [&engine](double step) { engine.FixedUpdate(step); };
    Require(clock.Advance(1.0 / 120, simulate) == 0, "Half a fixed step must accumulate without simulating.");
    Require(clock.Advance(1.0 / 120, simulate) == 1, "Two half steps must run one fixed update.");
    Require(clock.Advance(1.0 / 30, simulate) == 2, "A 30 FPS frame must run two 60 Hz fixed updates.");
    Require(std::abs(engine.SimulationSeconds() - 0.05) < 1e-10, "Simulation time must advance by fixed deltas.");
    clock.Pause();
    clock.Advance(100, simulate);
    Require(engine.CompletedFixedUpdates() == 3 && clock.DeltaSeconds() == 0, "Paused frames must not advance simulation.");
    clock.Resume();
    Require(clock.Advance(Veris::FrameClock::FixedStepSeconds, simulate) == 1, "Resume must start without accumulated pause time.");
    VerifyClockStalls(clock, engine, simulate);
}

void VerifyClockStalls(Veris::FrameClock& clock, Veris::Engine& engine, const std::function<void(double)>& simulate)
{
    Require(clock.Advance(100, simulate) == Veris::FrameClock::MaximumFixedUpdates, "Long stalls must cap catch-up updates.");
    Require(clock.DiscardedSeconds() > 99 && clock.InterpolationAlpha() >= 0 && clock.InterpolationAlpha() < 1, "Dropped backlog must preserve a valid interpolation remainder.");
    Reject<std::invalid_argument>([&] { clock.Advance(-1, simulate); });
    Reject<std::invalid_argument>([&] { clock.Advance(std::numeric_limits<double>::quiet_NaN(), simulate); });
    engine.Shutdown();
    Require(engine.SimulationSeconds() == 0 && engine.CompletedFixedUpdates() == 0, "Shutdown must reset simulation timing.");
}
