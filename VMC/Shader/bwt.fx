sampler2D input : register(s0);
float4 main(float2 uv : TEXCOORD) : COLOR
{
	float4 color = tex2D(input, uv);
	float average = (color.r + color.g + color.b) / 3.0f;
	color.rgb = average;
	color.a *= 0.8f;


	return color;
}