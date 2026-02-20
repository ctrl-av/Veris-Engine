#pragma once

#include <stdexcept>
#include <filesystem>

void Require(bool condition, const char* message);
void VerifyWindowManagement();
void VerifyFrameTiming();
void VerifyKeyboardTransitions();
void VerifyMouseAndFocus();
void VerifyInputFocusLoss();
void VerifyActionAggregation();
void VerifyActionRebindingAndQuickTaps();
void VerifySpriteRendering(const std::filesystem::path& previewPath);

template<typename Exception, typename Operation>
void Reject(Operation operation)
{
    try { operation(); }
    catch (const Exception&) { return; }
    throw std::runtime_error("Expected operation to fail.");
}
