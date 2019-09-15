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

		RectTransform trans;

		void Awake() {
			trans = GetComponent<RectTransform>();

			var parsys = GetComponentsInChildren<ParticleSystem>();
			particles = new Particle[parsys.Length];

			for (int i = 0; i < particles.Length; i++) {
				particles[i] = new Particle();
				particles[i].System = parsys[i];

				ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[parsys[i].emission.burstCount];
				var count = parsys[i].emission.GetBursts(bursts);
				if (count == 0) continue;

				particles[i].MinCount = bursts[0].minCount;
				particles[i].MaxCount = bursts[0].maxCount;

				parsys[i].emission.SetBursts(new ParticleSystem.Burst[0]);
			}
//			StartCoroutine(Handler());
		}

//		System.Collections.IEnumerator Handler() {
//			while (true) {
//				Emit();
//				yield return new WaitForSeconds(1);
//			}
//		}

		[ContextMenu("Emit")]
		public void Emit() {
			foreach (var particle in particles)
				particle.System.Emit(particle.Count);
		}

		public void Emit(Vector2 position) {
			trans.anchoredPosition = position;
			foreach (var particle in particles)
				particle.System.Emit(particle.Count);
		}
	}
}

