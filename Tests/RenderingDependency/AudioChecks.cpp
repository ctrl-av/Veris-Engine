#include "Checks.h"
#include "../../Engine/Engine.h"
#include "../../Engine/SceneFormat.h"
#include <SFML/Audio/SoundChannel.hpp>
#include <cmath>
#include <fstream>

void VerifyAudio(const std::filesystem::path& directory)
{
    std::filesystem::create_directories(directory);
    std::vector<std::int16_t> samples(44100);
    for (std::size_t index = 0; index < samples.size(); ++index) samples[index] = static_cast<std::int16_t>(200 * std::sin(index * 440 * 6.283185307 / 44100));
    sf::SoundBuffer fixture;
    Require(fixture.loadFromSamples(samples.data(), samples.size(), 1, 44100, {sf::SoundChannel::Mono}), "PCM samples must decode.");
    Require(fixture.saveToFile(directory / "tone.wav"), "Audio fixture must save.");
    Veris::Engine engine;
    engine.Initialize({16, 16});
    engine.Textures().SetRoot(directory);
    const auto id = engine.ActiveScene().CreateEntity("Audio");
    auto& source = engine.ActiveScene().Get(id).Audio;
    source = Veris::DeserializeAudio(R"({"clip":"tone.wav","assetId":"","volume":0,"loop":true,"autoPlay":true})", engine.Textures());
    Require(source->Buffer->getSampleCount() == samples.size(), "Audio components must retain decoded sample ownership.");
    engine.FixedUpdate(1.0 / 60);
    Require(engine.Audio().Status(id) == sf::SoundSource::Status::Playing, "Auto-play must start an actual SFML voice.");
    engine.Audio().Pause(id);
    Require(engine.Audio().Status(id) == sf::SoundSource::Status::Paused, "Pause must change native playback state.");
    engine.Audio().Resume(id);
    Require(engine.Audio().Status(id) == sf::SoundSource::Status::Playing, "Resume must restart a paused voice.");
    engine.Audio().PauseAll(); engine.Audio().PauseAll(); engine.Audio().ResumeAll();
    Require(engine.Audio().Status(id) == sf::SoundSource::Status::Playing, "Repeated session pauses must retain the list of voices to resume.");
    engine.Audio().Pause(id); engine.Audio().PauseAll(); engine.Audio().ResumeAll();
    Require(engine.Audio().Status(id) == sf::SoundSource::Status::Paused, "Session resume must preserve explicitly paused audio sources.");
    engine.Audio().Stop(id);
    engine.FixedUpdate(1.0 / 60);
    Require(engine.Audio().Status(id) == sf::SoundSource::Status::Stopped, "Stopped auto-play voices must not restart every fixed update.");
    const auto json = Veris::SerializeScene(engine.ActiveScene());
    auto restored = Veris::DeserializeScene(json, engine.Textures());
    Require(restored.Get(id).Audio->Loop && restored.Get(id).Audio->Buffer->getDuration().asSeconds() > 0.99f, "Scene round trips must restore audio configuration and PCM data.");
    engine.Audio().Play(id, *source);
    engine.ActiveScene().DestroyEntity(id);
    engine.Update();
    Require(engine.Audio().VoiceCount() == 0, "Entity removal must release playing voices and their buffers.");
    Reject<std::invalid_argument>([&] { Veris::DeserializeAudio(R"({"clip":"../tone.wav","assetId":"","volume":100,"loop":false,"autoPlay":false})", engine.Textures()); });
    std::filesystem::remove(directory / "tone.wav");
    std::filesystem::remove(directory);
}
