shader_type canvas_item;

uniform float strength : hint_range(0.0, 1.0, 0.1);

void fragment() {
	vec4 pixelcolor = texture(TEXTURE, UV);
	float grayscaleValue = 0.299 * pixelcolor.r + 0.587 * pixelcolor.g + 0.114 * pixelcolor.b;
	vec4 grayscale = vec4(grayscaleValue, grayscaleValue, grayscaleValue, pixelcolor.a);
	
	COLOR.rgba = mix(pixelcolor, grayscale, strength);
}
