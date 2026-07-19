shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear_mipmap;
uniform float blur_radius : hint_range(0.0, 6.0, 0.1) = 2.2;

void fragment() {
    vec2 offset = SCREEN_PIXEL_SIZE * blur_radius;
    vec4 color = texture(screen_texture, SCREEN_UV) * 0.20;
    color += texture(screen_texture, SCREEN_UV + vec2(offset.x, 0.0)) * 0.12;
    color += texture(screen_texture, SCREEN_UV - vec2(offset.x, 0.0)) * 0.12;
    color += texture(screen_texture, SCREEN_UV + vec2(0.0, offset.y)) * 0.12;
    color += texture(screen_texture, SCREEN_UV - vec2(0.0, offset.y)) * 0.12;
    color += texture(screen_texture, SCREEN_UV + offset) * 0.08;
    color += texture(screen_texture, SCREEN_UV - offset) * 0.08;
    color += texture(screen_texture, SCREEN_UV + vec2(offset.x, -offset.y)) * 0.08;
    color += texture(screen_texture, SCREEN_UV + vec2(-offset.x, offset.y)) * 0.08;
    COLOR = vec4(color.rgb * vec3(1.03, 0.98, 0.94), 1.0);
}

