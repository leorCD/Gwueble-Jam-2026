shader_type spatial;
render_mode unshaded;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear;

uniform vec4 tint : source_color = vec4(1.0);
uniform float wave_speed = 3.0;
uniform float wave_freq = 10.0;
uniform float wave_width = 1.0;

void fragment() {
	vec2 wave_uv_offset = vec2(0.0);

	wave_uv_offset.x = cos(
		(TIME * wave_speed) + UV.x + UV.y * wave_freq * 2.0
	) * wave_width * 0.01;

	vec3 screen_color = texture(
		screen_texture,
		SCREEN_UV + wave_uv_offset
	).rgb;

	ALBEDO = screen_color * tint.rgb;
}