shader_type canvas_item;

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;
uniform float size : hint_range(0.0, 5.0, 0.1) = 3.5;
uniform float power : hint_range(0.0, 1.0, 0.05) = 0.25;
uniform float brightness : hint_range(0.0, 15.0, 0.05) = 1;
uniform float hdr_threshold : hint_range(0.0, 1.0, 0.01) = 0.01;
render_mode blend_mix;

vec4 sample_glow_pixel(sampler2D tex, vec2 uv) {
	//float hdr_threshold = 0.01; // Exagerated, almost everything will glow
	return max((textureLod(tex, uv, size) * brightness - hdr_threshold), vec4(0.0));
}

void fragment() {
	vec4 col = texture(SCREEN_TEXTURE, SCREEN_UV);
	vec4 filter = texture(TEXTURE, UV);
	vec2 ps = SCREEN_PIXEL_SIZE;
	// Get blurred color from pixels considered glowing
	vec4 col0 = sample_glow_pixel(SCREEN_TEXTURE, SCREEN_UV + vec2(-ps.x, 0));
	vec4 col1 = sample_glow_pixel(SCREEN_TEXTURE, SCREEN_UV + vec2(ps.x, 0));
	vec4 col2 = sample_glow_pixel(SCREEN_TEXTURE, SCREEN_UV + vec2(0, -ps.y));
	vec4 col3 = sample_glow_pixel(SCREEN_TEXTURE, SCREEN_UV + vec2(0, ps.y));

	vec4 glowing_col = power * (col0 + col1 + col2 + col3);

	COLOR = vec4(col.rgb + glowing_col.rgb * filter.a, col.a);
}