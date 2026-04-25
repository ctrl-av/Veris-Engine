#pragma once

#include <stdint.h>
#include <stddef.h>

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

typedef struct VerisPoint
{
    float X;
    float Y;
} VerisPoint;

typedef struct VerisSprite
{
    float OriginX;
    float OriginY;
    uint32_t Tint;
    int32_t Layer;
    int32_t Visible;
    int32_t UseRectangle;
    int32_t RectangleX;
    int32_t RectangleY;
    int32_t RectangleWidth;
    int32_t RectangleHeight;
} VerisSprite;

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
VERIS_API int32_t __cdecl veris_duplicate_entity(uint64_t engine, uint64_t entity, uint64_t* duplicate);
VERIS_API int32_t __cdecl veris_rename_entity(uint64_t engine, uint64_t entity, const char* name);
VERIS_API int32_t __cdecl veris_entity_parent(uint64_t engine, uint64_t entity, uint64_t* parent);
VERIS_API int32_t __cdecl veris_set_parent(uint64_t engine, uint64_t entity, uint64_t parent);
VERIS_API int32_t __cdecl veris_pick_entity(uint64_t engine, int32_t x, int32_t y, uint64_t* entity);
VERIS_API int32_t __cdecl veris_parent_coordinates(uint64_t engine, uint64_t entity, int32_t x, int32_t y, VerisPoint* local);
VERIS_API int32_t __cdecl veris_entity_origin(uint64_t engine, uint64_t entity, VerisPoint* pixel);
VERIS_API int32_t __cdecl veris_entity_corners(uint64_t engine, uint64_t entity, VerisPoint* corners, uint32_t count);
VERIS_API int32_t __cdecl veris_set_content_root(uint64_t engine, const wchar_t* directory);
VERIS_API int32_t __cdecl veris_set_sprite(uint64_t engine, uint64_t entity, const char* texture, const VerisSprite* sprite);
VERIS_API int32_t __cdecl veris_get_sprite(uint64_t engine, uint64_t entity, VerisSprite* sprite, int32_t* present);
VERIS_API int32_t __cdecl veris_sprite_path(uint64_t engine, uint64_t entity, char* utf8, uint32_t capacity, uint32_t* required);
VERIS_API int32_t __cdecl veris_remove_sprite(uint64_t engine, uint64_t entity);
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
