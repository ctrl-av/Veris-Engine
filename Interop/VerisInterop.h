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
VERIS_API int32_t __cdecl veris_capture_scene(uint64_t engine, uint64_t* snapshot);
VERIS_API int32_t __cdecl veris_restore_scene(uint64_t engine, uint64_t snapshot);
VERIS_API int32_t __cdecl veris_matches_scene(uint64_t engine, uint64_t snapshot, int32_t* matches);
VERIS_API int32_t __cdecl veris_release_snapshot(uint64_t snapshot);
VERIS_API int32_t __cdecl veris_serialize_scene(uint64_t engine, char* utf8, uint32_t capacity, uint32_t* required);
VERIS_API int32_t __cdecl veris_deserialize_scene(uint64_t engine, const char* utf8);
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
VERIS_API int32_t __cdecl veris_load_asset_catalog(uint64_t engine, const wchar_t* manifest);
VERIS_API int32_t __cdecl veris_validate_texture(const wchar_t* file);
VERIS_API int32_t __cdecl veris_reload_texture(uint64_t engine, const char* path);
VERIS_API int32_t __cdecl veris_set_animation(uint64_t engine, uint64_t entity, const char* json);
VERIS_API int32_t __cdecl veris_animation(uint64_t engine, uint64_t entity, char* utf8, uint32_t capacity, uint32_t* required);
VERIS_API int32_t __cdecl veris_advance_animation(uint64_t engine, uint64_t entity, double seconds);
VERIS_API int32_t __cdecl veris_set_audio(uint64_t engine, uint64_t entity, const char* json);
VERIS_API int32_t __cdecl veris_audio(uint64_t engine, uint64_t entity, char* utf8, uint32_t capacity, uint32_t* required);
VERIS_API int32_t __cdecl veris_play_audio(uint64_t engine, uint64_t entity);
VERIS_API int32_t __cdecl veris_pause_audio(uint64_t engine, uint64_t entity);
VERIS_API int32_t __cdecl veris_resume_audio(uint64_t engine, uint64_t entity);
VERIS_API int32_t __cdecl veris_stop_audio(uint64_t engine, uint64_t entity);
VERIS_API int32_t __cdecl veris_audio_status(uint64_t engine, uint64_t entity, int32_t* status);
VERIS_API int32_t __cdecl veris_validate_audio(const wchar_t* path);
VERIS_API int32_t __cdecl veris_reload_audio(uint64_t engine, const char* path);
VERIS_API int32_t __cdecl veris_set_physics(uint64_t engine, uint64_t entity, const char* json);
VERIS_API int32_t __cdecl veris_physics(uint64_t engine, uint64_t entity, char* utf8, uint32_t capacity, uint32_t* required);
VERIS_API int32_t __cdecl veris_fixed_update(uint64_t engine, double seconds);
VERIS_API int32_t __cdecl veris_set_gravity(uint64_t engine, float x, float y);
VERIS_API int32_t __cdecl veris_apply_impulse(uint64_t engine, uint64_t entity, float x, float y);
VERIS_API int32_t __cdecl veris_set_physics_debug(uint64_t engine, int32_t enabled);
VERIS_API int32_t __cdecl veris_contacts(uint64_t engine, char* utf8, uint32_t capacity, uint32_t* required);
VERIS_API int32_t __cdecl veris_asset_usage(uint64_t engine, const char* asset, uint32_t* stable, uint32_t* legacy);
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



VERIS_API int32_t __cdecl veris_set_script_root(uint64_t engine, const wchar_t* directory);
VERIS_API int32_t __cdecl veris_set_script(uint64_t engine, uint64_t entity, const char* json);
VERIS_API int32_t __cdecl veris_script_log(uint64_t engine, char* utf8, uint32_t capacity, uint32_t* required);

VERIS_API int32_t __cdecl veris_configure_input(uint64_t engine, const wchar_t* directory);
VERIS_API int32_t __cdecl veris_named_input(uint64_t engine, const char* binding, int32_t down);
VERIS_API int32_t __cdecl veris_game_step(uint64_t engine, double seconds);
VERIS_API int32_t __cdecl veris_release_input(uint64_t engine);
VERIS_API int32_t __cdecl veris_pause_audio_all(uint64_t engine);
VERIS_API int32_t __cdecl veris_resume_audio_all(uint64_t engine);
VERIS_API int32_t __cdecl veris_script_path(uint64_t engine, uint64_t entity, char* utf8, uint32_t capacity, uint32_t* required);

VERIS_API int32_t __cdecl veris_load_scene_file(uint64_t engine, const char* path);
VERIS_API int32_t __cdecl veris_validate_scene_file(uint64_t engine, const char* path);

VERIS_API int32_t __cdecl veris_serialize_prefab(uint64_t engine, uint64_t root, char* utf8, uint32_t capacity, uint32_t* required);
VERIS_API int32_t __cdecl veris_instantiate_prefab(uint64_t engine, const char* path, float x, float y, uint64_t* root);
VERIS_API int32_t __cdecl veris_reapply_prefab(uint64_t engine, uint64_t root, const char* path);
VERIS_API int32_t __cdecl veris_unpack_prefab(uint64_t engine, uint64_t root);
VERIS_API int32_t __cdecl veris_prefab_source(uint64_t engine, uint64_t root, char* utf8, uint32_t capacity, uint32_t* required);

VERIS_API int32_t __cdecl veris_configure_simulation(uint64_t engine, float gravityX, float gravityY, float masterVolume);
VERIS_API int32_t __cdecl veris_validate_input_json(const char* json);

#ifdef __cplusplus
}
#endif
