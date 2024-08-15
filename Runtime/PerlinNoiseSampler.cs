using System;
using System.Threading.Tasks;
using Lvl3Mage.EditorEnhancements.Runtime;
using UnityEngine;
using Lvl3Mage.MathToolkit;

namespace Lvl3Mage.NoiseGenerationToolkit
{
	[Serializable]
	public class PerlinNoiseSampler
	{
		public void Reset(){
			noiseScale = 1;
			scrollSpeed = Vector2.zero;
			offset = Vector2.zero;
			octaves = 1;
			lacunarity = 1;
			persistence = 0.5f;
		}
		public PerlinNoiseSampler(){
			noiseScale = 1;
			scrollSpeed = Vector2.zero;
			offset = Vector2.zero;
			octaves = 1;
			lacunarity = 1;
			persistence = 0.5f;
		}
		//Source settings
		[Header("Source settings")]
		[SerializeField] float noiseScale;
		[SerializeField] Vector2 scrollSpeed;
		[SerializeField] Vector2 offset;

		[Tooltip("Amount of individual octaves the noise has")]
		[SerializeField] [Min(1)] int octaves = 1;
		[SerializeField] bool randomizeOctaveOffsets;

		[Tooltip("How much the frequency of each octave increases with octave amount")]
		[SerializeField] [Min(1)] float lacunarity = 1;

		[Tooltip("How much the amplitude of each octave decreases with octave amount")]
		[SerializeField] [Range(0,1)] float persistence;
		
		[SerializeField]
		[Texture2DPreview(hideProperty: true,
			showDropdown: true,
			text: "Perlin Noise Preview:",
			width: 200,
			updateMethodName: nameof(UpdatePreview))]
		Texture2D preview;
		void UpdatePreview()
		{
			if (!preview || preview.width != 50 || preview.height != 50){
				preview = new Texture2D(50,50);
			}
			preview.filterMode = FilterMode.Point;
			
			Vector2Int size = new(preview.width, preview.height);
			Color[] clrs = new Color[size.x*size.y];
			Parallel.For(0, size.x*size.y, index => {
				int j = index /size.y;
				int i = index % size.x;
				float val = SampleAt(new Vector2(i/(float)size.x,j/(float)size.y));
				clrs[index] = new Color(val,val,val, 1);
			});
			preview.SetPixels(clrs);
			preview.Apply();
		}
		float SampleSource(Vector2 position){
			float currentTime = Environment.TickCount*(float)1e-3;
			Vector2 baseCoords = (position + scrollSpeed*currentTime)*noiseScale + offset;
			float bounds = 0;
			float rawVal = 0;
			int hash = offset.GetHashCode();
			
			// return RandFloat((int)Mathf.Floor(baseCoords.x));
			float frequency = 1;
			float amplitude = 1;
			for(int i = 0; i < octaves; i++){
				
				
				
				Vector2 noiseCoords = baseCoords;
				if (randomizeOctaveOffsets){
					noiseCoords += new Vector2(RandFloat(i)*1000, RandFloat(i*2)*1000);
				}
				noiseCoords *= frequency;
				float octaveVal = Mathf.PerlinNoise(noiseCoords.x, noiseCoords.y)*amplitude;


				rawVal += octaveVal;
				bounds += amplitude;
				
				frequency *= lacunarity;
				amplitude *= persistence;
			}
			
			rawVal = Linear.TransformRange(rawVal,0,bounds, 0, 1);
			return rawVal;
			
			//Seems to be good enough for octave offset randomization
			//Todo test uniformity
			float RandFloat(int seed)
			{
				uint s = (uint)seed*0x9e3779b9;
				s += 0xe120fc15;
				s ^= s << 13;
				s *= 571823589;
				s ^= s >> 12;
				s *= 821759831;
				s ^= s >> 5;
				s *= 265443236;
				const float invMax = 1.0f / uint.MaxValue;
				return s * invMax;
			}
		}

		public float SampleAt(Vector2 position){
			float rawVal = SampleSource(position);
			// float val = Linear.TransformRange(rawVal,0,1,minValue,maxValue);
			return rawVal;
		}
		public float GetSourceAt(Vector2 position){
			return SampleSource(position);
		}
	}
}