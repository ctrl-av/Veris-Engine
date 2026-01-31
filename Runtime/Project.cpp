#include "Project.h"
#include <SFML/System/String.hpp>
#include <nlohmann/json.hpp>
#include <Windows.h>
#include <objbase.h>
#include <algorithm>
#include <cwctype>
#include <fstream>
#include <limits>
#include <stdexcept>
#include <unordered_set>

namespace
{
    const std::unordered_set<std::string> ProjectKeys{"formatVersion", "projectId", "name", "templateId", "contentDirectory", "gameCodeDirectory", "startupScene", "windowWidth", "windowHeight", "windowMode", "windowResizable", "verticalSync"};
    const std::wstring Whitespace = L" \t\n\v\f\r\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000";
    const std::unordered_set<std::wstring> ReservedNames{
        L"CON", L"PRN", L"AUX", L"NUL", L"CONIN$", L"CONOUT$",
        L"COM1", L"COM2", L"COM3", L"COM4", L"COM5", L"COM6", L"COM7", L"COM8", L"COM9", L"COM\u00b9", L"COM\u00b2", L"COM\u00b3",
        L"LPT1", L"LPT2", L"LPT3", L"LPT4", L"LPT5", L"LPT6", L"LPT7", L"LPT8", L"LPT9", L"LPT\u00b9", L"LPT\u00b2", L"LPT\u00b3"};

    void ValidateDocument(const nlohmann::json& document);
    std::string ReadText(const nlohmann::json& document, const char* key);
    std::filesystem::path ReadRelativePath(const nlohmann::json& document, const char* key);
    std::filesystem::path ResolveDirectory(const std::filesystem::path& root, const std::filesystem::path& relativePath);
    unsigned int ReadDimension(const nlohmann::json& document, const char* key, unsigned int defaultDimension);
    bool ReadBoolean(const nlohmann::json& document, const char* key);
    Veris::WindowMode ReadWindowMode(const nlohmann::json& document);
}

Veris::RuntimeProject Veris::LoadProject(const std::filesystem::path& projectFile)
{
    const auto fullProjectFile = std::filesystem::absolute(projectFile).lexically_normal();
    if (_wcsicmp(fullProjectFile.extension().c_str(), L".veris") != 0) throw std::runtime_error("Choose a .veris project file.");
    std::ifstream projectStream(fullProjectFile, std::ios::binary);
    if (!projectStream) throw std::runtime_error("Cannot read the project file.");
    const auto document = nlohmann::json::parse(projectStream);
    ValidateDocument(document);
    const auto root = fullProjectFile.parent_path();
    const auto content = ResolveDirectory(root, ReadRelativePath(document, "contentDirectory"));
    const auto gameCode = ResolveDirectory(root, ReadRelativePath(document, "gameCodeDirectory"));
    if (_wcsicmp(content.c_str(), gameCode.c_str()) == 0) throw std::runtime_error("Content and game code directories must be distinct.");
    return {fullProjectFile, ReadText(document, "name"), content, gameCode,
        {{ReadDimension(document, "windowWidth", 1280), ReadDimension(document, "windowHeight", 720)},
            ReadWindowMode(document), ReadBoolean(document, "windowResizable"), ReadBoolean(document, "verticalSync")}};
}

namespace
{
    std::string ReadText(const nlohmann::json& document, const char* key)
    {
        if (!document.contains(key) || !document.at(key).is_string()) throw std::runtime_error(std::string("Project requires a string: ") + key);
        auto text = document.at(key).get<std::string>();
        const auto wideText = sf::String::fromUtf8(text.begin(), text.end()).toWideString();
        if (wideText.find_first_not_of(Whitespace) == std::wstring::npos)
            throw std::runtime_error(std::string("Project field cannot be blank: ") + key);
        return text;
    }

    void ValidateIdentity(const nlohmann::json& document)
    {
        const auto identityText = ReadText(document, "projectId");
        if (identityText.size() != 36) throw std::runtime_error("Project identity must be a nonempty GUID.");
        const auto identityWide = L"{" + std::wstring(identityText.begin(), identityText.end()) + L"}";
        GUID identity{};
        const GUID emptyIdentity{};
        if (FAILED(IIDFromString(identityWide.c_str(), &identity)) || InlineIsEqualGUID(identity, emptyIdentity))
            throw std::runtime_error("Project identity must be a nonempty GUID.");
    }

    void ValidateDocument(const nlohmann::json& document)
    {
        if (!document.is_object()) throw std::runtime_error("Project document must be a JSON object.");
        for (auto field = document.begin(); field != document.end(); ++field)
            if (ProjectKeys.find(field.key()) == ProjectKeys.end()) throw std::runtime_error("Unknown project field: " + field.key());
        if (!document.contains("formatVersion") || !document.at("formatVersion").is_number_integer() || document.at("formatVersion") != 1)
            throw std::runtime_error("Unsupported or missing project format version.");
        ValidateIdentity(document);
        ReadText(document, "templateId");
        if (document.contains("startupScene") && !document.at("startupScene").is_null()) ReadRelativePath(document, "startupScene");
    }

    void ValidateSegment(const std::wstring& segment)
    {
        if (segment.empty() || segment == L"." || segment == L".." || segment.back() == L' ' || segment.back() == L'.' ||
            segment.find_first_of(L"<>:\"|?*") != std::wstring::npos ||
            std::any_of(segment.begin(), segment.end(), [](wchar_t character) { return character < 32; }))
            throw std::runtime_error("Invalid project-relative path segment.");
        auto stem = segment.substr(0, segment.find(L'.'));
        for (auto& character : stem) character = static_cast<wchar_t>(std::towupper(character));
        if (ReservedNames.find(stem) != ReservedNames.end()) throw std::runtime_error("Project paths cannot contain reserved Windows device names.");
    }

    std::filesystem::path ReadRelativePath(const nlohmann::json& document, const char* key)
    {
        const auto path = std::filesystem::u8path(ReadText(document, key));
        if (path.has_root_path()) throw std::runtime_error("Project paths must be relative.");
        const auto& text = path.native();
        std::size_t start = 0;
        while (true)
        {
            const auto end = text.find_first_of(L"/\\", start);
            ValidateSegment(text.substr(start, end == std::wstring::npos ? end : end - start));
            if (end == std::wstring::npos) break;
            start = end + 1;
        }
        return path;
    }

    std::filesystem::path ResolveDirectory(const std::filesystem::path& root, const std::filesystem::path& relativePath)
    {
        auto directory = root;
        for (const auto& segment : relativePath)
        {
            directory /= segment;
            const auto attributes = GetFileAttributesW(directory.c_str());
            if (attributes == INVALID_FILE_ATTRIBUTES || !(attributes & FILE_ATTRIBUTE_DIRECTORY) || (attributes & FILE_ATTRIBUTE_REPARSE_POINT))
                throw std::runtime_error("Project directory is missing, inaccessible, or linked: " + directory.u8string());
        }
        return directory;
    }

    unsigned int ReadDimension(const nlohmann::json& document, const char* key, unsigned int defaultDimension)
    {
        if (!document.contains(key)) return defaultDimension;
        const auto& dimension = document.at(key);
        if (!dimension.is_number_integer() || dimension < 1 || dimension > std::numeric_limits<int>::max())
            throw std::runtime_error(std::string("Window dimension must be a positive 32-bit integer: ") + key);
        return dimension.get<unsigned int>();
    }

    bool ReadBoolean(const nlohmann::json& document, const char* key)
    {
        if (!document.contains(key)) return true;
        if (!document.at(key).is_boolean()) throw std::runtime_error(std::string("Window setting must be a boolean: ") + key);
        return document.at(key).get<bool>();
    }

    Veris::WindowMode ReadWindowMode(const nlohmann::json& document)
    {
        const auto mode = document.contains("windowMode") ? ReadText(document, "windowMode") : "windowed";
        if (mode == "windowed") return Veris::WindowMode::Windowed;
        if (mode == "fullscreen") return Veris::WindowMode::Fullscreen;
        throw std::runtime_error("Window mode must be windowed or fullscreen.");
    }
}
