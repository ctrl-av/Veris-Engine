#include "pch.h"
#include "FrameClock.h"
#include <algorithm>
#include <cmath>
#include <stdexcept>

namespace Veris
{
    unsigned int FrameClock::Tick(const std::function<void(double)>& fixedUpdate)
    {
        const auto now = std::chrono::steady_clock::now();
        const auto elapsed = std::chrono::duration<double>(now - _previous).count();
        _previous = now;
        return Advance(elapsed, fixedUpdate);
    }

    unsigned int FrameClock::Advance(double elapsedSeconds, const std::function<void(double)>& fixedUpdate)
    {
        if (!std::isfinite(elapsedSeconds) || elapsedSeconds < 0) throw std::invalid_argument("Elapsed time must be finite and nonnegative.");
        if (_paused) { _deltaSeconds = 0; return 0; }
        _deltaSeconds = std::min(elapsedSeconds, MaximumFrameSeconds);
        _discardedSeconds += elapsedSeconds - _deltaSeconds;
        _accumulator += _deltaSeconds;
        unsigned int updates = 0;
        while (_accumulator >= FixedStepSeconds && updates < MaximumFixedUpdates)
        {
            fixedUpdate(FixedStepSeconds);
            _accumulator -= FixedStepSeconds;
            ++updates;
        }
        const auto remainder = std::fmod(_accumulator, FixedStepSeconds);
        _discardedSeconds += _accumulator - remainder;
        _accumulator = remainder;
        return updates;
    }

    void FrameClock::Pause()
    {
        if (_paused) return;
        _paused = true;
        _accumulator = 0;
        _deltaSeconds = 0;
        _previous = std::chrono::steady_clock::now();
    }

    void FrameClock::Resume()
    {
        if (!_paused) return;
        _paused = false;
        _previous = std::chrono::steady_clock::now();
    }
}
