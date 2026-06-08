shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_schlick_ggx;

uniform vec3 albedo : source_color = vec3(1.0);
uniform float spawn_progress : hint_range(0.0, 1.0) = 1.0;

void fragment() {
    // Standard color
    ALBEDO = albedo;
    
    // Use a height-based dissolve or noise pattern
    if (UV.y > spawn_progress) {
        discard; // Erases the pixels if they haven't "spawned" yet
    }
    
    // Add a glowing neon edge right at the line where they are appearing
    if (UV.y > spawn_progress - 0.05 && UV.y < spawn_progress) {
        EMISSION = vec3(0.0, 0.8, 1.0) * 5.0; // Bright blue glow line
    }
}