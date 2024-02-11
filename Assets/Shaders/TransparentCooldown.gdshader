shader_type canvas_item;

uniform float Progress = 1.0;

void fragment() {
	vec2 pos = UV - vec2(0.5, 0.5);
	float angle = mod(
		radians(450) + atan(pos.y, pos.x), 
		radians(360)
		);
	float value = angle / radians(360);
	float curAlpha = COLOR.a;
	float alpha = step(value, Progress);
	
	COLOR.a = curAlpha * (1.0 - alpha);
}