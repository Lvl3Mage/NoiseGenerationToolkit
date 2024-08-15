using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lvl3Mage.EditorDevToolkit.Runtime;
using Lvl3Mage.EditorEnhancements.Runtime;
using UnityEditor;
using MyBox;
using UnityEngine;
using Lvl3Mage.MathToolkit;
using UnityEngine.Serialization;

namespace Lvl3Mage.NoiseGenerationToolkit
{
	[Serializable]
	public class NoiseBuilder
	{
		[Serializable]
		public class FilterStep
		{
			
			public enum FilterMode
			{
				None,
				Value,
				PerlinNoise,
				VoronoiNoise,
				RemapRange,
				SCurve,
				Exponential,
				Falloff,
				Pixelate,
				NumericalDerivative,
				Clamp,
				Ceiling,
				Floor,
			}
			public enum CombineMode
			{
				Assign,
				Add,
				Subtract,
				Multiply,
				Divide,
			}
			Func<float, float, float>[] combineFuncs = {
				(a, b) => b,
				(a, b) => a + b,
				(a, b) => a - b,
				(a, b) => a * b,
				(a, b) => a / b,
			}; 
			public enum Channel
			{
				x,
				y,
				z,
			}


			
			[Header("Channel settings")]
			[SerializeField] Channel inputChannel;
			[SerializeField] CombineMode channelCombineMode = CombineMode.Assign;
			[SerializeField] Channel outputChannel;

			[SerializeField] FilterMode filterMode;
			//Defines all possible parameters including adding noise
			
			public void Reset()
			{
				oldRange = new Vector2(0,1);
				newRange = new Vector2(0,1);
				slope = 1;
				exponent = 1;
				halfpoint = 1;
				pixelationScale = 16;
				epsilon = 0.01f;
				clamp = new Vector2(0,1);
				perlin = new PerlinNoiseSampler();
				voronoi = new VoronoiNoiseSampler();
				perlinCombineMode = CombineMode.Assign;
				valueCombineMode = CombineMode.Assign;
				valueCombineMode = CombineMode.Assign;
			}
			[Space(10)]
			[ActionButton(buttonText: "Reset Filter Settings", methodName: nameof(Reset), hideField: true)] [SerializeField]
			string resetProp;
			[Header("Filter settings")]
			[EnumSelectableField(nameof(filterMode), (int)FilterMode.Value)] 
			[SerializeField] float value;
			[EnumSelectableField(nameof(filterMode), (int)FilterMode.Value)] 
			[SerializeField] CombineMode valueCombineMode;
			
			
			
			[EnumSelectableField(nameof(filterMode), (int)FilterMode.PerlinNoise)] 
			[SerializeField] PerlinNoiseSampler perlin;
			[EnumSelectableField(nameof(filterMode), (int)FilterMode.PerlinNoise)] 
			[SerializeField] CombineMode perlinCombineMode;
			
			
			[EnumSelectableField(nameof(filterMode), (int)FilterMode.VoronoiNoise)] 
			[SerializeField] VoronoiNoiseSampler voronoi;
			[EnumSelectableField(nameof(filterMode), (int)FilterMode.VoronoiNoise)] 
			[SerializeField] CombineMode voronoiCombineMode;
			
			
			[EnumSelectableField(nameof(filterMode), (int)FilterMode.RemapRange)] 
			[SerializeField] Vector2 oldRange;
			[EnumSelectableField(nameof(filterMode), (int)FilterMode.RemapRange)] 
			[SerializeField] Vector2 newRange;
		
			
			
			[EnumSelectableField(nameof(filterMode), (int)FilterMode.SCurve)] 
			[SerializeField] float slope;
		
			
			
			[EnumSelectableField(nameof(filterMode), (int)FilterMode.Exponential)] 
			[SerializeField] float exponent;
		
			
		
			[EnumSelectableField(nameof(filterMode), (int)FilterMode.Falloff)] 
			[SerializeField] float halfpoint;
		
			
		
			[EnumSelectableField(nameof(filterMode), (int)FilterMode.Pixelate)] 
			[SerializeField] float pixelationScale;
		
			
			
			
			[EnumSelectableField(nameof(filterMode), (int)FilterMode.NumericalDerivative)] 
			[SerializeField] float epsilon;
		
		
			[EnumSelectableField(nameof(filterMode), (int)FilterMode.Clamp)] 
			[SerializeField] Vector2 clamp;
			
			public Vector3 SampleAt(Vector2 position, Func<Vector2, Vector3> sample)
			{
				Vector3 prevSample = sample(position);

				float SamplePrev(Vector2 vec)
				{
					//Cache the previous sample if the position is the same to avoid redundant calculations
					return vec == position ? prevSample[(int)inputChannel] : sample(vec)[(int)inputChannel];
				}


				FilterFunc filterFunc = filterLookup[filterMode];
				float val = filterFunc(position, this, SamplePrev);
				
				
				Vector3 output = prevSample;
				output[(int)outputChannel] = combineFuncs[(int)channelCombineMode](prevSample[(int)outputChannel], val);

				return output;
			}
			
			
			public delegate float FilterFunc(Vector2 vec, FilterStep filterData, Func<Vector2, float> sampler);

			Dictionary<FilterMode, FilterFunc> filterLookup = new(){
				{
					FilterMode.None,
					(vec, filter, samplePrev) => samplePrev(vec)
				},
				{
					FilterMode.Value,
					(vec, filter, samplePrev) => filter.combineFuncs[(int)filter.valueCombineMode](samplePrev(vec), filter.value)
				},
				{	
					FilterMode.PerlinNoise,
					(vec, filter, samplePrev) => {
						float val = samplePrev(vec);
						float perlinVal = filter.perlin.SampleAt(vec);
						return filter.combineFuncs[(int)filter.perlinCombineMode](val, perlinVal);
					}
				},
				{	
					FilterMode.VoronoiNoise,
					(vec, filter, samplePrev) => {
						float val = samplePrev(vec);
						float voronoiVal = filter.voronoi.SampleAt(vec);
						return filter.combineFuncs[(int)filter.voronoiCombineMode](val, voronoiVal);
					}
				},
				{
					FilterMode.RemapRange, 
					(vec, filter, samplePrev) => Linear.TransformRange(samplePrev(vec), filter.oldRange, filter.newRange)
				},
				{
					FilterMode.SCurve,
					(vec, filter, samplePrev) => Curves.ValueActivation(samplePrev(vec), filter.slope)
				},
				{
					FilterMode.Exponential,
					(vec, filter, samplePrev) => {
						float val = samplePrev(vec);
						return Mathf.Pow(val, filter.exponent);
					}
				},
				{
					FilterMode.Falloff,
					(vec, filter, samplePrev) => {
						float val = samplePrev(vec);
						return Curves.ValueDecay(val, filter.halfpoint);
					}
				},
				{
					FilterMode.Pixelate,
					(vec, filter, samplePrev) => {
						Vector2 pixelated = new Vector2(
							Mathf.Floor(vec.x * filter.pixelationScale) / filter.pixelationScale,
							Mathf.Floor(vec.y * filter.pixelationScale) / filter.pixelationScale
						);
						return samplePrev(pixelated);
					}
				},
				{
					FilterMode.NumericalDerivative,
					(vec, filter, samplePrev) => {
						Vector2 offsetX = new Vector2(filter.epsilon, 0) + vec;
						Vector2 offsetY = new Vector2(0, filter.epsilon) + vec;
						float dx = (samplePrev(offsetX) - samplePrev(vec))/filter.epsilon;
						float dy = (samplePrev(offsetY) - samplePrev(vec))/filter.epsilon;
						return new Vector2(dx, dy).magnitude;
					}
					
				},
				{
					FilterMode.Clamp,
					(vec, filter, samplePrev) => {
						float val = samplePrev(vec);
						return Mathf.Clamp(val, filter.clamp.x, filter.clamp.y);
					}
				},
				{
					FilterMode.Ceiling,
					(vec, filter, samplePrev) => Mathf.Ceil(samplePrev(vec))
				},
				{
					FilterMode.Floor,
					(vec, filter, samplePrev) => Mathf.Floor(samplePrev(vec))
				},
				
			};
		}
		[SerializeField] FilterStep[] filters = {};
		Vector3 SampleAt(Vector2 position, int filterIndex)
		{
			// if(filterIndex <= filters.Length - 1) return Vector3.zero;
			FilterStep filter = filters[filterIndex];
			Func<Vector2,Vector3> prevSample;
			if (filterIndex == 0){
				prevSample = vec => Vector3.zero;
			}
			else{
				prevSample = vec => SampleAt(vec, filterIndex - 1);
			}
			return filter.SampleAt(position, prevSample);
		}
		public Vector3 SampleVectorAt(Vector2 position)
		{
			if (filters.Length == 0){
				return Vector3.zero;
			}
			return SampleAt(position, filters.Length - 1);
		}
		[Header("Output settings")]
		[SerializeField] FilterStep.Channel outputChannel;
		
		public float SampleValueAt(Vector2 position)
		{
			Vector3 sample = SampleVectorAt(position);
			return sample[(int)outputChannel];
		}
		[Header("Preview settings")] 
		[SerializeField] bool drawPreview;
		[ConditionalField(nameof(drawPreview))]
		[SerializeField] float previewSampleSize = 1;
		[ConditionalField(nameof(drawPreview))]
		[SerializeField] [Range(1, 64)] int previewResolution = 25;
		
		[ConditionalField(nameof(drawPreview))] [SerializeField] bool drawOutputChannel;
		
		[ConditionalField(nameof(drawPreview))] [SerializeField] FilterMode filterMode;
		
		public Color SamplePreview(Vector2 uv)
		{
			Vector3 sample = SampleVectorAt(uv*previewSampleSize);
			if (drawOutputChannel){
				float val = sample[(int)outputChannel];
				sample = new Vector3(val,val,val);
			}
			return new Color(sample.x, sample.y, sample.z, 1);
		}
		[SerializeField]
		[ConditionalField(nameof(drawPreview)), Texture2DPreview(hideProperty: true,
			 showDropdown: true,
			 text: "Noise Preview:",
			 width:200,
			 updateMethodName: nameof(UpdatePreview))]
		Texture2D preview;

		void UpdatePreview()
	    {
	        if (!preview || preview.width != previewResolution || preview.height != previewResolution){
	            preview = new Texture2D(previewResolution,previewResolution);
	        }
	        preview.filterMode = filterMode;
	        Vector2Int size = new(preview.width, preview.height);
	        Color[] clrs = new Color[size.x*size.y];
	        Parallel.For(0, size.x*size.y, index => {
	            int j = index /size.y;
	            int i = index % size.x;
	            Color val = SamplePreview(new Vector2(i/(float)size.x,j/(float)size.y));
	            clrs[index] = val;
	        });
	        preview.SetPixels(clrs);
	        preview.Apply();
	    }
	}
}