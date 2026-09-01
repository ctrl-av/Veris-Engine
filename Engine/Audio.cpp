#include "pch.h"
#include "Audio.h"
#include <cmath>
#include "Scene.h"
#include "TextureResources.h"
#include "JsonValidation.h"

namespace Veris
{
    std::string SerializeAudio(const AudioSource& source)
    {
        return Json::Document{{"clip", source.ClipPath}, {"assetId", source.AssetId}, {"volume", source.Volume}, {"loop", source.Loop}, {"autoPlay", source.AutoPlay}}.dump();
    }

    AudioSource DeserializeAudio(const std::string& json, TextureResources& resources)
    {
        const auto document = Json::Parse(json);
        Json::Fields(document, {"clip", "assetId", "volume", "loop", "autoPlay"});
        AudioSource source;
        source.ClipPath = document.at("clip").get<std::string>();
        source.AssetId = document.at("assetId").get<std::string>();
        source.Volume = Json::Number(document.at("volume"));
        if (source.Volume < 0 || source.Volume > 100) throw std::invalid_argument("Audio volume must be between 0 and 100.");
        source.Loop = document.at("loop").get<bool>();
        source.AutoPlay = document.at("autoPlay").get<bool>();
        if (!source.AssetId.empty())
        {
            source.AssetId = TextureResources::NormalizeAssetId(source.AssetId);
            source.ClipPath = resources.AssetPath(source.AssetId).u8string();
        }
        else source.AssetId = resources.AssetId(std::filesystem::u8path(source.ClipPath));
        const auto path = resources.ContentPath(std::filesystem::u8path(source.ClipPath));
        if (std::filesystem::file_size(path) > 256 * 1024 * 1024) throw std::invalid_argument("Audio clips cannot exceed 256 MiB.");
        auto buffer = std::make_shared<sf::SoundBuffer>();
        if (!buffer->loadFromFile(path)) throw std::invalid_argument("Cannot decode audio clip: " + source.ClipPath);
        source.Buffer = std::move(buffer);
        return source;
    }

    AudioPlayback::Voice::Voice(const AudioSource& source, float masterVolume) : Buffer(source.Buffer), Sound(*Buffer), SourceVolume(source.Volume)
    {
        Sound.setVolume(source.Volume * masterVolume / 100);
        Sound.setLooping(source.Loop);
    }

    void AudioPlayback::Play(std::uint64_t entity, const AudioSource& source)
    {
        if (!source.Buffer) throw std::invalid_argument("Audio playback requires an owned decoded buffer.");
        auto voice = std::make_unique<Voice>(source, _masterVolume);
        voice->Sound.play();
        _voices[entity] = std::move(voice);
    }

    void AudioPlayback::Pause(std::uint64_t entity) { _voices.at(entity)->Sound.pause(); }
    void AudioPlayback::Resume(std::uint64_t entity) { _voices.at(entity)->Sound.play(); }
    void AudioPlayback::Stop(std::uint64_t entity)
    {
        const auto voice = _voices.find(entity);
        if (voice != _voices.end()) voice->second->Sound.stop();
    }
    sf::SoundSource::Status AudioPlayback::Status(std::uint64_t entity) const
    {
        const auto voice = _voices.find(entity);
        return voice == _voices.end() ? sf::SoundSource::Status::Stopped : voice->second->Sound.getStatus();
    }

    void AudioPlayback::Synchronize(const Scene& scene)
    {
        for (auto voice = _voices.begin(); voice != _voices.end();)
        {
            if (!scene.Contains(voice->first) || !scene.Get(voice->first).Audio || scene.Get(voice->first).Audio->Buffer != voice->second->Buffer)
                voice = _voices.erase(voice);
            else ++voice;
        }
    }

    void AudioPlayback::StartAutomatic(const Scene& scene)
    {
        Synchronize(scene);
        for (const auto& entry : scene.Entities())
            if (entry.second.Audio && entry.second.Audio->AutoPlay && _voices.count(entry.first) == 0) Play(entry.first, *entry.second.Audio);
    }
}

void Veris::AudioPlayback::PauseAll()
{
    for (auto& voice : _voices)
        if (voice.second->Sound.getStatus() == sf::SoundSource::Status::Playing) { _suspended.insert(voice.first); voice.second->Sound.pause(); }
}
void Veris::AudioPlayback::ResumeAll()
{
    for (const auto id : _suspended)
        if (_voices.count(id) && _voices.at(id)->Sound.getStatus() == sf::SoundSource::Status::Paused) _voices.at(id)->Sound.play();
    _suspended.clear();
}

void Veris::AudioPlayback::SetMasterVolume(float volume)
{
    if (!std::isfinite(volume) || volume < 0 || volume > 100) throw std::invalid_argument("Master volume must be between 0 and 100.");
    _masterVolume = volume;
    for (auto& voice : _voices) voice.second->Sound.setVolume(voice.second->SourceVolume * volume / 100);
}
