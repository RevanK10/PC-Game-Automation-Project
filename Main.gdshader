shader_type canvas_item;

// FIX: Explicitly declare the screen texture sampler as required by Godot 4
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

void fragment() {
    // 1. Grab the current screen texture behind this rectangle
    vec4 color = texture(SCREEN_TEXTURE, SCREEN_UV);
    
    // 2. Calculate distance from the center of the screen
    float uv_dist = distance(UV, vec2(0.5));
    
    // 3. Create a smooth dark vignette edge
    float vignette = smoothstep(0.8, 0.4, uv_dist);
    
    // 4. Mix the original game colors with the vignette and boost contrast
    color.rgb *= vignette;
    color.rgb = pow(color.rgb, vec3(1.1)); // Slight contrast boost
    
    COLOR = color;
}