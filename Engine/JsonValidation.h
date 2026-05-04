#pragma once

#include <nlohmann/json.hpp>
#include <cmath>
#include <limits>
#include <stdexcept>
#include <set>
#include <vector>

namespace Veris::Json
{
    using Document = nlohmann::json;

    inline Document Parse(std::string_view json)
    {
        std::vector<std::set<std::string>> fields;
        return Document::parse(json, [&](int, Document::parse_event_t event, Document& parsed)
        {
            if (event == Document::parse_event_t::object_start) fields.emplace_back();
            else if (event == Document::parse_event_t::key && !fields.back().insert(parsed.get<std::string>()).second)
                throw std::invalid_argument("Duplicate JSON field: " + parsed.get<std::string>());
            else if (event == Document::parse_event_t::object_end) fields.pop_back();
            return true;
        });
    }

    inline void Fields(const Document& object, std::initializer_list<const char*> required, std::initializer_list<const char*> optional = {})
    {
        if (!object.is_object()) throw std::invalid_argument("Expected a JSON object.");
        for (const auto field : required) if (!object.contains(field)) throw std::invalid_argument(std::string("Missing JSON field: ") + field);
        for (const auto& field : object.items())
        {
            bool known = false;
            for (const auto name : required) known = known || field.key() == name;
            for (const auto name : optional) known = known || field.key() == name;
            if (!known) throw std::invalid_argument("Unknown JSON field: " + field.key());
        }
    }

    inline int Integer(const Document& number)
    {
        if (!number.is_number_integer() || number.get<double>() < std::numeric_limits<int>::min() || number.get<double>() > std::numeric_limits<int>::max())
            throw std::invalid_argument("Expected an integer within the 32-bit range.");
        return number.get<int>();
    }

    inline float Number(const Document& number)
    {
        if (!number.is_number() || !std::isfinite(number.get<float>())) throw std::invalid_argument("Expected a finite number.");
        return number.get<float>();
    }
}
