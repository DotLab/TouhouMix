using UnityEngine;

namespace TouhouMix.Prefabs {
	public sealed class ParticleBurstEmitter : MonoBehaviour {
		[System.Serializable]
		public class Particle {
			public ParticleSystem System;

			public int MinCount, MaxCount;

			public int Count {
				get {
					return MinCount == 0 ? MaxCount : Random.Range(MinCount, MaxCount + 1);
				}
			}
		}

		public Particle[] particles;

		RectTransform rect;
		Transform trans;

		void Awake() {
			rect = GetComponent<RectTransform>();
			trans = GetComponent<Transform>();

			var parsys = GetComponentsInChildren<ParticleSystem>();
			particles = new Particle[parsys.Length];

			for (int i = 0; i < particles.Length; i++) {
				particles[i] = new Particle();
				particles[i].System = parsys[i];

				ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[parsys[i].emission.burstCount];
				var count = parsys[i].emission.GetBursts(bursts);
				if (count == 0) continue;

				if (bursts[0].count.mode == ParticleSystemCurveMode.TwoConstants) {
					particles[i].MinCount = bursts[0].minCount;
					particles[i].MaxCount = bursts[0].maxCount;
				} else {
					particles[i].MinCount = particles[i].MaxCount = bursts[0].maxCount;
				}

				//parsys[i].simulationSpace = ParticleSystemSimulationSpace.World;
				var main = parsys[i].main;
				main.simulationSpace = ParticleSystemSimulationSpace.World;
				parsys[i].emission.SetBursts(new ParticleSystem.Burst[0]);
			}
		}

		System.Collections.IEnumerator Handler() {
			while (true) {
				Emit();
				yield return new WaitForSeconds(1);
			}
		}

		System.Collections.IEnumerator DelayHandler() {
			yield return new WaitForSeconds(1); 
			Emit();
		}

		[ContextMenu("Emit")]
		public void Emit() {
			foreach (var particle in particles)
				particle.System.Emit(particle.Count);
		}

		[ContextMenu("EmitDelayed")]
		public void EmitDelayed() {
			StartCoroutine(DelayHandler());
		}

		[ContextMenu("EmitLoop")]
		public void EmitLoop() {
			StartCoroutine(Handler());
		}

		[ContextMenu("EmitLoopStop")]
		public void EmitLoopStop() {
			StopAllCoroutines();
		}

		public void Emit(Vector2 position) {
			//trans.anchoredPosition = position;
			trans.localPosition = position;
			foreach (var particle in particles)
				particle.System.Emit(particle.Count);
		}
	}
}

