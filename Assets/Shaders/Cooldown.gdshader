shader_type canvas_item;

uniform float Progress = 1.0;

void fragment() {
	vec4 color = COLOR;//texture(Texture, UV);
	float lum = color.r * 0.299 + color.g * 0.587 + color.b * 0.114;
	vec4 disabled = vec4(lum, lum, lum, color.a);
	vec4 shadow = disabled * 0.25;
	shadow.a = disabled.a;
	
	vec2 pos = UV - vec2(0.5, 0.5);
	float angle = mod(
		radians(450) + atan(pos.y, pos.x), 
		radians(360)
		);
	float value = angle / radians(360);
	float alpha = step(value, Progress);
	
	vec4 withProgress = mix(shadow, color, alpha);
	
	COLOR = withProgress;
}