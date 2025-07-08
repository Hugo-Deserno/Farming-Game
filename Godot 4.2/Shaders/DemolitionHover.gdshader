shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_lambert, specular_schlick_ggx, unshaded;

// Varyings
varying vec2 Size;

uniform sampler2D Albedo : source_color, hint_default_transparent, filter_nearest;
uniform sampler2D Alpha : filter_nearest;

uniform vec2 Scale;
uniform vec2 Direction;
uniform float Speed;


void fragment() {
	// Create The Uv And correct it with the ratio so that the texture doesnt stretch
	float Ratio =  VIEWPORT_SIZE.x / VIEWPORT_SIZE.y;
	vec2 ScreenUv = vec2(SCREEN_UV.x,SCREEN_UV.y / Ratio);
	vec2 Uv = (ScreenUv) * Scale ;

	vec4 AlbedoOutput = texture(Albedo, Uv + (Direction * TIME * Speed));
	vec4 AlphaOutput = texture(Alpha, Uv + (Direction * TIME * Speed));
	
	ALBEDO = vec3(AlbedoOutput.xyz);
	ALPHA = AlphaOutput.x;
}
