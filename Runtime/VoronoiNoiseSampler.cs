using System;
using System.Threading.Tasks;
using Lvl3Mage.EditorEnhancements.Runtime;
using UnityEngine;
using Random = System.Random;

namespace Lvl3Mage.NoiseGenerationToolkit
{
	[Serializable]
	public class VoronoiNoiseSampler
	{
		public VoronoiNoiseSampler(){
			noiseScale = 1;
			scrollSpeed = Vector2.zero;
			offset = Vector2.zero;
		}
		//Source settings
		[Header("Source settings")]
		[SerializeField] float noiseScale;
		[SerializeField] Vector2 scrollSpeed;
		[SerializeField] Vector2 offset;
		[SerializeField] [Texture2DPreview(true, true, "Voronoi Preview:", 5, nameof(UpdatePreview))] Texture2D preview;
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
			Vector2 octaveOffset = Vector2.zero;
			Vector2 basePoint = new Vector2(Mathf.Floor(baseCoords.x), Mathf.Floor(baseCoords.y));
			Vector2[] points = {
				basePoint,
				basePoint + Vector2.up,
				basePoint + Vector2.right,
				basePoint + Vector2.one,
				basePoint - Vector2.up,
				basePoint - Vector2.right,
				basePoint - Vector2.one,
				basePoint + Vector2.up - Vector2.right,
				basePoint - Vector2.up + Vector2.right,
			};
			Vector2[] pointOffsets = new Vector2[points.Length];
			for (int i = 0; i < points.Length; i++){
				pointOffsets[i] = points[i] + RandomVector2(points[i].GetHashCode());
			}
			float smallestDist = float.MaxValue;
			foreach (Vector2 pointOffset in pointOffsets){
				float dist = (pointOffset - baseCoords).sqrMagnitude;
				if(smallestDist > dist){
					smallestDist = dist;
				}
			}
			return Mathf.Sqrt(smallestDist)/1.41421f;
			
			Vector2 RandomVector2(int seed)
			{
				Random rand = new Random(seed);
				Vector2 result = new Vector2((float)rand.NextDouble(), (float)rand.NextDouble());
				return result;
			}

		}

		public float SampleAt(Vector2 position){
			float rawVal = SampleSource(position);
			// float val = Linear.TransformRange(rawVal,0,1,minValue,maxValue);
			return rawVal;
		}
	}
}