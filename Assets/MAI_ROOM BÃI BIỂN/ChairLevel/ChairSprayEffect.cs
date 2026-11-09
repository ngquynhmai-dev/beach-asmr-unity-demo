using UnityEngine;

namespace Mai.Beach
{
    public sealed class ChairSprayEffect : MonoBehaviour
    {
        private Mesh _mesh;
        private Material _material;
        private MeshRenderer _renderer;
        private ParticleSystem _particles;
        private Material _particleMaterial;
        private Texture2D _waterTexture,_soapTexture;
        private AudioSource _sound;
        private AudioClip _waterSound,_soapSound;
        private float _emission;
        private readonly Vector3[] _vertices=new Vector3[4];
        public void Initialize(Shader shader,Texture2D water,Texture2D soap,AudioClip waterSound,AudioClip soapSound)
        {
            _waterTexture=water;_soapTexture=soap;_waterSound=waterSound;_soapSound=soapSound;
            _sound=gameObject.AddComponent<AudioSource>();_sound.loop=true;_sound.playOnAwake=false;_sound.volume=.3f;
            _mesh=new Mesh {name="Continuous water spray fan"};_mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh=_mesh;
            _renderer=gameObject.AddComponent<MeshRenderer>();_renderer.sortingOrder=115;
            _material=new Material(shader);_renderer.sharedMaterial=_material;_renderer.enabled=false;
            _mesh.vertices=_vertices;_mesh.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,0),new Vector2(1,1)};
            _mesh.triangles=new[]{0,1,2,2,1,3};
            var go=new GameObject("ASMR_Contact_Spray");go.transform.SetParent(transform,false);
            _particles=go.AddComponent<ParticleSystem>();_particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=_particles.main;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=180;main.startLifetime=.25f;main.startSpeed=0;
            var emission=_particles.emission;emission.enabled=false;var shape=_particles.shape;shape.enabled=false;
            var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sortingOrder=116;
            _particleMaterial=new Material(Shader.Find("Sprites/Default"));renderer.sharedMaterial=_particleMaterial;
        }
        public void Show(bool active,bool foam,Vector3 source,Vector3 contact,float spread)
        {
            if(_renderer==null)return;_renderer.enabled=active;
            if(!active){if(_sound.isPlaying)_sound.Stop();return;}
            var clip=foam?_soapSound:_waterSound;if(_sound.clip!=clip){_sound.Stop();_sound.clip=clip;}if(clip!=null && !_sound.isPlaying)_sound.Play();
            source.z=contact.z=-.3f;
            var dir=(contact-source).normalized;var side=new Vector3(-dir.y,dir.x,0);
            _vertices[0]=source-side*.015f;_vertices[1]=source+side*.015f;
            _vertices[2]=contact-side*spread;_vertices[3]=contact+side*spread;
            _mesh.vertices=_vertices;_mesh.RecalculateBounds();
            _material.SetColor("_Tint",foam?new Color(.97f,.99f,1,.18f):new Color(.62f,.87f,1,.13f));
            _particleMaterial.mainTexture=foam?_soapTexture:_waterTexture;
            _emission+=Time.unscaledDeltaTime*(foam?75:125);
            int count=Mathf.Min(10,(int)_emission);_emission-=count;
            for(int i=0;i<count;i++)
            {
                var p=new ParticleSystem.EmitParams();p.position=source;p.velocity=(contact-source)/.22f+side*Random.Range(-spread,spread)*3;
                p.startLifetime=Random.Range(.18f,.28f);p.startSize=foam?Random.Range(.08f,.15f):Random.Range(.025f,.06f);
                p.startColor=foam?new Color(1,1,1,.65f):new Color(.8f,.93f,1,.48f);p.rotation=Random.Range(0,360);_particles.Emit(p,1);
            }
        }
        private void OnDestroy(){if(_mesh!=null)Destroy(_mesh);if(_material!=null)Destroy(_material);if(_particleMaterial!=null)Destroy(_particleMaterial);}
    }
}
