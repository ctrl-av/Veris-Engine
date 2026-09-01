#pragma once
#include <SFML/Audio/Sound.hpp>
#include <SFML/Audio/SoundBuffer.hpp>
#include <map>
#include <set>
#include <memory>
#include <string>
#include <cstdint>

namespace Veris
{
    class Scene;
    class TextureResources;
    struct AudioSource
    {
        std::string ClipPath;
        std::string AssetId;
        std::shared_ptr<const sf::SoundBuffer> Buffer;
        float Volume = 100;
        bool Loop = false;
        bool AutoPlay = false;
    };

    std::string SerializeAudio(const AudioSource& source);
    AudioSource DeserializeAudio(const std::string& json, TextureResources& resources);

    class AudioPlayback
    {
    public:
        void Synchronize(const Scene& scene);
        void StartAutomatic(const Scene& scene);
        void Play(std::uint64_t entity, const AudioSource& source);
        void Pause(std::uint64_t entity);
        void Resume(std::uint64_t entity);
        void Stop(std::uint64_t entity);
        sf::SoundSource::Status Status(std::uint64_t entity) const;
        std::size_t VoiceCount() const { return _voices.size(); }
        void Clear() noexcept { _voices.clear(); _suspended.clear(); }
        void SetMasterVolume(float volume);
        void PauseAll();
        void ResumeAll();

    private:
        struct Voice
        {
            std::shared_ptr<const sf::SoundBuffer> Buffer;
            sf::Sound Sound;
            float SourceVolume;
            Voice(const AudioSource& source, float masterVolume);
        };
        std::map<std::uint64_t, std::unique_ptr<Voice>> _voices;
        std::set<std::uint64_t> _suspended;
        float _masterVolume = 100;
    };
}
