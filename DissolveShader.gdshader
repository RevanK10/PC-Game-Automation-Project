shader_type spatial;
render_mode blend_mix, depth_draw_always, cull_disabled;

// --- Enemy Visual Profile Inputs ---
uniform vec4 albedo_color : source_color = vec4(0.5, 0.5, 0.5, 1.0);
uniform sampler2D albedo_texture : hint_default_white;
uniform float metallic : hint_range(0.0, 1.0) = 0.0;
uniform float roughness : hint_range(0.0, 1.0) = 0.5;
uniform float rim_strength : hint_range(0.0, 1.0) = 0.0;

// --- Emission Details ---
uniform bool emission_enabled = false;
uniform sampler2D emission_texture : hint_default_black;
uniform vec4 emission_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);

// --- Dissolve Controls ---
uniform float spawn_progress : hint_range(-0.1, 1.1) = 1.1;
uniform vec3 glow_color : source_color = vec3(0.0, 0.7, 1.0);

void fragment() {
	// 1. Map Core Enemy Material Settings
	vec4 tex_color = texture(albedo_texture, UV);
	ALBEDO = albedo_color.rgb * tex_color.rgb;
	METALLIC = metallic;
	ROUGHNESS = roughness;

	// Fallback Rim
	float rim = 1.0 - dot(NORMAL, VIEW);
	SPECULAR = mix(0.5, 1.0, rim * rim_strength);

	if (emission_enabled) {
		vec4 em_tex = texture(emission_texture, UV);
		EMISSION = emission_color.rgb * em_tex.rgb;
	}

	// 2. Dissolve Clipping Math
	float height_check = 1.0 - UV.y; // 0.0 at bottom, 1.0 at top
	
	// Physically erase pixels above the progress line
	if (height_check > spawn_progress) {
		discard;
	}
	
	// Burn bright edge right at the split line
	if (height_check > spawn_progress - 0.04) {
		ALBEDO = glow_color;
		EMISSION = glow_color * 8.0; // Drive intense glow into Volumetric Fog
	}
}