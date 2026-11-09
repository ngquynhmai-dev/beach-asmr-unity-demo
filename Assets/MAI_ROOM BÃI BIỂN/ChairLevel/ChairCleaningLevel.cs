using System;
using UnityEngine;

namespace Mai.Beach
{
    /// <summary>Playable thirteen-step chair restoration. Mouse and touch share the same input path.</summary>
    public sealed class ChairCleaningLevel : MonoBehaviour
    {
        [Serializable] public sealed class Step
        {
            public string title, tool, instruction, region, effect;
            public float clean;
            public Step(string title, string tool, string instruction, string region, string effect, float clean)
            { this.title=title; this.tool=tool; this.instruction=instruction; this.region=region; this.effect=effect; this.clean=clean; }
        }
        [SerializeField] private Camera _camera;
        [SerializeField] private SpriteRenderer _chair;
        [SerializeField] private SpriteRenderer _background;
        [SerializeField] private SpriteRenderer _leftPalm, _rightPalm;
        [SerializeField] private Texture2D _cleanTexture;
        [SerializeField] private Texture2D _animalsAtlas;
        [SerializeField] private Texture2D _crabTongsArt;
        [SerializeField] private Texture2D _toolAtlas, _foamTexture, _bareDirtyTexture, _kelpAtlas;
        [SerializeField] private Texture2D _wetState, _soapState;
        [SerializeField] private Texture2D _sandFreeState;
        [SerializeField] private Texture2D _waterSprayTexture, _soapSprayTexture;
        [SerializeField] private AudioClip _waterSpraySound, _soapSpraySound, _scrubSound;
        private AudioSource _scrubLoop;
        private float _asmrScrubStrength;
        [SerializeField] private Shader _sprayShader;
        [SerializeField] private Shader _shader;
        [SerializeField] private RescueBasket _rescueBasket;
        [SerializeField] private RescueBasket _seaweedBasket;
        [SerializeField] private Vector4 _tear = new Vector4(.49f,.49f,.16f,.13f);

        public static readonly Step[] Steps = {
            new Step("Nhặt ốc và gắp cua", "Tay / Kẹp mềm", "Nhặt 4 con ốc, dùng kẹp mềm gắp cua và thả vào lòng giỏ mây có cát ẩm.", "animals", "collect", 0),
            new Step("Gỡ rong biển", "Kẹp gỡ rong", "Kẹp từng búi rong, nhấc khỏi ghế rồi thả vào giỏ rong bên trái.", "seaweed", "pickup", 0),
            new Step("Quét cát trên đệm", "Chổi mềm", "Quét đều mặt đệm từ trên xuống dưới để gạt lớp cát.", "cushion", "clean", .25f),
            new Step("Hút sạch cát trên ghế", "Máy hút cát", "Hút toàn bộ tựa lưng, mặt ngồi và các khe cho đến khi hết cát.", "all", "sand", 0),
            new Step("Xịt ướt chiếc ghế", "Vòi nước nhẹ", "Phun nước đều lên đệm, tay vịn và khung gỗ.", "all", "wet", .35f),
            new Step("Phủ bọt lên đệm", "Bình tạo bọt", "Phủ kín mặt đệm bằng lớp bọt làm sạch.", "cushion", "foam", .35f),
            new Step("Chà sạch mặt đệm", "Bàn chải mềm", "Chà nhiều lượt để bọt dày lên và phủ kín toàn bộ đệm xanh, cả mép ghế.", "cushion", "scrub", .78f),
            new Step("Xử lý vết ố", "Dung dịch / Bọt biển", "Thấm dung dịch lên 3 vết ố, chờ ngấm rồi lau từng vết.", "stains", "stain", .96f),
            new Step("Làm sạch đường may", "Bàn chải nhỏ", "Đi theo đường viền đệm để làm sạch các mép và khe may.", "seams", "clean", 1),
            new Step("Chà khung gỗ", "Bàn chải gỗ", "Chà tay vịn, thanh đỡ và từng chân ghế để lộ vân gỗ.", "frame", "clean", 1),
            new Step("Rửa trôi bọt bẩn", "Vòi xả", "Xả từ trên xuống dưới cho đến khi hết bọt và cặn bẩn.", "all", "rinse", 1),
            new Step("Hút nước và lau khô", "Máy hút / Khăn khô", "Hút nước trên đệm, sau đó dùng khăn lau khô khung ghế.", "all", "dry", 1),
            new Step("Dán vá chỗ rách", "Keo / Miếng da / Miết", "Bóp keo theo viền rách, kéo miếng da vào chỗ rách rồi miết từ giữa ra mép.", "tear", "repair", 1)
        };

        private const int N=128;
        private readonly Color _navy=new Color(.035f,.16f,.2f), _teal=new Color(.05f,.56f,.53f), _gold=new Color(1,.79f,.36f);
        private Color[] _pixels;
        private bool[] _surface, _dirtySurface, _cushion, _region, _covered;
        private Texture2D _mask, _dot, _brushMask;
        private Texture2D _foamFlow, _cushionEdge;
        private Color[] _foamFlowPixels;
        private Vector2 _lastFoamStamp = new Vector2(-10,-10);
        private Color[] _brushPixels;
        private Material _material, _original;
        private Sprite[] _animalSprites;
        private SpriteRenderer[] _animals;
        private SpriteRenderer[] _kelp;
        private Sprite[] _kelpSprites;
        private readonly Vector2[] _kelpHomes={new Vector2(.16f,.81f),new Vector2(.82f,.30f)};
        private readonly bool[] _kelpRemoved=new bool[2];
        private int _heldKelp=-1;
        private Rect[] _toolUVs;
        private float[] _toolAspects;
        private ChairSprayEffect _spray;
        private Vector2[] _animalHomes;
        private bool[] _rescued;
        private int _step, _tool=-1, _heldAnimal=-1, _repairPhase, _dryPhase;
        private bool _dragPatch, _done, _pending, _pointerDown, _hasLast;
        private float _progress, _pendingTime, _repairAmount;
        private int _required, _hits, _width, _height;
        private Vector2 _lastUV, _pointer;
        private Vector2 _brushStrokeCenter;
        private float _tongsGrip, _tongsReleaseTime;
        private Vector2 _tongsReleaseScreen;
        private GUIStyle _heading, _body, _small, _button, _badge;
        private Rect _tray, _top;
        private float _uiScale, _uiHeight;
        private string _feedback;
        private float _feedbackTime;
        private AudioSource _audio;
        private AudioClip _tick, _finish;
        private bool _previousRunInBackground;
        private readonly Vector2[] _stains={new Vector2(.32f,.87f),new Vector2(.44f,.73f),new Vector2(.53f,.44f)};
        private readonly int[] _stainPhase=new int[3];
        private readonly float[] _stainTimers=new float[3];

        public int CurrentStep { get { return _step; } }
        public int StepTotal { get { return Steps.Length; } }
        public float Progress { get { return _progress; } }
        public bool IsComplete { get { return _done; } }
        public int RepairPhase { get { return _repairPhase; } }
        public int RescuedCount { get { int n=0; if(_rescued!=null)foreach(bool b in _rescued)if(b)n++;return n; } }
        public bool IsReady { get { return _material!=null && _region!=null; } }
        public bool CrabTongsVisible {get{return _crabTongsArt!=null && ((_step==0 && _tool==1) || (_step==1 && _tool==0) || _tongsReleaseTime>0);}}
        public float CrabTongsGrip {get{return _tongsGrip;}}
        public int RemovedKelpCount {get{return (_kelpRemoved[0]?1:0)+(_kelpRemoved[1]?1:0);}}

        private void Start()
        {
            _previousRunInBackground=Application.runInBackground;Application.runInBackground=true;
            if(_chair==null || _camera==null || _cleanTexture==null || _shader==null || _animalsAtlas==null || _rescueBasket==null || _seaweedBasket==null || _kelpAtlas==null || _bareDirtyTexture==null || _foamTexture==null || _toolAtlas==null || _sprayShader==null || _crabTongsArt==null)
            { Debug.LogError("Chair level is missing a required asset.",this);enabled=false;return; }
            _original=_chair.sharedMaterial;
            _material=new Material(_shader);
            _material.SetTexture("_DirtyTex",_bareDirtyTexture);
            _material.SetTexture("_SandFreeState",_sandFreeState);
            _material.SetTexture("_CleanTex",_cleanTexture);
            _material.SetTexture("_FoamTex",_foamTexture);
            _material.SetTexture("_WetState",_wetState);_material.SetTexture("_SoapState",_soapState);
            _material.SetVector("_Damage",_tear);
            _chair.sharedMaterial=_material;
            _mask=new Texture2D(N,N,TextureFormat.RGBA32,false,true) { name="Chair local cleaning mask",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp };
            _pixels=new Color[N*N];_surface=new bool[N*N];_dirtySurface=new bool[N*N];_cushion=new bool[N*N];_region=new bool[N*N];_covered=new bool[N*N];
            for(int y=0;y<N;y++)for(int x=0;x<N;x++)
            {
                int i=y*N+x;Color c=_cleanTexture.GetPixelBilinear((x+.5f)/N,(y+.5f)/N);
                Color d=_chair.sprite.texture.GetPixelBilinear((x+.5f)/N,(y+.5f)/N);
                _surface[i]=c.a>.3f;_dirtySurface[i]=Mathf.Max(c.a,d.a)>.65f;
                // Include pale highlights, stitching and the dark front/side roll of the cushion.
                _cushion[i]=c.a>.3f && (InsideCushionOutline(new Vector2((x+.5f)/N,(y+.5f)/N)) ||
                    (c.g>c.r*1.04f && c.b>c.r*1.02f));
            }
            // Static high-resolution silhouette decouples the visible foam edge from the 128px work grid.
            const int edgeSize=1024;
            _cushionEdge=new Texture2D(edgeSize,edgeSize,TextureFormat.RGBA32,false,true){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var edgePixels=new Color[edgeSize*edgeSize];
            for(int y=0;y<edgeSize;y++)for(int x=0;x<edgeSize;x++)
            {
                Color c=_cleanTexture.GetPixelBilinear((x+.5f)/edgeSize,(y+.5f)/edgeSize);
                float teal=Mathf.Min(c.g-c.r,c.b-c.r);
                float edge=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.005f,.045f,teal))*c.a;
                edgePixels[y*edgeSize+x]=new Color(edge,edge,edge,edge);
            }
            _cushionEdge.SetPixels(edgePixels);_cushionEdge.Apply(false,true);
            _material.SetTexture("_CushionEdge",_cushionEdge);
            _material.SetTexture("_WorkMask",_mask);
            _brushMask=new Texture2D(N,N,TextureFormat.RGBA32,false,true){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            _brushPixels=new Color[N*N];_material.SetTexture("_BrushMask",_brushMask);
            _foamFlow=new Texture2D(N,N,TextureFormat.RGBA32,false,true){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            _foamFlowPixels=new Color[N*N];_material.SetTexture("_FoamFlow",_foamFlow);
            _dot=CreateDot();
            _audio=gameObject.AddComponent<AudioSource>();_audio.playOnAwake=false;
            _tick=CreateTone(700,.07f);_finish=CreateTone(1050,.22f);
            CreateAnimals();CreateKelpAndTools();_rescueBasket.Initialize(_camera);_seaweedBasket.Initialize(_camera);
            var sprayObject=new GameObject("Water_And_Foam_Spray");sprayObject.transform.SetParent(transform);
            _spray=sprayObject.AddComponent<ChairSprayEffect>();_spray.Initialize(_sprayShader,_waterSprayTexture,_soapSprayTexture,_waterSpraySound,_soapSpraySound);
            _scrubLoop=gameObject.AddComponent<AudioSource>();_scrubLoop.clip=_scrubSound;_scrubLoop.loop=true;_scrubLoop.playOnAwake=false;_scrubLoop.volume=.3f;
            RestartLevel();FitCamera();
        }

        private static Rect TrimCell(Color32[] pixels,int width,int height,Rect cell)
        {
            int x0=(int)cell.x,y0=(int)cell.y,x1=(int)cell.xMax,y1=(int)cell.yMax;
            int minX=x1-1,maxX=x0,minY=y1-1,maxY=y0;
            for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(pixels[y*width+x].a>20)
            {minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
            return new Rect(minX,minY,Mathf.Max(1,maxX-minX+1),Mathf.Max(1,maxY-minY+1));
        }
        private static Rect[] FindToolObjects(Color32[] pixels,int width,int height)
        {
            // Follow each connected silhouette so an angled handle crossing a grid boundary is not cut off.
            var found=new Rect[16];var largest=new int[16];var visited=new bool[pixels.Length];var queue=new int[pixels.Length];
            for(int seed=0;seed<pixels.Length;seed++)
            {
                if(visited[seed] || pixels[seed].a<28)continue;
                int read=0,write=1;queue[0]=seed;visited[seed]=true;
                int minX=seed%width,maxX=minX,minY=seed/width,maxY=minY;
                while(read<write)
                {
                    int index=queue[read++],x=index%width,y=index/width;
                    minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);
                    for(int d=0;d<4;d++)
                    {
                        int nx=x+(d==0?-1:d==1?1:0),ny=y+(d==2?-1:d==3?1:0);
                        if(nx<0 || nx>=width || ny<0 || ny>=height)continue;int n=ny*width+nx;
                        if(visited[n] || pixels[n].a<28)continue;visited[n]=true;queue[write++]=n;
                    }
                }
                if(write<512)continue;
                int col=Mathf.Clamp((minX+maxX)*2/width,0,3),row=Mathf.Clamp(3-(minY+maxY)*2/height,0,3),slot=row*4+col;
                if(write<=largest[slot])continue;largest[slot]=write;
                int left=Mathf.Max(0,minX-2),bottom=Mathf.Max(0,minY-2);
                found[slot]=new Rect(left,bottom,Mathf.Min(width,maxX+3)-left,Mathf.Min(height,maxY+3)-bottom);
            }
            return found;
        }
        private void CreateKelpAndTools()
        {
            _kelp=new SpriteRenderer[2];_kelpSprites=new Sprite[2];var pixels=_kelpAtlas.GetPixels32();
            for(int i=0;i<2;i++)
            {
                Rect rect=TrimCell(pixels,_kelpAtlas.width,_kelpAtlas.height,new Rect(i*_kelpAtlas.width/2,0,_kelpAtlas.width/2,_kelpAtlas.height));
                _kelpSprites[i]=Sprite.Create(_kelpAtlas,rect,new Vector2(.5f,.5f),100);
                var go=new GameObject("Pickable_Kelp_"+(i+1));go.transform.SetParent(transform);var r=go.AddComponent<SpriteRenderer>();
                r.sprite=_kelpSprites[i];r.sortingOrder=70;r.transform.position=UVToWorld(_kelpHomes[i]);
                r.transform.localScale=Vector3.one*(_chair.bounds.size.y*(i==0?.44f:.40f)/r.sprite.bounds.size.y);_kelp[i]=r;
            }
            _toolUVs=new Rect[16];_toolAspects=new float[16];pixels=_toolAtlas.GetPixels32();
            var objects=FindToolObjects(pixels,_toolAtlas.width,_toolAtlas.height);
            int w=_toolAtlas.width/4,h=_toolAtlas.height/4;
            for(int i=0;i<16;i++)
            {
                Rect rect=objects[i].width>0?objects[i]:TrimCell(pixels,_toolAtlas.width,_toolAtlas.height,new Rect((i%4)*w,(3-i/4)*h,w,h));
                _toolAspects[i]=rect.width/rect.height;
                _toolUVs[i]=new Rect(rect.x/_toolAtlas.width,rect.y/_toolAtlas.height,rect.width/_toolAtlas.width,rect.height/_toolAtlas.height);
            }
        }

        private static Texture2D CreateDot()
        {
            var t=new Texture2D(32,32,TextureFormat.RGBA32,false);
            for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {float a=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(12,15,Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))));t.SetPixel(x,y,new Color(1,1,1,a));}
            t.Apply();return t;
        }
        private static AudioClip CreateTone(float hz,float duration)
        {
            int count=(int)(22050*duration);var data=new float[count];
            for(int i=0;i<count;i++)data[i]=Mathf.Sin(i*hz*2*Mathf.PI/22050f)*.12f*(1-(float)i/count);
            var clip=AudioClip.Create("Chair feedback",count,1,22050,false);clip.SetData(data,0);return clip;
        }
        private void CreateAnimals()
        {
            _animalSprites=new Sprite[2];int half=_animalsAtlas.width/2;
            var pixels=_animalsAtlas.GetPixels32();
            for(int i=0;i<2;i++)
            {
                int minX=(i+1)*half-1,maxX=i*half,minY=_animalsAtlas.height-1,maxY=0;
                for(int y=0;y<_animalsAtlas.height;y++)for(int x=i*half;x<(i+1)*half;x++)
                {if(pixels[y*_animalsAtlas.width+x].a<20)continue;minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
                _animalSprites[i]=Sprite.Create(_animalsAtlas,new Rect(minX,minY,maxX-minX+1,maxY-minY+1),new Vector2(.5f,.5f),100);
            }
            _animalHomes=new[]{new Vector2(.27f,.82f),new Vector2(.40f,.68f),new Vector2(.40f,.40f),new Vector2(.76f,.43f),new Vector2(.56f,.55f)};
            _animals=new SpriteRenderer[5];_rescued=new bool[5];
            for(int i=0;i<5;i++)
            {
                var go=new GameObject(i==4?"Rescue_Crab":"Rescue_SeaSnail_"+(i+1));go.transform.SetParent(transform);
                var r=go.AddComponent<SpriteRenderer>();r.sprite=_animalSprites[i==4?1:0];r.sortingOrder=80;
                float width=i==4?.95f:.65f;r.transform.localScale=Vector3.one*(width/r.sprite.bounds.size.x);
                r.transform.position=UVToWorld(_animalHomes[i]);_animals[i]=r;
            }
        }

        public void RestartLevel()
        {
            if(_pixels==null)return;
            if(_rescueBasket!=null)_rescueBasket.ResetContents();
            if(_seaweedBasket!=null)_seaweedBasket.ResetContents();
            _step=0;_tool=-1;_repairPhase=0;_dryPhase=0;_repairAmount=0;_done=false;_pending=false;
            _heldAnimal=-1;_heldKelp=-1;_dragPatch=false;_pointerDown=false;_hasLast=false;
            _tongsGrip=0;_tongsReleaseTime=0;
            Array.Clear(_pixels,0,_pixels.Length);Array.Clear(_stainPhase,0,3);Array.Clear(_stainTimers,0,3);
            Array.Clear(_brushPixels,0,_brushPixels.Length);
            for(int i=0;i<_foamFlowPixels.Length;i++)_foamFlowPixels[i]=new Color(.5f,.5f,0,0);
            _lastFoamStamp=new Vector2(-10,-10);
            for(int i=0;i<_pixels.Length;i++)if(!_surface[i])_pixels[i].r=1;
            for(int i=0;i<2;i++){_kelpRemoved[i]=false;_kelp[i].gameObject.SetActive(true);_kelp[i].transform.position=UVToWorld(_kelpHomes[i]);_kelp[i].sortingOrder=70;}
            for(int i=0;i<5;i++){_rescued[i]=false;_animals[i].gameObject.SetActive(true);_animals[i].transform.position=UVToWorld(_animalHomes[i]);}
            _material.SetFloat("_Repair",0);_material.SetFloat("_Before",0);Upload();BeginStep();Say("Giải cứu những vị khách nhỏ trên ghế trước nhé!");
        }
        private void BeginStep()
        {
            if(_seaweedBasket!=null)_seaweedBasket.gameObject.SetActive(_step>=1);
            _progress=0;_hits=0;_required=0;_tool=-1;_hasLast=false;_pointerDown=false;
            Array.Clear(_covered,0,_covered.Length);
            for(int y=0;y<N;y++)for(int x=0;x<N;x++)
            {
                int i=y*N+x;Vector2 uv=new Vector2((x+.5f)/N,(y+.5f)/N);
                _region[i]=AcceptRegion(uv,i);if(_region[i])_required++;
            }
        }
        private bool AcceptRegion(Vector2 uv,int i)
        {
            string region=Steps[Mathf.Min(_step,12)].region;
            if(region=="tear")return Mathf.Abs(TearDistance(uv)-.95f)<.22f;
            if(region=="seaweed")return _dirtySurface[i] && (Ellipse(uv,new Vector2(.17f,.80f),new Vector2(.13f,.25f)) || Ellipse(uv,new Vector2(.82f,.32f),new Vector2(.14f,.25f)));
            if(!_surface[i])return false;
            switch(region)
            {
                case "animals":return false;
                case "cushion":return _cushion[i];
                case "frame":return !_cushion[i];
                case "seams":
                    if(!_cushion[i])return false;
                    int x=i%N,y=i/N;
                    for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)
                    {int xx=x+dx, yy=y+dy;if(xx>=0 && xx<N && yy>=0 && yy<N && !_cushion[yy*N+xx])return true;}
                    return (Mathf.Abs(uv.y-(.70f-.20f*uv.x))<.045f && uv.x>.17f && uv.x<.7f)
                        || Ellipse(uv,new Vector2(.49f,.61f),new Vector2(.20f,.075f));
                case "stains":foreach(var p in _stains)if(Vector2.Distance(uv,p)<.09f)return true;return false;
                default:return true;
            }
        }
        private static readonly Vector2[] CushionOutline = {
            new Vector2(.060f,.952f),new Vector2(.085f,.973f),new Vector2(.417f,.997f),
            new Vector2(.444f,.972f),new Vector2(.548f,.741f),new Vector2(.600f,.649f),
            new Vector2(.694f,.593f),new Vector2(.965f,.466f),new Vector2(.978f,.443f),
            new Vector2(.972f,.398f),new Vector2(.551f,.303f),new Vector2(.250f,.450f),
            new Vector2(.181f,.661f)
        };
        private static bool InsideCushionOutline(Vector2 uv)
        {
            bool inside=false;
            for(int i=0,j=CushionOutline.Length-1;i<CushionOutline.Length;j=i++)
            {
                Vector2 a=CushionOutline[i],b=CushionOutline[j];
                if((a.y>uv.y)!=(b.y>uv.y) && uv.x<(b.x-a.x)*(uv.y-a.y)/(b.y-a.y)+a.x)inside=!inside;
            }
            return inside;
        }
        private static bool Ellipse(Vector2 uv,Vector2 center,Vector2 radius)
        {return new Vector2((uv.x-center.x)/radius.x,(uv.y-center.y)/radius.y).sqrMagnitude<1;}
        private float TearDistance(Vector2 uv)
        {return new Vector2((uv.x-_tear.x)/_tear.z,(uv.y-_tear.y)/_tear.w).magnitude;}
        private void Update()
        {
            if(!IsReady)return;
            AdvanceSimulation(Time.deltaTime);
            if(_width!=Screen.width || _height!=Screen.height)FitCamera();
            if(_done || _pending)return;
            bool down,held,up;Vector2 screen;
            if(Input.touchCount>0)
            {var t=Input.GetTouch(0);screen=t.position;down=t.phase==TouchPhase.Began;held=t.phase==TouchPhase.Moved || t.phase==TouchPhase.Stationary;up=t.phase==TouchPhase.Ended || t.phase==TouchPhase.Canceled;}
            else {screen=Input.mousePosition;down=Input.GetMouseButtonDown(0);held=Input.GetMouseButton(0);up=Input.GetMouseButtonUp(0);}
            _pointer=screen;
            Vector2 gui=new Vector2(screen.x/_uiScale,(Screen.height-screen.y)/_uiScale);
            Vector2 uv=ScreenToUV(screen);
            if(down && !_top.Contains(gui) && !_tray.Contains(gui))PointerBeginUV(uv);
            if(held && _pointerDown)PointerMoveUV(uv);
            if(up && (_pointerDown || _dragPatch))
            {
                if(_heldAnimal>=0)ReleaseAnimal(_rescueBasket.ContainsScreenPoint(screen));
                else if(_heldKelp>=0)ReleaseKelp(_seaweedBasket.ContainsScreenPoint(screen));
                else if(_dragPatch)PlacePatchUV(uv);
                PointerEnd();
            }
            if(_heldAnimal>=0 && held)_animals[_heldAnimal].transform.position=UVToWorld(uv);
            if(_heldKelp>=0 && held)_kelp[_heldKelp].transform.position=UVToWorld(uv);
        }
        private void LateUpdate()
        {
            if(_spray==null || !IsReady)return;
            _material.SetFloat("_ScrubActive",_asmrScrubStrength);_material.SetFloat("_ScrubTime",Time.unscaledTime);
            bool scrubbing=(_step==6 || (_step==7 && _tool==1)) && _pointerDown && !_pending;
            if(scrubbing && _scrubSound!=null && !_scrubLoop.isPlaying)_scrubLoop.Play();else if(!scrubbing && _scrubLoop.isPlaying)_scrubLoop.Stop();
            bool active=_pointerDown && !_pending && _tool>=0 && (_step==4 || _step==5 || _step==10);
            Vector2 uv=ScreenToUV(_pointer);int x=Mathf.Clamp((int)(uv.x*N),0,N-1),y=Mathf.Clamp((int)(uv.y*N),0,N-1);
            active=active && uv.x>=0 && uv.x<=1 && uv.y>=0 && uv.y<=1 && _surface[y*N+x];
            Vector3 source=_camera.ScreenToWorldPoint(new Vector3(_pointer.x+58*_uiScale,_pointer.y+72*_uiScale,10));
            _spray.Show(active,_step==5,source,UVToWorld(uv),_chair.bounds.size.x*.075f);
        }
        public void AdvanceSimulation(float delta)
        {
            if(!IsReady)return;
            _asmrScrubStrength=Mathf.MoveTowards(_asmrScrubStrength,(_step==6 || (_step==7 && _tool==1)) && _pointerDown && !_pending?1:0,delta*5);
            _material.SetFloat("_ScrubActive",_asmrScrubStrength);
            _tongsGrip=Mathf.MoveTowards(_tongsGrip,_heldAnimal==4 || _heldKelp>=0?1:0,delta*7);
            _tongsReleaseTime=Mathf.Max(0,_tongsReleaseTime-delta);
            _feedbackTime=Mathf.Max(0,_feedbackTime-delta);
            for(int i=0;i<3;i++)if(_stainPhase[i]==1){_stainTimers[i]+=delta;if(_stainTimers[i]>=1.1f){_stainPhase[i]=2;Say("Dung dịch đã ngấm. Chọn bọt biển để lau!");}}
            if(_pending)
            {
                _pendingTime-=delta;if(_pendingTime<=0)
                {
                    _pending=false;_step++;
                    if(_step>=13){_done=true;_step=13;_progress=1;_material.SetFloat("_Repair",1);Say("Ghế đã được phục hồi!");}
                    else BeginStep();
                }
            }
        }
        public void SelectTool(int index)
        {
            if(_done || _pending)return;
            if(index<0 || index>=ToolNames().Length)return;
            if(_step==12 && index!=_repairPhase){Say("Hoàn thành thao tác hiện tại trước nhé.");return;}
            if(_step==11 && index!=_dryPhase){Say("Hút nước trên đệm trước, rồi lau khung gỗ.");return;}
            _tool=index;
        }
        public bool PointerBeginUV(Vector2 uv)
        {
            if(!IsReady || _done || _pending || _tool<0)return false;
            _pointerDown=true;_hasLast=false;
            _brushStrokeCenter=uv;
            if(_step==0)
            {
                for(int i=4;i>=0;i--)if(!_rescued[i] && Vector2.Distance(uv,_animalHomes[i])<(i==4?.10f:.075f))
                {
                    if((i==4 && _tool!=1)||(i<4 && _tool!=0)){Say(i==4?"Dùng kẹp mềm để gắp cua.":"Dùng tay nhặt ốc vào khay.");return false;}
                    _heldAnimal=i;_animals[i].sortingOrder=130;return true;
                }
                return false;
            }
            if(_step==1)
            {
                for(int i=0;i<2;i++)if(!_kelpRemoved[i] && Ellipse(uv,_kelpHomes[i],new Vector2(.13f,.23f)))
                {_heldKelp=i;_kelp[i].sortingOrder=130;return true;}
                return false;
            }
            if(_step==12 && _repairPhase==1)return false;
            PointerMoveUV(uv);return true;
        }
        public void PointerMoveUV(Vector2 uv)
        {
            if(!_pointerDown || _pending || _done || _step==0 || _step==1 || _dragPatch)return;
            if(uv.x<0 || uv.x>1 || uv.y<0 || uv.y>1){_hasLast=false;return;}
            if(_step==7){TreatStain(uv);return;}
            if(_step==12 && _repairPhase==1)return;
            if(_step==11 && _tool!=_dryPhase || _step==12 && _tool!=_repairPhase)return;
            if(_hasLast)
            {
                int n=Mathf.Clamp(Mathf.CeilToInt(Vector2.Distance(_lastUV,uv)/.025f),1,40);
                for(int j=1;j<=n;j++)Paint(Vector2.Lerp(_lastUV,uv,(float)j/n));
            }
            else Paint(uv);
            _lastUV=uv;_hasLast=true;Upload();CheckCoverage();
            if(_step==6)_material.SetVector("_ScrubPoint",new Vector4(uv.x,uv.y,0,0));
        }
        public void PointerEnd(){_pointerDown=false;_hasLast=false;}
        public bool ReleaseAnimal(bool insideTray)
        {
            if(_heldAnimal<0)return false;int i=_heldAnimal;_heldAnimal=-1;
            if(i==4)
            {
                _tongsReleaseScreen=_camera.WorldToScreenPoint(_animals[i].transform.position);
                _tongsReleaseTime=.32f;
            }
            if(!insideTray || !_rescueBasket.Dock(i,_animals[i])){_animals[i].transform.position=UVToWorld(_animalHomes[i]);_animals[i].sortingOrder=80;Say("Thả vào phần cát bên trong miệng giỏ nhé.");return false;}
            _rescued[i]=true;_progress=RescuedCount/5f;_audio.PlayOneShot(_tick);
            if(RescuedCount==5)FinishStep();return true;
        }
        public bool ReleaseKelp(bool insideTray)
        {
            if(_heldKelp<0)return false;int i=_heldKelp;_heldKelp=-1;
            _tongsReleaseScreen=_camera.WorldToScreenPoint(_kelp[i].transform.position);_tongsReleaseTime=.32f;
            if(!insideTray || !_seaweedBasket.Dock(i,_kelp[i],.17f,i==0?-65:55))
            {_kelp[i].transform.position=UVToWorld(_kelpHomes[i]);_kelp[i].sortingOrder=70;Say("Thả rong vào giỏ bên trái, tách khỏi giỏ ốc và cua.");return false;}
            _kelpRemoved[i]=true;_progress=RemovedKelpCount/2f;_audio.PlayOneShot(_tick);
            if(RemovedKelpCount==2)FinishStep();return true;
        }
        private void TreatStain(Vector2 uv)
        {
            if(_tool==1)
            {
                BuildSoapAt(uv);_material.SetVector("_ScrubPoint",new Vector4(uv.x,uv.y,0,0));Upload();
            }
            for(int i=0;i<3;i++)if(Vector2.Distance(uv,_stains[i])<.11f)
            {
                if(_tool==0 && _stainPhase[i]==0){_stainPhase[i]=1;_stainTimers[i]=0;Say("Chờ dung dịch ngấm...");}
                else if(_tool==1 && _stainPhase[i]==2)
                {
                    _stainPhase[i]=3;
                    for(int y=0;y<N;y++)for(int x=0;x<N;x++)if(Vector2.Distance(new Vector2((x+.5f)/N,(y+.5f)/N),_stains[i])<.11f)
                    {int p=y*N+x;_pixels[p].r=Mathf.Max(_pixels[p].r,.96f);_pixels[p].g=Mathf.Max(_pixels[p].g,.9f);}
                    Upload();_audio.PlayOneShot(_tick);
                }
            }
            int done=0;for(int i=0;i<3;i++)if(_stainPhase[i]==3)done++;
            _progress=done/3f;if(done==3)FinishStep();
        }
        private void BuildSoapAt(Vector2 uv)
        {
            PaintFoamSurface(uv);
            int cx=Mathf.RoundToInt(uv.x*N),cy=Mathf.RoundToInt(uv.y*N),radius=10;
            for(int y=Mathf.Max(0,cy-radius);y<=Mathf.Min(N-1,cy+radius);y++)for(int x=Mathf.Max(0,cx-radius);x<=Mathf.Min(N-1,cx+radius);x++)
            {
                int i=y*N+x;float distance=Vector2.Distance(new Vector2(x,y),new Vector2(cx,cy));
                if(distance>radius || !_cushion[i])continue;float pressure=1-distance/radius;
                _pixels[i].g=Mathf.Min(1,Mathf.Max(.7f,_pixels[i].g)+.025f*pressure);_pixels[i].b=1;
                _brushPixels[i]=new Color(Mathf.Min(1,_brushPixels[i].r+.07f*pressure),uv.x,uv.y,_brushPixels[i].a);
            }
        }
        private void Paint(Vector2 uv)
        {
            if(Steps[_step].effect=="scrub")PaintFoamSurface(uv);
            int cx=Mathf.RoundToInt(uv.x*N),cy=Mathf.RoundToInt(uv.y*N);
            int radius=_step==3 || _step==8 || _step==12?6:10;
            for(int y=Mathf.Max(0,cy-radius);y<=Mathf.Min(N-1,cy+radius);y++)for(int x=Mathf.Max(0,cx-radius);x<=Mathf.Min(N-1,cx+radius);x++)
            {
                int i=y*N+x;if(!_region[i] || (x-cx)*(x-cx)+(y-cy)*(y-cy)>radius*radius)continue;
                if(_step==11 && (_dryPhase==0?!_cushion[i]:_cushion[i]))continue;
                if(!_covered[i]){_covered[i]=true;_hits++;}
                Color p=_pixels[i];string effect=Steps[_step].effect;
                if(effect=="clean" || effect=="scrub" || effect=="rinse")p.r=Mathf.Max(p.r,Steps[_step].clean);
                if(effect=="wet")p.b=1;
                if(effect=="sand")_brushPixels[i].a=1;
                if(effect=="foam")p.g=1;
                if(effect=="scrub")
                {
                    float pressure=1-Mathf.Sqrt((x-cx)*(x-cx)+(y-cy)*(y-cy))/radius;
                    p.g=Mathf.Min(1,Mathf.Max(.7f,p.g)+.025f*pressure);p.b=1;
                    _brushPixels[i]=new Color(Mathf.Min(1,_brushPixels[i].r+.07f*pressure),_brushStrokeCenter.x,_brushStrokeCenter.y,_brushPixels[i].a);
                }
                if(effect=="rinse"){p.g=0;p.b=1;p.r=1;_brushPixels[i]=Color.clear;}
                if(effect=="dry")p.b=0;
                if(effect=="repair" && _repairPhase==0)p.a=1;
                _pixels[i]=p;
            }
        }
        private void CheckCoverage()
        {
            int total=_required;
            if(_step==11)
            {total=0;for(int i=0;i<N*N;i++)if(_region[i] && (_dryPhase==0?_cushion[i]:!_cushion[i]))total++;}
            float coverage=_hits/(float)Mathf.Max(1,total);
            if(_step==6)
            {
                float built=0;
                for(int i=0;i<N*N;i++)if(_region[i])built+=Mathf.Clamp01(_brushPixels[i].r/.85f);
                _progress=built/Mathf.Max(1,total);
                if(_progress>=.97f)FinishStep();
                return;
            }
            if(_step==12)
            {
                if(_repairPhase==0)
                {
                    _progress=Mathf.Clamp01(coverage/.88f)*.35f;
                    if(coverage>=.88f){_repairPhase=1;_tool=-1;Say("Keo đã đủ. Kéo miếng da xanh vào chỗ rách.");}
                }
                else if(_repairPhase==2)
                {
                    _repairAmount=Mathf.Clamp01(coverage/.9f);_material.SetFloat("_Repair",Mathf.Lerp(.55f,1,_repairAmount));
                    _progress=.6f+.4f*_repairAmount;if(coverage>=.9f)FinishStep();
                }
                return;
            }
            if(_step==11)
            {
                _progress=(_dryPhase+Mathf.Clamp01(coverage/.86f))*.5f;
                if(coverage>=.86f && _dryPhase==0){_dryPhase=1;_tool=-1;_hits=0;Array.Clear(_covered,0,_covered.Length);Say("Đệm đã ráo. Chọn khăn để lau khô khung gỗ.");return;}
            }
            else _progress=Mathf.Clamp01(coverage/.86f);
            if(coverage>=.86f)FinishStep();
        }
        public bool BeginPatchDrag()
        {
            if(_step!=12 || _repairPhase!=1 || _tool!=1 || _pending)return false;
            _dragPatch=true;_pointerDown=true;return true;
        }
        public bool PlacePatchUV(Vector2 uv)
        {
            if(!_dragPatch)return false;_dragPatch=false;
            if(TearDistance(uv)>1.35f){Say("Đặt miếng da vào đúng vùng rách có viền keo.");return false;}
            _repairPhase=2;_tool=-1;_progress=.6f;_hits=0;_material.SetFloat("_Repair",.55f);
            _required=0;
            for(int y=0;y<N;y++)for(int x=0;x<N;x++)
            {int i=y*N+x;_region[i]=TearDistance(new Vector2((x+.5f)/N,(y+.5f)/N))<1.15f;if(_region[i])_required++;}
            Array.Clear(_covered,0,_covered.Length);Say("Chọn miết da, vuốt từ giữa ra các mép để ép dính.");return true;
        }
        private void FinishStep()
        {
            if(_pending)return;
            _progress=1;_pending=true;_pendingTime=.7f;_pointerDown=false;_hasLast=false;_tool=-1;
            // Resolve a completed pass so no thin strips of foam, dirt or moisture remain.
            if(_step>1 && _step!=7 && _step!=12)
            {
                for(int i=0;i<N*N;i++)if(_region[i])
                {
                    var p=_pixels[i];string effect=Steps[_step].effect;
                    if(effect=="clean" || effect=="scrub" || effect=="rinse")p.r=Mathf.Max(p.r,Steps[_step].clean);
                    if(effect=="wet")p.b=1;if(effect=="foam")p.g=1;
                    if(effect=="sand")_brushPixels[i].a=1;
                    if(effect=="scrub"){p.g=1;p.b=1;_brushPixels[i].r=1;}if(effect=="rinse"){p.r=1;p.g=0;p.b=1;_brushPixels[i]=Color.clear;}if(effect=="dry")p.b=0;
                    _pixels[i]=p;
                }
                Upload();
            }
            if(_step==12){_material.SetFloat("_Repair",1);_repairAmount=1;}
            _audio.PlayOneShot(_finish);Say("Hoàn thành bước "+(_step+1)+"!");
        }
        // Ported from ASMR WinterDoghousePresentation.Foam: persistent local deformation.
        private void PaintFoamSurface(Vector2 center)
        {
            if(Vector2.Distance(center,_lastFoamStamp)<.045f)return;
            _lastFoamStamp=center;
            const float radius=.09f;
            int cx=Mathf.RoundToInt(center.x*N),cy=Mathf.RoundToInt(center.y*N),reach=Mathf.CeilToInt(radius*N);
            for(int y=Mathf.Max(0,cy-reach);y<=Mathf.Min(N-1,cy+reach);y++)
            for(int x=Mathf.Max(0,cx-reach);x<=Mathf.Min(N-1,cx+reach);x++)
            {
                int index=y*N+x;if(!_cushion[index])continue;
                Vector2 d=new Vector2((x+.5f)/N,(y+.5f)/N)-center;
                float r=d.magnitude/radius;if(r>=1)continue;
                float weight=1-Mathf.SmoothStep(.25f,1,r),angle=weight*1.7f;
                Vector2 turned=new Vector2(Mathf.Cos(angle)*d.x-Mathf.Sin(angle)*d.y,Mathf.Sin(angle)*d.x+Mathf.Cos(angle)*d.y);
                Vector2 offset=turned-d;
                float curl=.5f+.5f*Mathf.Sin(r*17-Mathf.Atan2(d.y,d.x)*2);
                Color target=new Color(.5f+offset.x*5,.5f+offset.y*5,curl,1);
                _foamFlowPixels[index]=Color.Lerp(_foamFlowPixels[index],target,weight);
            }
        }
        private void Upload(){_mask.SetPixels(_pixels);_mask.Apply(false);_brushMask.SetPixels(_brushPixels);_brushMask.Apply(false);_foamFlow.SetPixels(_foamFlowPixels);_foamFlow.Apply(false);}
        private void Say(string message){_feedback=message;_feedbackTime=3;}
        private Vector3 UVToWorld(Vector2 uv)
        {var b=_chair.bounds;return new Vector3(Mathf.Lerp(b.min.x,b.max.x,uv.x),Mathf.Lerp(b.min.y,b.max.y,uv.y),-.15f);}
        private Vector2 ScreenToUV(Vector2 screen)
        {var p=_camera.ScreenToWorldPoint(new Vector3(screen.x,screen.y,10));var b=_chair.bounds;return new Vector2((p.x-b.min.x)/b.size.x,(p.y-b.min.y)/b.size.y);}
        private Vector2 UVToGUI(Vector2 uv)
        {var p=_camera.WorldToScreenPoint(UVToWorld(uv));return new Vector2(p.x/_uiScale,(Screen.height-p.y)/_uiScale);}
        private void FitCamera()
        {
            _width=Screen.width;_height=Screen.height;_uiScale=Screen.width/900f;_uiHeight=Screen.height/_uiScale;
            float aspect=(float)Screen.width/Mathf.Max(1,Screen.height);
            float available=Mathf.Clamp01((_uiHeight-300)/_uiHeight);
            var b=_chair.bounds;_camera.orthographic=true;
            _camera.orthographicSize=Mathf.Max(b.size.y/Mathf.Max(.25f,available*.88f),b.size.x/aspect/.86f)*.5f;
            _camera.transform.position=new Vector3(b.center.x,b.center.y,-10);
            if(_background!=null)
            {
                var n=_background.sprite.bounds.size;float s=Mathf.Max(_camera.orthographicSize*2/n.y,_camera.orthographicSize*2*aspect/n.x)*1.01f;
                _background.transform.localScale=new Vector3(s,s,1);_background.transform.position=new Vector3(b.center.x,b.center.y,0);
            }
            float halfWidth=_camera.orthographicSize*aspect;
            if(_leftPalm!=null)_leftPalm.transform.position=new Vector3(b.center.x-halfWidth-.26f,b.center.y+3.31f,-.002f);
            if(_rightPalm!=null)_rightPalm.transform.position=new Vector3(b.center.x+halfWidth-.74f,b.center.y+2.42f,-.002f);
            if(_rescueBasket!=null)_rescueBasket.Layout(_uiScale,_uiHeight);
            if(_seaweedBasket!=null)_seaweedBasket.Layout(_uiScale,_uiHeight);
        }

        private void InitStyles()
        {
            if(_heading!=null)return;
            _heading=new GUIStyle(GUI.skin.label){fontSize=27,fontStyle=FontStyle.Bold,wordWrap=true};_heading.normal.textColor=_navy;
            _body=new GUIStyle(GUI.skin.label){fontSize=19,wordWrap=true};_body.normal.textColor=_navy;
            _small=new GUIStyle(_body){fontSize=16};
            _button=new GUIStyle(GUI.skin.button){fontSize=18,fontStyle=FontStyle.Bold,wordWrap=true};
            _button.normal.background=Texture2D.whiteTexture;_button.hover.background=Texture2D.whiteTexture;_button.active.background=Texture2D.whiteTexture;
            _button.normal.textColor=_navy;_button.hover.textColor=_navy;_button.active.textColor=_navy;
            _badge=new GUIStyle(_body){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,fontSize=17};_badge.normal.textColor=Color.white;
        }
        private void Box(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        private void Disc(Vector2 p,float radius,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(new Rect(p.x-radius,p.y-radius,radius*2,radius*2),_dot);GUI.color=old;}
        private void OnGUI()
        {
            if(!IsReady)return;InitStyles();
            var previous=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(_uiScale,_uiScale,1));
            _top=new Rect(18,18,864,140);_tray=new Rect(18,_uiHeight-138,864,120);
            Box(_top,new Color(.97f,.99f,.97f,.97f));Box(new Rect(18,18,7,140),_teal);
            Box(new Rect(35,31,100,28),_teal);GUI.Label(new Rect(35,31,100,28),_done?"13 / 13":((_step+1)+" / 13"),_badge);
            GUI.Label(new Rect(149,29,497,38),_done?"GHẾ ĐÃ PHỤC HỒI!":Steps[_step].title,_heading);
            if(GUI.Button(new Rect(650,32,101,30),"Về biển",_button))UnityEngine.SceneManagement.SceneManager.LoadScene(ChairLevelEntry.ReturnScene);
            if(GUI.Button(new Rect(758,32,105,30),"Làm lại",_button))RestartLevel();
            GUI.Label(new Rect(36,74,823,49),_done?"Sạch cát, hết rong rêu, khô ráo và không còn vết rách.":Steps[_step].instruction,_body);
            Box(new Rect(36,134,828,8),new Color(.81f,.89f,.87f));Box(new Rect(36,134,828*(_done?1:(_step+_progress)/13f),8),_teal);
            if(_done){DrawCompletion();GUI.matrix=previous;return;}
            if(_step==12)DrawRepairOutline();Box(_tray,new Color(.97f,.99f,.97f,.97f));
            GUI.Label(new Rect(36,_tray.y+9,570,25),"DỤNG CỤ  ·  "+Mathf.RoundToInt(_progress*100)+"%",_small);
            DrawTools();
            if(_step==0)
            {
                GUI.Label(new Rect(670,_tray.y+16,190,28),"ĐÃ VÀO GIỎ",_small);
                GUI.Label(new Rect(670,_tray.y+46,190,40),RescuedCount+" / 5 bạn nhỏ",_heading);
            }
            else if(_step==1)
            {
                GUI.Label(new Rect(670,_tray.y+16,190,28),"RONG ĐÃ GẮP",_small);
                GUI.Label(new Rect(670,_tray.y+46,190,40),RemovedKelpCount+" / 2 búi",_heading);
            }
            else if(_step==12)DrawLeatherCard();
            else GUI.Label(new Rect(670,_tray.y+44,190,48),_tool<0?"Chọn dụng cụ\nđể bắt đầu":"Giữ và kéo\ntrên bề mặt ghế",_small);
            if(_feedbackTime>0)
            {
                float y=_tray.y-48;Box(new Rect(70,y,760,37),new Color(.035f,.16f,.2f,.9f));
                GUI.Label(new Rect(80,y+4,740,32),_feedback,_badge);
            }
            DrawCursorTool();GUI.matrix=previous;
        }
        private string[] ToolNames()
        {
            if(_step==0)return new[]{"Tay nhặt ốc","Kẹp gắp cua"};
            if(_step==7)return new[]{"Chấm dung dịch","Bọt biển"};
            if(_step==11)return new[]{"Máy hút nước","Khăn lau khô"};
            if(_step==12)return new[]{"1. Bóp keo","2. Miếng da","3. Miết da"};
            return new[]{Steps[_step].tool};
        }
        private void DrawTools()
        {
            var names=ToolNames();float width=names.Length==3?194:270;
            for(int i=0;i<names.Length;i++)
            {
                Rect r=new Rect(36+i*(width+9),_tray.y+43,width,57);
                var old=GUI.backgroundColor;GUI.backgroundColor=_tool==i?_gold:new Color(.7f,.9f,.88f);
                if(GUI.Button(r,names[i],_button))SelectTool(i);GUI.backgroundColor=old;
                if(_step==0 && i==1 && _crabTongsArt!=null)GUI.DrawTexture(new Rect(r.x+9,r.y+4,35,r.height-8),_crabTongsArt,ScaleMode.ScaleToFit);
                else if(_step>0)DrawToolGraphic(ToolArtIndex(i),new Rect(r.x+7,r.y+4,43,r.height-8));
            }
        }
        private void DrawRepairOutline()
        {
            if(_pending)return;var matrix=GUI.matrix;
            for(int k=0;k<48;k++)
            {
                float a=k*Mathf.PI*2/48,b=(k+1)*Mathf.PI*2/48;
                Vector2 p=UVToGUI(new Vector2(_tear.x+Mathf.Cos(a)*_tear.z*.95f,_tear.y+Mathf.Sin(a)*_tear.w*.95f));
                Vector2 q=UVToGUI(new Vector2(_tear.x+Mathf.Cos(b)*_tear.z*.95f,_tear.y+Mathf.Sin(b)*_tear.w*.95f));
                GUIUtility.RotateAroundPivot(Mathf.Atan2(q.y-p.y,q.x-p.x)*Mathf.Rad2Deg,p);
                Box(new Rect(p.x,p.y,Vector2.Distance(p,q)+1,2),_repairPhase==0?_gold:_teal);GUI.matrix=matrix;
            }
        }
        private void DrawLeatherCard()
        {
            if(_repairPhase!=1)return;
            Rect r=new Rect(675,_tray.y+15,175,88);Box(r,new Color(.17f,.7f,.7f));
            GUI.Label(new Rect(r.x+6,r.y+8,r.width-12,30),"DA XANH NGỌC",_badge);GUI.Label(new Rect(r.x+6,r.y+43,r.width-12,32),"Kéo vào chỗ rách",_small);
            if(Event.current.type==EventType.MouseDown && r.Contains(Event.current.mousePosition) && _tool==1)
            {BeginPatchDrag();Event.current.Use();}
            if(_dragPatch)
            {
                Vector2 p=new Vector2(_pointer.x/_uiScale,(Screen.height-_pointer.y)/_uiScale);
                Rect patch=new Rect(p.x-70,p.y-48,140,96);DrawToolGraphic(13,patch);
            }
        }
        private void DrawCursorTool()
        {
            if(CrabTongsVisible){DrawCrabTongs();return;}
            if(_tool<0 || _step==0 || _dragPatch || _pending)return;
            Vector2 p=new Vector2(_pointer.x/_uiScale,(Screen.height-_pointer.y)/_uiScale);
            if(_top.Contains(p) || _tray.Contains(p))return;
            if(_step==4 || _step==5 || _step==10)p+=new Vector2(58,-72);
            var old=GUI.matrix;
            float wobble=_pointerDown && (_step==2 || _step==6 || _step==8 || _step==9)?Mathf.Sin(Time.time*18)*6:0;
            GUIUtility.RotateAroundPivot(wobble,p);
            DrawToolGraphic(ToolArtIndex(_tool),new Rect(p.x-22,p.y-22,145,145));GUI.matrix=old;
        }
        private int ToolArtIndex(int tool)
        {
            switch(_step)
            {
                case 1:return 15;case 2:return 0;case 3:return 1;case 4:return 2;case 5:return 3;
                case 6:return 4;case 7:return tool==0?5:6;case 8:return 7;case 9:return 8;case 10:return 9;
                case 11:return tool==0?10:11;case 12:return 12+tool;default:return 15;
            }
        }
        private void DrawToolGraphic(int index,Rect box)
        {
            if(_toolUVs==null || _toolAtlas==null)return;
            float aspect=_toolAspects[index];float width=Mathf.Min(box.width,box.height*aspect),height=width/aspect;
            GUI.DrawTextureWithTexCoords(new Rect(box.x+(box.width-width)*.5f,box.y+(box.height-height)*.5f,width,height),_toolAtlas,_toolUVs[index]);
        }
        private void DrawCrabTongs()
        {
            Vector2 screen=_tongsReleaseTime>0?_tongsReleaseScreen:_pointer;
            if(_heldAnimal==4)screen=_camera.WorldToScreenPoint(_animals[4].transform.position);
            if(_heldKelp>=0)screen=_camera.WorldToScreenPoint(_kelp[_heldKelp].transform.position+Vector3.up*_kelp[_heldKelp].bounds.size.y*.36f);
            Vector2 p=new Vector2(screen.x/_uiScale,(Screen.height-screen.y)/_uiScale);
            if(_tongsReleaseTime<=0 && _heldAnimal<0 && _heldKelp<0 && (_top.Contains(p)||_tray.Contains(p)))return;
            // Fit the opened jaws around the actual crab, at every camera zoom and screen ratio.
            float heldWidth=_heldKelp>=0?_kelp[_heldKelp].bounds.size.x:Mathf.Max(.8f,_animals[4].bounds.size.x);
            float crabWidth=heldWidth/(_camera.orthographicSize*2*_camera.aspect)*Screen.width/_uiScale;
            float width=Mathf.Clamp(crabWidth*1.35f,105,205);
            float height=width*_crabTongsArt.height/_crabTongsArt.width;
            Rect rect=new Rect(p.x-width*.5f,p.y-height*.88f,width,height);
            Vector2 hinge=new Vector2(rect.center.x,rect.y+height*.11f);
            var matrix=GUI.matrix;var color=GUI.color;
            float alpha=_tongsReleaseTime>0?Mathf.Clamp01(_tongsReleaseTime/.09f):1;
            GUI.color=new Color(1,1,1,alpha);
            GUIUtility.RotateAroundPivot(-_tongsGrip*8,hinge);
            GUI.DrawTextureWithTexCoords(new Rect(rect.x,rect.y+height*.15f,width*.5f,height*.85f),_crabTongsArt,new Rect(0,0,.5f,.85f));
            GUI.matrix=matrix;GUIUtility.RotateAroundPivot(_tongsGrip*8,hinge);
            GUI.DrawTextureWithTexCoords(new Rect(rect.center.x,rect.y+height*.15f,width*.5f,height*.85f),_crabTongsArt,new Rect(.5f,0,.5f,.85f));
            GUI.matrix=matrix;
            // Preserve the original continuous metal hinge over the moving arms.
            GUI.DrawTextureWithTexCoords(new Rect(rect.x,rect.y,width,height*.21f),_crabTongsArt,new Rect(0,.79f,1,.21f));
            GUI.color=color;
        }
        private void DrawCompletion()
        {
            if(_uiHeight<650)
            {
                Rect compact=new Rect(145,_uiHeight-133,610,115);Box(compact,new Color(.97f,.99f,.97f,.97f));
                GUI.Label(new Rect(compact.x+16,compact.y+5,575,35),"HOÀN THÀNH 13 BƯỚC",_heading);
                GUI.Label(new Rect(compact.x+16,compact.y+38,575,25),"Ốc và cua đã an toàn · Ghế sạch và hết rách",_small);
                if(GUI.Button(new Rect(compact.x+16,compact.y+70,260,34),"Chơi lại",_button))RestartLevel();
                bool compare=GUI.RepeatButton(new Rect(compact.x+290,compact.y+70,304,34),"Giữ để xem trước",_button);
                _material.SetFloat("_Before",compare?1:0);return;
            }
            Rect card=new Rect(145,_uiHeight-245,610,210);Box(card,new Color(.97f,.99f,.97f,.97f));
            GUI.Label(new Rect(card.x+25,card.y+15,560,45),"HOÀN THÀNH 13 BƯỚC",_heading);
            GUI.Label(new Rect(card.x+25,card.y+66,560,52),"Đã đưa 4 con ốc và chú cua về nơi an toàn.\nChiếc ghế sạch sẽ, miếng da đã dán kín chỗ rách.",_body);
            if(GUI.Button(new Rect(card.x+25,card.y+139,250,48),"Chơi lại",_button))RestartLevel();
            bool before=GUI.RepeatButton(new Rect(card.x+300,card.y+139,280,48),"Giữ để xem trước",_button);
            _material.SetFloat("_Before",before?1:0);
        }
        private void OnDestroy()
        {
            Application.runInBackground=_previousRunInBackground;
            if(_chair!=null && _original!=null)_chair.sharedMaterial=_original;
            if(_material!=null)Destroy(_material);if(_mask!=null)Destroy(_mask);if(_dot!=null)Destroy(_dot);
            if(_cushionEdge!=null)Destroy(_cushionEdge);if(_brushMask!=null)Destroy(_brushMask);if(_foamFlow!=null)Destroy(_foamFlow);
            if(_tick!=null)Destroy(_tick);if(_finish!=null)Destroy(_finish);
            if(_animalSprites!=null)foreach(var s in _animalSprites)if(s!=null)Destroy(s);
            if(_kelpSprites!=null)foreach(var s in _kelpSprites)if(s!=null)Destroy(s);
        }
    }
}

