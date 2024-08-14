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

		[Tooltip("How much the frequency of each octave increases with octave amount")]
		[SerializeField] [Min(1)] float lacunarity = 1;

		[Tooltip("How much the amplitude of each octave decreases with octave amount")]
		[SerializeField] [Range(0,1)] float persistence;
		
		[SerializeField]
		[Texture2DPreview(hideProperty: true,
			showDropdown: true,
			text: "Perlin Noise Preview:",
			width: 5,
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
			Vector2 baseCoords = (position + scrollSpeed*currentTime)*noiseScale;
			float bounds = 0;
			float rawVal = 0;
			Vector2 octaveOffset = Vector2.zero;
			int hash = offset.GetHashCode();
			
			
			float frequency = 1;
			float amplitude = 1;
			for(int i = 0; i < octaves; i++){
				
				
				
				Vector2 noiseCoords = baseCoords*frequency +offset;
				float octaveVal = Mathf.PerlinNoise(noiseCoords.x + octaveOffset.x, noiseCoords.y + octaveOffset.y)*amplitude;
				
				//Randomize octave offset to avoid repeating patterns
				octaveOffset = new Vector2(RandFloat(hash++),RandFloat(hash++));
				
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
				float x = seed;
				x *= 17179869183.0f;
				x -= Mathf.Floor(x);
				return x;
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