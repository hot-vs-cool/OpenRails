// COPYRIGHT 2010, 2013 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

////////////////////////////////////////////////////////////////////////////////
//                   P O P U P   W I N D O W   S H A D E R                    //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

float4x4 World;         // model -> world
float4x4 WorldViewProjection;  // model -> world -> view -> projection
float3   GlassColor;
float2   ScreenSize;
texture  ScreenTexture;
texture  WindowTexture;

sampler ScreenSampler = sampler_state
{
	Texture = (ScreenTexture);
	MagFilter = Point;
	MinFilter = Point;
	MipFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};

sampler WindowSampler = sampler_state
{
	Texture = (WindowTexture);
	MagFilter = Point;
	MinFilter = Point;
	MipFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};

////////////////////    V E R T E X   I N P U T S    ///////////////////////////

struct VERTEX_INPUT
{
	float4 Position     : POSITION;
	float2 TexCoords    : TEXCOORD0;
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position      : POSITION;
	float4 TexCoords_Pos : TEXCOORD0;
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

VERTEX_OUTPUT VSPopupWindow(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	Out.Position = mul(In.Position, WorldViewProjection);
	Out.TexCoords_Pos.xy = In.TexCoords;

	return Out;
}

VERTEX_OUTPUT VSPopupWindowGlass(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = VSPopupWindow(In);

	Out.TexCoords_Pos.zw = mul(In.Position, World).xy / ScreenSize;

	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

float4 PSPopupWindow(in VERTEX_OUTPUT In) : COLOR
{
	float4 Color = tex2D(WindowSampler, In.TexCoords_Pos.xy);
	float Mask = tex2D(WindowSampler, In.TexCoords_Pos.xy + float2(0.5, 0.0)).r;
	float4 ScreenColor = float4(GlassColor, Mask);
	return lerp(ScreenColor, Color, Color.a);
}

float4 PSPopupWindowGlass(in VERTEX_OUTPUT In) : COLOR
{
	float4 Color = tex2D(WindowSampler, In.TexCoords_Pos.xy);
	float Mask = tex2D(WindowSampler, In.TexCoords_Pos.xy + float2(0.5, 0.0)).r;
	float3 ScreenColor = tex2D(ScreenSampler, In.TexCoords_Pos.zw);
	float3 ScreenColor1 = tex2D(ScreenSampler, In.TexCoords_Pos.zw + float2(+1 / ScreenSize.x, +1 / ScreenSize.y));
	float3 ScreenColor2 = tex2D(ScreenSampler, In.TexCoords_Pos.zw + float2(+1 / ScreenSize.x,  0 / ScreenSize.y));
	float3 ScreenColor3 = tex2D(ScreenSampler, In.TexCoords_Pos.zw + float2(+1 / ScreenSize.x, -1 / ScreenSize.y));
	float3 ScreenColor4 = tex2D(ScreenSampler, In.TexCoords_Pos.zw + float2( 0 / ScreenSize.x, +1 / ScreenSize.y));
	float3 ScreenColor5 = tex2D(ScreenSampler, In.TexCoords_Pos.zw + float2( 0 / ScreenSize.x,  0 / ScreenSize.y));
	float3 ScreenColor6 = tex2D(ScreenSampler, In.TexCoords_Pos.zw + float2( 0 / ScreenSize.x, -1 / ScreenSize.y));
	float3 ScreenColor7 = tex2D(ScreenSampler, In.TexCoords_Pos.zw + float2(-1 / ScreenSize.x, +1 / ScreenSize.y));
	float3 ScreenColor8 = tex2D(ScreenSampler, In.TexCoords_Pos.zw + float2(-1 / ScreenSize.x,  0 / ScreenSize.y));
	float3 ScreenColor9 = tex2D(ScreenSampler, In.TexCoords_Pos.zw + float2(-1 / ScreenSize.x, -1 / ScreenSize.y));
	ScreenColor = lerp(ScreenColor, (22 * GlassColor + ScreenColor + ScreenColor1 + ScreenColor2 + ScreenColor3 + ScreenColor4 + ScreenColor5 + ScreenColor6 + ScreenColor7 + ScreenColor8 + ScreenColor9) / 32, Mask);
	return float4(lerp(ScreenColor, Color.rgb, Color.a), 1);
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

technique PopupWindow {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSPopupWindow();
		PixelShader = compile ps_2_0 PSPopupWindow();
	}
}

technique PopupWindowGlass {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSPopupWindowGlass();
		PixelShader = compile ps_2_0 PSPopupWindowGlass();
	}
}
