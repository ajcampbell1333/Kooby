Shader "Hidden/OutlineMaskSelective"
{
	Properties
	{
		_TargetMaterialIDs ("Target Material IDs", Vector) = (0,0,0,0)
		_TargetMaterialCount ("Target Material Count", Int) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue"="Geometry" }
		Cull Off
		ZWrite On
		ZTest LEqual
		ColorMask RGB

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
			};

			float4 _TargetMaterialIDs;
			int _TargetMaterialCount;

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// Check if current material ID matches any target material ID
				// This is a simplified approach - in practice you'd need to pass material IDs
				// For now, just render white (this shader will be used only for target materials)
				return fixed4(1,1,1,1);
			}
			ENDCG
		}
	}
}
