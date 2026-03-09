#pragma once

#include <stdint.h>

#ifdef VERIS_INTEROP_EXPORTS
#define VERIS_API __declspec(dllexport)
#else
#define VERIS_API __declspec(dllimport)
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef struct VerisTransform
{
    float PositionX;
    float PositionY;
    float ScaleX;
    float ScaleY;
    float Rotation;
} VerisTransform;

enum VerisStatus
{
    VerisOk = 0,
    VerisInvalidArgument = 1,
    VerisNotFound = 2,
    VerisBufferTooSmall = 3,
    VerisFailure = 4
};

VERIS_API int32_t __cdecl veris_create(uint32_t width, uint32_t height, uint64_t* engine);
VERIS_API int32_t __cdecl veris_destroy(uint64_t engine);
VERIS_API int32_t __cdecl veris_resize(uint64_t engine, uint32_t width, uint32_t height);
VERIS_API int32_t __cdecl veris_update(uint64_t engine);
VERIS_API int32_t __cdecl veris_create_entity(uint64_t engine, const char* name, uint64_t* entity);
VERIS_API int32_t __cdecl veris_destroy_entity(uint64_t engine, uint64_t entity);
VERIS_API int32_t __cdecl veris_entity_count(uint64_t engine, uint64_t* count);
VERIS_API int32_t __cdecl veris_entity_at(uint64_t engine, uint64_t index, uint64_t* entity);
VERIS_API int32_t __cdecl veris_entity_name(uint64_t engine, uint64_t entity, char* utf8, uint32_t capacity, uint32_t* required);
VERIS_API int32_t __cdecl veris_get_transform(uint64_t engine, uint64_t entity, VerisTransform* transform);
VERIS_API int32_t __cdecl veris_set_transform(uint64_t engine, uint64_t entity, const VerisTransform* transform);
VERIS_API int32_t __cdecl veris_copy_frame(uint64_t engine, uint8_t* rgba, uint64_t capacity, uint64_t* required);
VERIS_API uint32_t __cdecl veris_error_message(char* utf8, uint32_t capacity);

#ifdef __cplusplus
}
#endif
