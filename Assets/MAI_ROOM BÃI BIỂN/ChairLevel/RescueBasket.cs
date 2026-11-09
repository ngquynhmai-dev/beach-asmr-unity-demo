using System.Collections.Generic;
using UnityEngine;

namespace Mai.Beach
{
    /// <summary>Back wall -> sand/contact shadows -> occupants -> front wall.
    /// Occupants are clipped to the mouth in world space, rather than pasted over the basket.</summary>
    public sealed class RescueBasket : MonoBehaviour
    {
        [SerializeField] private Texture2D _art;
        [SerializeField] private Shader _layerShader;
        [SerializeField] private Vector4 _mouth=new Vector4(.5f,.46f,.42f,.16f);
        [SerializeField] private bool _leftSide;
        private Camera _camera;
        private SpriteRenderer _back,_front;
        private Sprite _basketSprite,_shadowSprite;
        private Texture2D _shadowTexture;
        private Material _frontMaterial;
        private Rect _designRect;
        private float _scale;
        private readonly List<Occupant> _contents=new List<Occupant>();
        private sealed class Occupant
        {
            public int index;
            public SpriteRenderer renderer,shadow;
            public Material original,clipped;
            public Vector3 originalScale,fromPosition,fromScale;
            public Quaternion originalRotation;
            public float restingRotation;
            public float time;
            public float relativeWidth;
        }
        public int Count {get{return _contents.Count;}}
        public int FrontSortingOrder {get{return _front==null?0:_front.sortingOrder;}}
        public bool Ready {get{return _back!=null;}}

        public void Initialize(Camera camera)
        {
            _camera=camera;if(Ready)return;
            if(_art==null || _layerShader==null){Debug.LogError("Basket art or layer shader missing.",this);return;}
            _basketSprite=Sprite.Create(_art,new Rect(0,0,_art.width,_art.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            _back=MakeRenderer("Basket_Back_And_Sand",_basketSprite,90);
            _front=MakeRenderer("Basket_Foreground_Rim",_basketSprite,120);
            _frontMaterial=new Material(_layerShader);_frontMaterial.SetFloat("_FrontOnly",1);_frontMaterial.SetVector("_Mouth",_mouth);
            _front.sharedMaterial=_frontMaterial;
            _shadowTexture=new Texture2D(64,32,TextureFormat.RGBA32,false);
            for(int y=0;y<32;y++)for(int x=0;x<64;x++)
            {float d=new Vector2((x-31.5f)/31.5f,(y-15.5f)/15.5f).magnitude;_shadowTexture.SetPixel(x,y,new Color(.15f,.085f,.025f,Mathf.Pow(Mathf.Clamp01(1-d),1.3f)*.5f));}
            _shadowTexture.Apply();_shadowSprite=Sprite.Create(_shadowTexture,new Rect(0,0,64,32),new Vector2(.5f,.5f),100);
        }
        private SpriteRenderer MakeRenderer(string name,Sprite sprite,int order)
        {var go=new GameObject(name);go.transform.SetParent(transform,false);var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;return r;}

        public void Layout(float scale,float designHeight)
        {
            if(!Ready)return;_scale=scale;
            _designRect=designHeight<650?new Rect(_leftSide?20:665,designHeight-320,215,156):new Rect(_leftSide?25:565,designHeight-470,310,230);
            Vector3 center=DesignToWorld(_designRect.center);
            float worldWidth=_camera.orthographicSize*2*_camera.aspect*(_designRect.width*scale/Screen.width);
            float worldHeight=_camera.orthographicSize*2*(_designRect.height*scale/Screen.height);
            transform.position=new Vector3(center.x,center.y,-.2f);
            transform.localScale=new Vector3(worldWidth/_basketSprite.bounds.size.x,worldHeight/_basketSprite.bounds.size.y,1);
            foreach(var o in _contents)UpdateOccupant(o,false);
        }
        private Vector3 DesignToWorld(Vector2 point)
        {return _camera.ScreenToWorldPoint(new Vector3(point.x*_scale,Screen.height-point.y*_scale,10));}
        public bool ContainsScreenPoint(Vector2 screen)
        {
            if(!Ready)return false;
            Vector2 p=new Vector2(screen.x/_scale,(Screen.height-screen.y)/_scale);
            Vector2 uv=new Vector2((p.x-_designRect.x)/_designRect.width,1-(p.y-_designRect.y)/_designRect.height);
            return MouthContains(uv);
        }
        public bool MouthContains(Vector2 uv)
        {return new Vector2((uv.x-_mouth.x)/(_mouth.z*.94f),(uv.y-_mouth.y)/(_mouth.w*.86f)).sqrMagnitude<=1;}
        public Vector2 SlotUV(int index)
        {
            if(_leftSide)return index==0?new Vector2(.34f,.45f):new Vector2(.65f,.48f);
            // Four small shells around a larger crab, with a little sand between them.
            var slots=new[]{new Vector2(-.22f,.005f),new Vector2(-.08f,.06f),new Vector2(.115f,.045f),new Vector2(.24f,-.035f),new Vector2(.005f,-.085f)};
            return new Vector2(_mouth.x,_mouth.y)+slots[index];
        }
        public bool Dock(int index,SpriteRenderer animal,float relativeWidth=-1,float restingRotation=0)
        {
            if(!Ready)return false;foreach(var o in _contents)if(o.index==index)return false;
            var entry=new Occupant {index=index,renderer=animal,original=animal.sharedMaterial,originalScale=animal.transform.localScale,fromPosition=animal.transform.position,fromScale=animal.transform.localScale,relativeWidth=relativeWidth,originalRotation=animal.transform.rotation,restingRotation=restingRotation};
            entry.clipped=new Material(_layerShader);entry.clipped.SetFloat("_ClipInside",1);entry.clipped.SetVector("_Mouth",_mouth);
            entry.shadow=MakeRenderer("Basket_ContactShadow_"+index,_shadowSprite,95);
            // Drop animation stays above the lip until the animal has entered the opening.
            animal.sortingOrder=130;_contents.Add(entry);UpdateOccupant(entry,false);return true;
        }
        private void LateUpdate()
        {foreach(var o in _contents){o.time=Mathf.Min(.34f,o.time+Time.deltaTime);UpdateOccupant(o,true);}}
        private void UpdateOccupant(Occupant o,bool animate)
        {
            Vector2 uv=SlotUV(o.index);var bounds=_back.bounds;
            Vector3 target=new Vector3(bounds.min.x+uv.x*bounds.size.x,bounds.min.y+uv.y*bounds.size.y,-.24f);
            float width=bounds.size.x*(o.relativeWidth>0?o.relativeWidth:(o.index==4?.225f:.145f));
            Vector3 targetScale=Vector3.one*(width/o.renderer.sprite.bounds.size.x);
            float t=Mathf.Clamp01(o.time/.34f);float eased=1-Mathf.Pow(1-t,3);
            o.renderer.transform.position=Vector3.Lerp(o.fromPosition,target,eased)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*bounds.size.y*.13f);
            o.renderer.transform.localScale=Vector3.Lerp(o.fromScale,targetScale,eased);
            o.renderer.transform.rotation=Quaternion.Slerp(o.originalRotation,Quaternion.Euler(0,0,o.restingRotation),eased);
            if(t>=.55f){o.renderer.sortingOrder=o.index==4?105:100;o.renderer.sharedMaterial=o.clipped;}
            o.clipped.SetVector("_BasketRect",new Vector4(bounds.min.x,bounds.min.y,bounds.size.x,bounds.size.y));
            o.shadow.transform.position=target+Vector3.down*(width*.22f);
            // Compensate the basket parent's scale so the shadow stays flat on the sand.
            o.shadow.transform.localScale=new Vector3(width/_shadowSprite.bounds.size.x/transform.localScale.x,width*.27f/_shadowSprite.bounds.size.y/transform.localScale.y,1);
            o.shadow.sharedMaterial=o.clipped;o.shadow.color=new Color(1,1,1,eased);
        }
        public bool IsSettledInside(int index)
        {
            foreach(var o in _contents)if(o.index==index)return o.time>=.34f && o.renderer.gameObject.activeSelf && o.renderer.sortingOrder<FrontSortingOrder && MouthContains(SlotUV(index));
            return false;
        }
        public void SettleForValidation()
        {foreach(var o in _contents){o.time=.34f;UpdateOccupant(o,false);}}
        public void ResetContents()
        {
            foreach(var o in _contents)
            {
                if(o.renderer!=null){o.renderer.sharedMaterial=o.original;o.renderer.transform.localScale=o.originalScale;o.renderer.transform.rotation=o.originalRotation;o.renderer.sortingOrder=80;}
                if(o.shadow!=null)Destroy(o.shadow.gameObject);if(o.clipped!=null)Destroy(o.clipped);
            }
            _contents.Clear();
        }
        private void OnDestroy()
        {ResetContents();if(_frontMaterial!=null)Destroy(_frontMaterial);if(_basketSprite!=null)Destroy(_basketSprite);if(_shadowSprite!=null)Destroy(_shadowSprite);if(_shadowTexture!=null)Destroy(_shadowTexture);}
    }
}
