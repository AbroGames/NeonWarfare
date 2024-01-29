shader_type canvas_item;

// 0 - full color, 1 - full grayscale
uniform float colorMaskFactor : hint_range(0, 1) = 0.0f;
uniform vec4 colorMask : source_color = vec4(1,1,1,1);

// 0 - full color, 1 - full grayscale
uniform float grayscaleFactor : hint_range(0, 1) = 0.0f;

varying flat vec4 modulate;
void vertex() {
	modulate = COLOR;
}

vec4 toGrayscale(vec4 source){
	float luminace = 0.3*source.r + 0.59*source.g + 0.11*source.b;
	vec4 result = vec4(luminace, luminace, luminace, source.a);
	return mix(source, result, grayscaleFactor);
}

vec4 applyMask(vec4 source){
	float alphaFactor = colorMask.a;
	vec4 result = mix(source, colorMask, colorMaskFactor * alphaFactor);
	result.a = source.a;
	return result;
}

void fragment() {
	vec4 color = COLOR;//texture(TEXTURE, UV);
	
	color = toGrayscale(color);
	color = applyMask(color);
	
	COLOR = color;
}


