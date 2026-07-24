shader_type canvas_item; // Changed to 2D

// In 2D, SCREEN_TEXTURE is built-in, but we still define the filter hint
uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

uniform vec3 colorCount = vec3(32.0); 
uniform vec3 colorShift = vec3(1.0); 

uniform bool doShiftFirst = false; 
uniform bool includeAlpha = true; 

vec3 hsv_to_rgb(vec3 color) {
	vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	vec3 p = abs(fract(color.xxx + K.xyz) * 6.0 - K.www);
	return color.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), color.y);
}

vec3 rgb_to_hsv(vec3 color) {
	vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
	vec4 p = mix(vec4(color.bg, K.wz), vec4(color.gb, K.xy), step(color.b, color.g));
	vec4 q = mix(vec4(p.xyw, color.r), vec4(color.r, p.yzx), step(p.x, color.r));
	float d = q.x - min(q.w, q.y);
	float e = 1.0e-10;
	return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

void fragment() {
	// In 2D canvas shaders, we use SCREEN_UV instead of UV for full-screen textures
	vec4 tex = texture(screen_texture, SCREEN_UV); 
	vec3 hvs = rgb_to_hsv(tex.xyz);
	
	if (doShiftFirst){
		hvs.xyz = (hvs * colorShift);
	}
	
	hvs.x =  floor(hvs.x * colorCount.x) / colorCount.x;
	hvs.y =  floor(hvs.y * colorCount.y) / colorCount.y;
	hvs.z =  floor(hvs.z * colorCount.z) / colorCount.z;
	
	if (!doShiftFirst){
		hvs.xyz = (hvs * colorShift);
	}

	// In 2D, we output to COLOR instead of ALBEDO
	COLOR.rgb = hsv_to_rgb(hvs.xyz);
	
	if (!includeAlpha){
		COLOR.a = tex.a;
	} else {
		COLOR.a = 1.0;
	}
}
