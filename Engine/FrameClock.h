#pragma once

#include <chrono>
#include <functional>

namespace Veris
{
    class FrameClock
    {
    public:
        static constexpr double FixedStepSeconds = 1.0 / 60.0;
        static constexpr double MaximumFrameSeconds = 0.25;
        static constexpr unsigned int MaximumFixedUpdates = 8;

        unsigned int Tick(const std::function<void(double)>& fixedUpdate);
        unsigned int Advance(double elapsedSeconds, const std::function<void(double)>& fixedUpdate);
        void Pause();
        void Resume();
        bool IsPaused() const { return _paused; }
        double DeltaSeconds() const { return _deltaSeconds; }
        double InterpolationAlpha() const { return _accumulator / FixedStepSeconds; }
        double DiscardedSeconds() const { return _discardedSeconds; }

    private:
        std::chrono::steady_clock::time_point _previous = std::chrono::steady_clock::now();
        double _accumulator = 0;
        double _deltaSeconds = 0;
        double _discardedSeconds = 0;
        bool _paused = false;
    };
}
